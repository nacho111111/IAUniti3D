using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine.UIElements;
using System;
using Unity.MLAgents.Sensors;
using static UnityEngine.GraphicsBuffer;

public class MultiAgentv2 : Agent
{
    //public enum Role
    //{
    //    Hunter,
    //    Hunted
    //}

    [HideInInspector] public Color colorState;
    public float Energy;
    [HideInInspector] public float MaxEnergy;
    [HideInInspector] public float LossOfEnergy;
    [HideInInspector] public float MovSpeed;

    private float m_RealLossEnergy;
    [HideInInspector]
    public Team team;

    //public Role role;

    private float m_Existential; // penalizacion por tiempo de ejecucion 
    private float m_HuntedExistentialBonus; // bonus por no morir hunted 
    public float temp;
  
    private Renderer m_Renderer;

    [HideInInspector]
    public Rigidbody agentRb;
    public bool state;  // estado, si ha comido o no
    //private TargetMulti m_target;
    private EnvControllerv2 m_envController;

    BehaviorParameters m_BehaviorParameters;


    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
        m_Renderer = GetComponentInParent<Renderer>();
        m_envController = gameObject.GetComponentInParent<EnvControllerv2>();

        m_Existential = 1f / m_envController.MaxEnvironmentSteps;
        m_RealLossEnergy = LossOfEnergy;

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();

        if (m_BehaviorParameters.TeamId == (int)Team.Hunted)
        {
            team = Team.Hunted;
        }
        else
        {
            team = Team.Hunter;
        }
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(state); // bool, 1 observacion
        sensor.AddObservation(transform.InverseTransformDirection(agentRb.velocity)); // referencia de orientacion // Vector3, 3 observaciones
        sensor.AddObservation(Energy/MaxEnergy); // energia normalizada 
    }
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 0:// no hace nada
                Energy -= m_RealLossEnergy * 0.1f;
                break;
            case 1:
                dirToGo = transform.forward * 1f;
                Energy -= m_RealLossEnergy;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                Energy -= m_RealLossEnergy;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                Energy -= m_RealLossEnergy;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                Energy -= m_RealLossEnergy;
                break;
        }
        transform.Rotate(rotateDir, Time.deltaTime * 200f);

        agentRb.AddForce(dirToGo * MovSpeed, ForceMode.VelocityChange);
        

    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (team == Team.Hunted)
        {
            AddReward(-m_Existential + m_HuntedExistentialBonus);
            temp = -m_Existential + m_HuntedExistentialBonus;
            m_HuntedExistentialBonus += 0.001f / m_envController.MaxEnvironmentSteps;
            if (Energy <= 0)
            {
                RespawnAgent();
            }
        }
        else
        {
            AddReward(-m_Existential);
            if (Energy <= 0)
            {
                RespawnAgent();
            }
        }

        MoveAgent(actionBuffers.DiscreteActions); // recibe 1 vector de 5 observaciones 

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (team == Team.Hunted)
    //    {
    //        if (state == true && other.CompareTag("HuntedArea")) // cuando deposita el target
    //        {
    //            state = false;
    //            m_target.MoveTo(other.transform.position); // funcion mover a
    //            m_target = null; // suelta el target que transporta 
    //            Energy = MaxEnergy;
    //            //AddReward(2f); // premio
    //        }
    //    }
    //    else
    //    {
    //        if (state == true && other.CompareTag("HunterArea")) // cuando deposita la presa
    //        {
    //            state = false;
    //            m_Renderer.material = m_MaterialHunted1;
    //            Energy = MaxEnergy;
    //            //AddReward(2f); // premio
    //        }
    //    }
    //}
    private void OnCollisionEnter(Collision collision)
    {
        if (team == Team.Hunted)
        {
            if (state == false && collision.gameObject.CompareTag("Target")) // cuando toma el target
            {
                state = true;
                Energy = MaxEnergy;
                m_Renderer.material.color = colorState;
                m_RealLossEnergy = 0; // al "comer" deja de perder energia
                AddReward(1.5f);
                m_envController.RewardGroup(Team.Hunted);

                collision.gameObject.GetComponent<TargetMulti>().ResetTarget();
                m_envController.SpawnObject(collision.transform);
                //m_target = collision.gameObject.GetComponent<TargetMulti>();// guarda el target que transporta
                //m_target.FollowTo(transform);
                //AddReward(0.5f); 


            }
        }
        else
        {
            if (state == false && collision.gameObject.CompareTag("hunted")) // el cazador atrapa
            {
                state = true;
                Energy = MaxEnergy;
                m_Renderer.material.color = colorState;
                m_RealLossEnergy = 0;
                AddReward(2f); // premio
                m_envController.RewardGroup(Team.Hunter);

                //Debug.Log(gameObject.name + " ha cazado a " + collision.gameObject.name);

                // comportamiento hunted
                MultiAgentv2 agentHunted = collision.gameObject.GetComponent<MultiAgentv2>();
            
                agentHunted.RespawnAgent();
                //collision.gameObject.SetActive(false); // desactiva el hunted
                
            }
        }
    }
    //public void ReconfigTarget()
    //{
    //    if (m_target != null)
    //    {
    //        m_target.ResetTarget();
    //    }
    //}

    // reespawn 

    private void RespawnAgent()
    {
        if (state == true)
        {
            m_envController.Penalize(team);
            AddReward(-3f); // si "comio" y "muere" el castigo es mayor
        }
        else
        {
            AddReward(-2f); 
        }
        
        ResetAgent();
        m_envController.SpawnObject(transform);
    }
    public void ResetAgent()
    {
        if (team == Team.Hunted)
        {
            m_HuntedExistentialBonus = 0;
        }
        agentRb.angularVelocity = Vector3.zero;
        agentRb.velocity = Vector3.zero;
        state = false;
        Energy = MaxEnergy;
        m_Renderer.material.color = Color.white;
        m_RealLossEnergy = LossOfEnergy;
    }

    // end respawn
    public override void OnEpisodeBegin()
    {

    }
}
