using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UnloadingArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            AgentML agent = other.GetComponent<AgentML>();
            if (agent.Transporting == true)
            {
                agent.Target.GetComponent<Target>().MoveTo(transform);
            }
        }
    }
}
