using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine.UIElements;
using System;
using Unity.MLAgents.Sensors;
using static UnityEngine.GraphicsBuffer;

public enum Team
{
    Hunted = 0, // Blue
    Hunter = 1  // Red
}
public class MultiAgent : Agent
{
    //public enum Role
    //{
    //    Hunter,
    //    Hunted
    //}

    public Color colorHunter;

    public float Energy = 0f;
    public float MaxEnergy = 100f;
    public float LossOfEnergy = 0.2f ;

    [HideInInspector]
    public Team team;

    //public Role role;

    private float m_Existential; // penalizacion por tiempo de ejecucion 
    public float m_HuntedExistentialBonus; // bonus por no morir hunted 
    
    //private Material m_MaterialHunted1;
    private Renderer m_Renderer;
    [SerializeField]
    [Tooltip("Solo se usa con hunter")]
    //private Material m_MaterialHunted2;

    [HideInInspector]
    public Rigidbody agentRb;
    [HideInInspector] public bool state;  // estado, para el hunted es si esta transportando un Target o no, para el hunter es si esta transportado un hunted
    private TargetMulti m_target;
    private EnvController m_envController;

    BehaviorParameters m_BehaviorParameters;


    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
        m_envController = gameObject.GetComponentInParent<EnvController>();

        m_Existential = 1f / m_envController.MaxEnvironmentSteps;
 
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();

        Energy = MaxEnergy;
        if (m_BehaviorParameters.TeamId == (int)Team.Hunted)
        {
            team = Team.Hunted;
            m_HuntedExistentialBonus = 1f / m_envController.MaxEnvironmentSteps;
        }
        else
        {
            team = Team.Hunter;

            // rederer initialize
            m_Renderer = GetComponentInParent<Renderer>();
            //m_MaterialHunted1 = m_Renderer.material;
        }
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(state); // bool, 1 observacion
        sensor.AddObservation(transform.InverseTransformDirection(agentRb.velocity)); // referencia de orientacion // Vector3, 3 observaciones
    }
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                Energy -= LossOfEnergy;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                Energy -= LossOfEnergy;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                Energy -= LossOfEnergy;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                Energy -= LossOfEnergy;
                break;
                
        }
        transform.Rotate(rotateDir, Time.deltaTime * 200f);
        agentRb.AddForce(dirToGo * 2f, ForceMode.VelocityChange);
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (team == Team.Hunted)
        {
            AddReward(-m_Existential + m_HuntedExistentialBonus);
            m_HuntedExistentialBonus += 0.01f / m_envController.MaxEnvironmentSteps;
            if (Energy <= 0)
            {
                ResetAgentHunted();
            }
        }
        else
        {
            AddReward(-m_Existential);
            if (Energy <= 0)
            {
                ResetAgentHunter();
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
    private void OnTriggerEnter(Collider other)
    {
        if (team == Team.Hunted)
        {
            if (state == true && other.CompareTag("HuntedArea")) // cuando deposita el target
            {
                state = false;
                m_target.MoveTo(other.transform.position); // funcion mover a
                m_target = null; // suelta el target que transporta 
                Energy = MaxEnergy;
                //AddReward(2f); // premio
            }
        }
        else
        {
            if (state == true && other.CompareTag("HunterArea")) // cuando deposita la presa
            {
                state = false;
                m_Renderer.material.color = Color.white;
                Energy = MaxEnergy;
                //AddReward(2f); // premio
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (team == Team.Hunted)
        {
            if (state == false && collision.gameObject.CompareTag("Target")) // cuando toma el target
            {
                state = true;
                m_target = collision.gameObject.GetComponent<TargetMulti>();// guarda el target que transporta
                m_target.FollowTo(transform);
                AddReward(0.5f); // premio
            }
        }
        else
        {
            if (state == false && collision.gameObject.CompareTag("hunted")) // el cazador atrapa
            {
                m_Renderer.material.color = colorHunter;

                AddReward(1f); // premio
                state = true;
                //Debug.Log(gameObject.name + " ha cazado a " + collision.gameObject.name);

                // comportamiento hunted
                MultiAgent agentHunted = collision.gameObject.GetComponent<MultiAgent>();

                agentHunted.ResetAgentHunted();
                //collision.gameObject.SetActive(false); // desactiva el hunted
            }
        }
    }
    public void ReconfigTarget()
    {
        if (m_target != null)
        {
            m_target.ResetTarget();
        }
    }

    // reespawn 

    private void ResetAgentHunted() 
    {
        ReconfigTarget();
        m_HuntedExistentialBonus = 1f / m_envController.MaxEnvironmentSteps;
        ResetAgent();
    }
    private void ResetAgentHunter() 
    {
        ResetRendererHunter();
        ResetAgent();
    }
    private void ResetAgent()
    {
        AddReward(-2f); // castigo  
        state = false;
        agentRb.velocity = Vector3.zero;
        Energy = MaxEnergy;
        m_envController.SpawnObject(transform);
    }
    public void ResetRendererHunter()
    {
        m_Renderer.material.color = Color.white;
    }

    // end respawn
    public override void OnEpisodeBegin()
    {

    }
}
