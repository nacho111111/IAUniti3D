using Palmmedia.ReportGenerator.Core;
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

    [SerializeField] private Color m_colorHuntedState;
    [SerializeField] private Color m_colorHunterState;

    [SerializeField, Range(1, 5)] private float m_MovSpeedHunted = 2;
    [SerializeField, Range(1, 5)] private float m_MovSpeedHunter = 2;

    [SerializeField, Range(10, 200)] private float m_MaxEnergyHunted = 100;
    [SerializeField, Range(10, 200)] private float m_MaxEnergyHunter = 150;

    [SerializeField, Range(0.1f, 1f)] private float m_LossEnergyHunted = 0.1f;
    [SerializeField, Range(0.1f, 1f)] private float m_LossEnergyHunter = 0.1f;

    [Tooltip("Cada cuantas comidas se reproduce el hunted"), SerializeField, Range(1, 10)] private int m_repRateHunted = 2;
    [Tooltip("Cada cuantas comidas se reproduce el hunter"), SerializeField, Range(1, 10)] private int m_repRateHunter = 2;

    [Tooltip("Tiempo entre comidas del hunted"), SerializeField, Range(0, 10)] private float m_TimerStateHunted = 2;
    [Tooltip("Tiempo entre comidas del hunter"), SerializeField, Range(0, 10)] private float m_TimerStateHunter = 2;

    private SimpleMultiAgentGroup m_HuntedGroup;
    private SimpleMultiAgentGroup m_HunterGroup;

    private int m_ResetTimer;
    [Header("Generator")]

    [SerializeField, Range(5, 30)] private float m_GrowthTime;
    [SerializeField, Range(10, 120)] private float m_DespawnTime;
    [SerializeField, Range(1, 10)] private int m_FoodSpawnRate;
    [SerializeField, Range(1, 20)] private int m_SpawnConut;
    [SerializeField, Range(1, 10)] private int m_GeneratorSpawnRate;
    [SerializeField] private Material m_PostMaterial;
    
    [Header("Listas")]

    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    //public List<TargetInfo> TargetList = new List<TargetInfo>();
    public List<SpawnInfo> SpawnList = new List<SpawnInfo>();
    private Stack<GameObject> IdleHuntedStack = new Stack<GameObject>();
    private Stack<GameObject> IdleHunterStack = new Stack<GameObject>();
    private Stack<GameObject> IdleTargetStack = new Stack<GameObject>();
    private Stack<GameObject> IdleGeneratorStack = new Stack<GameObject>();
    private Stack<GameObject> IdleFertilizerStack = new Stack<GameObject>();

    [Header("Prefavs")]

    [SerializeField] private GameObject m_PrefavHunted;
    [SerializeField] private GameObject m_PrefavHunter;
    [SerializeField] private GameObject m_PrefavGenerator;
    private float prefavGeneratorY;
    [SerializeField] private GameObject m_PrefavTarget;
    [SerializeField] private GameObject m_PrefavFertilizer;



    private int m_countAgents;
    private int m_countTarget;
    private int m_countSpawns;

    private int scoreHunted = 0;
    private int scoreHunter = 0;
    private void OnValidate()
    {
        if (m_GrowthTime >= m_DespawnTime - 10)
        {
            m_GrowthTime = m_DespawnTime - 10;
            Debug.LogWarning("El tiempo de despawn tiene que ser considerablemente mayor al de crecimiento. Se ha ajustado automáticamente.");
        }
    }
             //////////////////
            /// Initialize ///
           //////////////////

    void Start()
    {
        // el colicionador de "unloadingArea" no se aplica para "hunted", ya que este simula un escodite para estos 
        //Physics.IgnoreLayerCollision(6, 7);

        // Initialize count
        m_countAgents = AgentsList.Count;
        m_countSpawns = SpawnList.Count;
        //m_countTarget = TargetList.Count;

        prefavGeneratorY = m_PrefavGenerator.transform.position.y; // evita hacer esta consulta constantemente

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
    //void FixedUpdate()
    //{
    //    m_ResetTimer += 1;
    //    if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
    //    {
    //        m_HuntedGroup.GroupEpisodeInterrupted();
    //        m_HunterGroup.GroupEpisodeInterrupted();
    //        ResetScene();
    //    }
    //}
    private void InitializeHunted(MultiAgentv3 hunted)
    {
        hunted.colorState = m_colorHuntedState;
        hunted.MaxEnergy = m_MaxEnergyHunted;
        hunted.Energy = m_MaxEnergyHunted;
        hunted.LossEnergy = m_LossEnergyHunted;
        hunted.MovSpeed = m_MovSpeedHunted;
        hunted.reproductionRate = m_repRateHunted;
        hunted.IntervalTimerState = m_TimerStateHunted;
    }
    private void InitializeHunter(MultiAgentv3 hunter)
    {
        hunter.colorState = m_colorHunterState;
        hunter.LossEnergy = m_LossEnergyHunter;
        hunter.MaxEnergy = m_MaxEnergyHunter;
        hunter.Energy = m_MaxEnergyHunter;
        hunter.MovSpeed = m_MovSpeedHunter;
        hunter.reproductionRate = m_repRateHunter;
        hunter.IntervalTimerState = m_TimerStateHunter;
    }
    public void InitializeGenerator(Generatorv2 generator)
    {
        generator.DespawnTime = m_DespawnTime;
        generator.GrowthTime = m_GrowthTime;
        generator.SpawnRate = m_FoodSpawnRate;
        generator.SpawnCount = m_SpawnConut;
        generator.PostMaterial = m_PostMaterial;
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
                //ResetScene();
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
                //ResetScene();
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

    // fertilizer
    public void AddFertilizer(Vector3 position)
    {
        int ran = Random.Range(0, m_GeneratorSpawnRate);
        if (ran == 0)
        {
            SpawnGenerator(position);
        }
        SpawnFertilizer(position);
    }
    private void SpawnFertilizer(Vector3 position)
    {
        if (IdleFertilizerStack.Count > 0)
        {
            GameObject gameObject = IdleFertilizerStack.Pop();
            gameObject.SetActive(true);
            //gameObject.transform.position = position + new Vector3(0,-0.56f,0);
            gameObject.transform.position = position;
        }
        else
        {
            GameObject gameObject = Instantiate(m_PrefavFertilizer, position, Quaternion.identity, transform);
        }
    }
    public void DespawnFertilizer(GameObject Fertilizer)
    {
        Fertilizer.SetActive(false);
        IdleFertilizerStack.Push(Fertilizer);
    }
    // end fertilizer

    // generator
    private void SpawnGenerator(Vector3 position)
    {
        if (IdleGeneratorStack.Count > 0)
        {
            GameObject gameObject = IdleGeneratorStack.Pop();
            gameObject.SetActive(true);
            //gameObject.transform.position = position + new Vector3(0,-0.56f,0);
            gameObject.transform.position = new Vector3(position.x, prefavGeneratorY, position.z);
        }
        else
        {

            GameObject gameObject = Instantiate(m_PrefavGenerator, new Vector3(position.x, prefavGeneratorY, position.z), Quaternion.identity, transform);
            

            //InitializeGenerator(gameObject.GetComponent<Generatorv2>()); // se llama en generator par evitar errores en el tiempo de ejecucion
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
        int ran1 = Random.Range(-5, 5);
        int ran2 = Random.Range(-5, 5);
        if (IdleTargetStack.Count > 0)
        {
            GameObject gameObject = IdleTargetStack.Pop();
            gameObject.SetActive(true);
            gameObject.transform.position = position + new Vector3(ran1, 5, ran2);
        }
        else
        {
            Instantiate(m_PrefavTarget, position + new Vector3(ran1, 5, ran2), Quaternion.identity, transform);
        }
    }
    public void DespawnTarget(GameObject target)
    {
        target.GetComponent<Targetv3>().ResetTarget();
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
