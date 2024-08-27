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

    [HideInInspector]
    public Team team;

    //public Role role;

    private float m_Existential; // penalizacion por tiempo de ejecucion 
    
    private Material m_MaterialHunted1;
    private Renderer m_Renderer;
    [SerializeField]
    [Tooltip("Solo se usa con hunter")]
    private Material m_MaterialHunted2;

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

        if (m_envController != null)
        {
            m_Existential = 1f / m_envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Hunted)
        {
            team = Team.Hunted;
        }
        else
        {
            team = Team.Hunter;

            // rederer initialize
            m_Renderer = GetComponentInParent<Renderer>();
            m_MaterialHunted1 = m_Renderer.material;
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
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
        }
        transform.Rotate(rotateDir, Time.deltaTime * 200f);
        agentRb.AddForce(dirToGo * 2f, ForceMode.VelocityChange);
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-m_Existential);
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
                //AddReward(2f); // premio
            }
        }
        else
        {
            if (state == true && other.CompareTag("HunterArea")) // cuando deposita la presa
            {
                state = false;
                m_Renderer.material = m_MaterialHunted1;
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
                m_Renderer.material = m_MaterialHunted2;

                AddReward(1f); // premio
                state = true;
                //Debug.Log(gameObject.name + " ha cazado a " + collision.gameObject.name);

                // comportamiento hunted
                MultiAgent agentHunted = collision.gameObject.GetComponent<MultiAgent>();
                agentHunted.AddReward(-2f); // castigo al hunted
                agentHunted.ReconfigTarget();
                ResetAgentHunted(agentHunted);
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
    private void ResetAgentHunted(MultiAgent agentHunted)
    {
        agentHunted.state = false;
        agentHunted.agentRb.velocity = Vector3.zero;
        m_envController.SpawnObject(agentHunted.transform);
    }
    public void ResetRendererHunter()
    {
        m_Renderer.material = m_MaterialHunted1;
    }
    public override void OnEpisodeBegin()
    {

    }
}
