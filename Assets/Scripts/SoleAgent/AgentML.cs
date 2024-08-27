using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;
using static UnityEngine.GraphicsBuffer;
public class AgentML : Agent
{
    //[SerializeField]
    //private Transform _target;

    private Rigidbody m_AgentRb;
    public bool useVectorObs;
    [HideInInspector] public bool Transporting;
    [HideInInspector] public GameObject Target;
    //private Rigidbody TargetRB;
    [SerializeField] private GameObject GmSpawn;
    private EsenarySpawn Spawn;

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        Spawn = GmSpawn.GetComponent<EsenarySpawn>();
    }
    public override void OnEpisodeBegin()
    {
        m_AgentRb.velocity = Vector3.zero;
        //m_AgentRb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        //TargetRB.velocity = Vector3.zero;

        Spawn.ResetRandomSpawn();
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
        m_AgentRb.AddForce(dirToGo * 2f, ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        AddReward(-1f / MaxStep); // por cada paso da una penalizacion 
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            sensor.AddObservation(Transporting);
            sensor.AddObservation(transform.InverseTransformDirection(m_AgentRb.velocity)); // referencia de orientacion 
        }
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
        if (Transporting == true && other.CompareTag("UnloadingArea")) // cuando deposita el target
        {
            Transporting = false;
            AddReward(2f); // premio
            EndEpisode();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Target")) // cuando toma el target
        {
            AddReward(0.5f); // premio
        }
        if (collision.gameObject.CompareTag("hunter")) // lo atrapa el cazador 
        {
            AddReward(-2f); // castigo
        }
    }
}
