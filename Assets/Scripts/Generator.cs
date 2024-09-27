using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Generator : MonoBehaviour
{
    private float m_Timer = 0;
    public float RealDespawnTime;
    public float DespawnTime;
    public float RealGenerateTime;
    public float GenerateTime;
    public float GrowthTime;
    public float RealGrowthTime ;
    private bool grows = true;
    public int SpawnRate;

    private EnvControllerv3 m_EnvController;

    [SerializeField] private GameObject m_Plant;

    private Material m_PreMaterial;
    public Material PostMaterial;
    private Renderer m_Renderer;

    private void Start()
    {
        m_EnvController = GetComponentInParent<EnvControllerv3>();

        m_Renderer = m_Plant.GetComponent<Renderer>();

        m_PreMaterial = m_Renderer.material;

    }
    private void Update()
    {
        m_Timer += Time.deltaTime;
        if (m_Timer >= RealGrowthTime && grows == true)
        {
            PlantGrows();
            return;
        }
        if (m_Timer <= GrowthTime * 3)
        {
            return;
        }

        if (m_Timer >= RealGenerateTime)
        {
            int ran = Random.Range(0, SpawnRate);
            if (ran == 0)
            {
                m_EnvController.SpawnTarget(transform.position);
            }
            RealGenerateTime += GenerateTime;
        }
        if (m_Timer >= RealDespawnTime)
        {
            m_EnvController.DespawnGenerator(gameObject);
            ResetGenerator();
        }
    }
    private void PlantGrows()
    {
        if (m_Timer <= GrowthTime * 3)
        {
            RealGrowthTime += GrowthTime;
            Vector3 endScale = m_Plant.transform.localScale * 2;
            StartCoroutine(ScaleOverTime(m_Plant.transform,endScale, 5f));
        }
        else
        {
            grows = false;
            StartCoroutine(ScaleOverTime(m_Plant.transform,Vector3.one, 5f));
            m_Renderer.material = PostMaterial;
            //StartCoroutine(ChangeMaterialOverTime(m_Renderer, m_PostMaterial,5f));
            RealGenerateTime += m_Timer;
        } 
    }
    private void ResetGenerator()
    {
        RealDespawnTime = DespawnTime;
        RealGenerateTime = GenerateTime;
        m_Timer = 0;

        grows = true;
        m_Renderer.material = m_PreMaterial;
        m_Plant.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
    }


    IEnumerator ScaleOverTime(Transform target, Vector3 endScale, float time)
    {
        Vector3 startScale = target.transform.localScale;
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            target.transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        target.transform.localScale = endScale;
    }
    IEnumerator ChangeMaterialOverTime(Renderer target, Material endMaterial, float time)
    {
        Material startMaterial = target.material;
        float elapsedTime = 0f;

        while (elapsedTime < time)
        {
            target.material.Lerp(startMaterial, endMaterial, elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        //target.material = endMaterial;
    }
}
