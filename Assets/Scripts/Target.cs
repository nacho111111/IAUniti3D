using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Target : MonoBehaviour
{
    private bool move = true;

    private Transform goTo;
    public Rigidbody rb;
    private BoxCollider bc;
    private AgentML agent;
    private EsenarySpawn Spawn;

    //mover

    //bool isMoving = false;
    private Transform toPoint; // destino
    private Vector3 fromPoint; // llegada
    //private float moveDuration = 3f; // Duración del movimiento
    //private float elapsedTime = 0f; // Temporizador

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        bc = GetComponent<BoxCollider>();
        Spawn = transform.parent.gameObject.GetComponent<EsenarySpawn>();
        
    }
    void Update()
    {
        if (goTo != null)
        {
            // Sigue al agente con un pequeño desfase en la posición 'z'
            Vector3 followPosition = goTo.position - goTo.forward * 2.0f; // Ajusta la distancia deseada aquí
            followPosition.y = transform.position.y; // Mantén la altura del objeto que sigue

            transform.position = Vector3.Lerp(transform.position, followPosition, Time.deltaTime * 2.0f); // Ajusta la velocidad de seguimiento aquí
            transform.LookAt(goTo); // Opcional: para que el objeto mire al agente
        }

        // segundo entrenamiento 
        //if (isMoving)
        //{
        //    elapsedTime += Time.deltaTime;
        //    float t = elapsedTime / moveDuration;

        //    // Mueve el objeto de A a B usando Lerp
        //    transform.position = Vector3.Lerp(fromPoint, toPoint.position, t);

        //    // Verifica si el movimiento ha terminado
        //    if (t >= 1.0f)
        //    {
        //        isMoving = false;
        //    }
        //}
    }
    void OnCollisionEnter(Collision other)
    {
        if (move == true && other.gameObject.CompareTag("agent"))
        {
            agent = other.gameObject.GetComponent<AgentML>();

            goTo = other.transform;
            bc.enabled = false;
            rb.useGravity = false;
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
            agent.Transporting = true;
            agent.Target = gameObject;
        }
    }
    public void MoveTo(Transform newPoint)
    {
        goTo = null;
        bc.enabled = true;
        rb.useGravity = true;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;

        // segundo entrenamiento 

        //toPoint = newPoint;
        //fromPoint = transform.position;
        //isMoving = true;
        //move = false;
        //gameObject.tag = "Untagged";  

    }
}
