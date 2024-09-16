using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using static UnityEditor.Progress;

//using static UnityEditor.Progress;
//using static UnityEditor.Timeline.Actions.MenuPriority;
using Random = UnityEngine.Random;

public class EnvControllerv2 : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public MultiAgentv2 Agent;
        [HideInInspector]
        public Rigidbody Rb;
    }
    [System.Serializable]
    public class TargetInfo
    {
        public TargetMulti Target;
        [HideInInspector]
        public Rigidbody Rb;
    }
    [System.Serializable]
    public class SpawnInfo
    {
        public Transform Tr;
        [HideInInspector]
        public Vector3 Vector;
    }

    [Header("Configuracion")]
    
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    public Color colorHuntedState;
    public Color colorHunterState;

    public float MovSpeedHunted = 2;
    public float MovSpeedHunter = 2;

    public float MaxEnergyHunted;
    public float MaxEnergyHunter;

    public float LossOfEnergyHunted;
    public float LossOfEnergyHunter;

    private SimpleMultiAgentGroup m_HuntedGroup;
    private SimpleMultiAgentGroup m_HunterGroup;

    private int m_ResetTimer;

    [Header("Listas")]

    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    public List<TargetInfo> TargetList = new List<TargetInfo>();
    public List<SpawnInfo> SpawnList = new List<SpawnInfo>();

    private int m_countAgents;
    private int m_countTarget;
    private int m_countSpawns;

    private int scoreHunted = 0;
    private int scoreHunter = 0;

    void Start()
    {
        // el colicionador de "unloadingArea" no se aplica para "hunted", ya que este simula un escodite para estos 
        Physics.IgnoreLayerCollision(6, 7);

        // Initialize count
        m_countAgents = AgentsList.Count;
        m_countSpawns = SpawnList.Count;
        m_countTarget = TargetList.Count;

        // Initialize TeamManager
        m_HuntedGroup = new SimpleMultiAgentGroup();
        m_HunterGroup = new SimpleMultiAgentGroup();

        // Initialize Info
        foreach (var item in AgentsList)
        {
            //item.StartingPos = item.Agent.transform.position;
            //item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Hunted)
            {
                m_HuntedGroup.RegisterAgent(item.Agent);
                iniciaInitializeHunted(item.Agent);
            }
            else
            {
                m_HunterGroup.RegisterAgent(item.Agent);
                iniciaInitializeHunter(item.Agent);
            }
        }
        foreach (var item in TargetList)
        {
            item.Rb = item.Target.GetComponent<Rigidbody>();
        }
        foreach (var item in SpawnList)
        {
            item.Vector = item.Tr.position;
        }
        ResetScene();
    }
    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_HuntedGroup.GroupEpisodeInterrupted();
            m_HunterGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }
    public void iniciaInitializeHunted(MultiAgentv2 hunted)
    {
        hunted.colorState = colorHuntedState;
        hunted.MaxEnergy = MaxEnergyHunted;
        hunted.Energy = MaxEnergyHunted;
        hunted.LossOfEnergy = LossOfEnergyHunted;
        hunted.MovSpeed = MovSpeedHunted;
    }
    public void iniciaInitializeHunter(MultiAgentv2 hunted)
    {
        hunted.colorState = colorHunterState;
        hunted.LossOfEnergy = LossOfEnergyHunter;
        hunted.MaxEnergy = MaxEnergyHunter;
        hunted.Energy = MaxEnergyHunter;
        hunted.MovSpeed = MovSpeedHunter;
    }
    public void SpawnObject(Transform tr)
    {
        int index = Random.Range(0, m_countSpawns);
        tr.position = SpawnList[index].Vector;
    }
    public void RewardGroup(Team team)
    {
        if (team == Team.Hunted)
        {
            m_HuntedGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            scoreHunted++;
            if (scoreHunted == 3)
            {
                Debug.Log("Hunted Group Reward");
                m_HuntedGroup.AddGroupReward(2 - (float)m_ResetTimer / MaxEnvironmentSteps);
                m_HunterGroup.AddGroupReward(-1);
                m_HuntedGroup.EndGroupEpisode();
                m_HunterGroup.EndGroupEpisode();
                ResetScene();
            }
        }
        else
        {
            m_HunterGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            scoreHunter++;
            if (scoreHunter == 2)
            {
                Debug.Log("Hunter Group Reward");
                m_HunterGroup.AddGroupReward(2 - (float)m_ResetTimer / MaxEnvironmentSteps);
                m_HuntedGroup.AddGroupReward(-1);
                m_HuntedGroup.EndGroupEpisode();
                m_HunterGroup.EndGroupEpisode();
                ResetScene();
            }
            //HowManyHunted();
        }
    }
    public void Penalize(Team team)
    {
        if(team == Team.Hunted)
        {
            scoreHunted--;
        }
        else
        {
            scoreHunter--;
        }
        
    }
    //public void HowManyHunted() // detecta si se resetea la escena por falta de hunteds
    //{
    //    int howMany = m_HuntedGroup.GetRegisteredAgents().Count;
    //    Debug.Log("quedan " + howMany + " hunteds");
    //    if (howMany == 0)
    //    {
    //        ResetScene();
    //    }
    //}

    public void ResetScene()
    {
        m_ResetTimer = 0;

        var randomIndices = Enumerable.Range(0, m_countSpawns)
                                    .OrderBy(x => Guid.NewGuid())
                                    .Take(m_countAgents + m_countTarget)
                                    .ToArray();

        // Reset Agents
        for (int i = 0; i < m_countAgents; i++)
        {
            var index = randomIndices[i];
            var item = AgentsList[i];
            var newPosition = SpawnList[index].Vector;
            var newRotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

            item.Agent.transform.SetPositionAndRotation(newPosition, newRotation);

            //item.Agent.gameObject.SetActive(true);
            //if (item.Agent.team == Team.Hunted)
            //{
            //    m_HuntedGroup.RegisterAgent(item.Agent);
            //}
            //else
            //{
            //    m_HunterGroup.RegisterAgent(item.Agent);
            //    item.Agent.ResetRendererHunter();
            //}
            item.Agent.ResetAgent();

        }
        // Reset Target
        for (int i = 0; i < m_countTarget; i++)
        {
            var index = randomIndices[m_countAgents + i];
            var newPosition = SpawnList[index].Vector;
            TargetList[i].Target.transform.position = newPosition;
            TargetList[i].Target.ResetTarget();
            TargetList[i].Rb.velocity = Vector3.zero;
        }
        // reset scores
        scoreHunted = 0;
        scoreHunter = 0;
    }
}
