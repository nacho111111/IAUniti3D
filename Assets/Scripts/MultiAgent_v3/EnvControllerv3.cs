using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

//using static UnityEditor.Progress;
//using static UnityEditor.Timeline.Actions.MenuPriority;
using Random = UnityEngine.Random;

public class EnvControllerv3 : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public MultiAgentv3 Agent;
        [HideInInspector]
        public Rigidbody Rb;
    }
    //[System.Serializable]
    //public class TargetInfo
    //{
    //    public TargetMulti Target;
    //    [HideInInspector]
    //    public Rigidbody Rb;
    //}
    [System.Serializable]
    public class SpawnInfo
    {
        public Transform Tr;
        [HideInInspector]
        public Vector3 Vector;
    }

    [Header("Agentes")]

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
    [Header("Generator")]

    [SerializeField] private float GrowthTime;
    [SerializeField] private float DespawnTime;
    [SerializeField] private float FoodGenerateTime;
    [SerializeField] private int FoodSpawnRate;
    [SerializeField] private Material PostMaterial;


    [Header("Listas")]

    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    //public List<TargetInfo> TargetList = new List<TargetInfo>();
    public List<SpawnInfo> SpawnList = new List<SpawnInfo>();
    private Stack<GameObject> IdleHuntedStack = new Stack<GameObject>();
    private Stack<GameObject> IdleHunterStack = new Stack<GameObject>();
    private Stack<GameObject> IdleTargetStack = new Stack<GameObject>();
    private Stack<GameObject> IdleGeneratorStack = new Stack<GameObject>();

    [SerializeField] private GameObject m_PrefavHunted;
    [SerializeField] private GameObject m_PrefavHunter;
    [SerializeField] private GameObject m_PrefavGenerator;
    [SerializeField] private GameObject m_PrefavTarget;
    

    private int m_countAgents;
    private int m_countTarget;
    private int m_countSpawns;

    private int scoreHunted = 0;
    private int scoreHunter = 0;

                 //////////////////
                /// Initialize ///
               //////////////////
    
    void Start()
    {
        // el colicionador de "unloadingArea" no se aplica para "hunted", ya que este simula un escodite para estos 
        Physics.IgnoreLayerCollision(6, 7);

        // Initialize count
        m_countAgents = AgentsList.Count;
        m_countSpawns = SpawnList.Count;
        //m_countTarget = TargetList.Count;

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
                InitializeHunted(item.Agent);
            }
            else
            {
                m_HunterGroup.RegisterAgent(item.Agent);
                InitializeHunter(item.Agent);
            }
        }
        //foreach (var item in TargetList)
        //{
        //    item.Rb = item.Target.GetComponent<Rigidbody>();
        //}
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
    private void InitializeHunted(MultiAgentv3 hunted)
    {
        hunted.colorState = colorHuntedState;
        hunted.MaxEnergy = MaxEnergyHunted;
        hunted.Energy = MaxEnergyHunted;
        hunted.LossEnergy = LossOfEnergyHunted;
        hunted.MovSpeed = MovSpeedHunted;
    }
    private void InitializeHunter(MultiAgentv3 hunted)
    {
        hunted.colorState = colorHunterState;
        hunted.LossEnergy = LossOfEnergyHunter;
        hunted.MaxEnergy = MaxEnergyHunter;
        hunted.Energy = MaxEnergyHunter;
        hunted.MovSpeed = MovSpeedHunter;
    }
    private void InitializeGenerator(Generator generator)
    {
        generator.DespawnTime = DespawnTime;
        generator.RealDespawnTime = DespawnTime;
        generator.GenerateTime = FoodGenerateTime;
        generator.RealGenerateTime = FoodGenerateTime;
        generator.GrowthTime = GrowthTime;
        generator.RealGrowthTime = GrowthTime;
        generator.SpawnRate = FoodSpawnRate; 
        generator.PostMaterial = PostMaterial;
    }

             ///////////////
            /// Rewards ///
           ///////////////

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
        if (team == Team.Hunted)
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

    //////////////
    /// spawns ///
    //////////////

    public void SpawnObject(Transform tr)
    {
        int index = Random.Range(0, m_countSpawns);
        tr.position = SpawnList[index].Vector;
    }

    // agent
    public void SpawnAgent(Team team, Vector3 position)
    {
        GameObject gameObject;

        if (team == Team.Hunted)
        {
            if (IdleHuntedStack.Count > 0)
            {
                gameObject = IdleHuntedStack.Pop();
                gameObject.transform.position = position;
            }
            else
            {
                gameObject = Instantiate(m_PrefavHunted, position, Quaternion.identity, transform);
            }
            MultiAgentv3 multiAgentv3 = gameObject.GetComponent<MultiAgentv3>();
            InitializeHunted(multiAgentv3);
            gameObject.SetActive(true);
            m_HuntedGroup.RegisterAgent(multiAgentv3);
        }
        else
        {
            if (IdleHunterStack.Count > 0)
            {
                gameObject = IdleHunterStack.Pop();
                gameObject.transform.position = position;
            }
            else
            {
                gameObject = Instantiate(m_PrefavHunter, position, Quaternion.identity, transform);
            }
            MultiAgentv3 multiAgentv3 = gameObject.GetComponent<MultiAgentv3>();
            InitializeHunter(multiAgentv3);
            gameObject.SetActive(true);
            m_HunterGroup.RegisterAgent(multiAgentv3);
            
        }
    }
    public void DespawnAgent(MultiAgentv3 Agent)
    {
        if (Agent.team == Team.Hunted)
        {
            Agent.gameObject.SetActive(false);
            IdleHuntedStack.Push(Agent.gameObject);
        }
        else
        {
            Agent.gameObject.SetActive(false);
            IdleHunterStack.Push(Agent.gameObject);
        }
    }
    // end Agent

    // generator
    public void AddFertilizer(Vector3 position)
    {
        SpawnGenerator(position);
    }
    private void SpawnGenerator(Vector3 position)
    {
        if (IdleGeneratorStack.Count > 0)
        {
            GameObject gameObject = IdleGeneratorStack.Pop();
            gameObject.SetActive(true);
            gameObject.transform.position = position + new Vector3(0,-0.56f,0);
        }
        else
        {
            GameObject gameObject = Instantiate(m_PrefavGenerator, position + new Vector3(0, -0.56f, 0), Quaternion.identity, transform);
            InitializeGenerator(gameObject.GetComponent<Generator>());
        }
    }
    public void DespawnGenerator(GameObject generator)
    {
        generator.SetActive(false);
        IdleGeneratorStack.Push(generator);
    }
    // end generator

    // target
    public void SpawnTarget(Vector3 position)
    {
        if (IdleTargetStack.Count > 0)
        {
            GameObject gameObject = IdleTargetStack.Pop();
            gameObject.SetActive(true);
            gameObject.transform.position = position + new Vector3(5,5,0);
        }
        else
        {
            Instantiate(m_PrefavTarget, position + new Vector3(5, 5, 0), Quaternion.identity, transform);
        }
    }
    public void DespawnTarget(GameObject target)
    {
        target.SetActive(false);
        IdleTargetStack.Push(target);
    }
    // end target

                /////////////
               /// reset ///
              /////////////

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
        //for (int i = 0; i < m_countTarget; i++)
        //{
        //    var index = randomIndices[m_countAgents + i];
        //    var newPosition = SpawnList[index].Vector;
        //    TargetList[i].Target.transform.position = newPosition;
        //    TargetList[i].Target.ResetTarget();
        //    TargetList[i].Rb.velocity = Vector3.zero;
        //}
        // reset scores
        scoreHunted = 0;
        scoreHunter = 0;
    }
}
