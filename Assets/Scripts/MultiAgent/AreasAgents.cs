using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreasAgents : MonoBehaviour
{
    [SerializeField]
    private GameObject esenary;
    private EnvController m_envController;

    private void Start()
    {
        m_envController = esenary.GetComponent<EnvController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        MultiAgent agent = other.GetComponent<MultiAgent>();

        if (agent != null)
        {
            if (tag == "HuntedArea" && other.CompareTag("hunted"))
            {
                if (agent.state == true)
                {
                    Debug.Log("hunted rewards");
                    m_envController.RewardGroup(Team.Hunted);
                }
            }
            else if (tag == "HunterArea" && other.CompareTag("hunter"))
            {
                if (agent.state == true)
                {
                    Debug.Log("hunters rewards");
                    m_envController.RewardGroup(Team.Hunter);
                }
            }
        }
    }
}
