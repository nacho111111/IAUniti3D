using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TargetMulti : MonoBehaviour
{
    //private bool m_move = true;  // se puede mover ?

    private Transform m_goTo;
    private Rigidbody m_rb;
    private SphereCollider m_sc;
    private MultiAgent m_agent;
    private EnvController m_spawn;

    //mover

    private bool m_isMoving = false;
    private Vector3 m_toPoint; // destino
    private Vector3 m_fromPoint; // llegada
    private float m_moveDuration = 3f; // Duración del movimiento
    private float m_elapsedTime = 0f; // Temporizador

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_sc = GetComponent<SphereCollider>();
        m_spawn = gameObject.GetComponentInParent<EnvController>();
    }
    void Update()   // 3 estados, 1 reposo no hace nada espera colicion con "Hunted", 2 colicion con "hunted" lo sigue, 3 "hunted" lo deposita en el area de descarga.
    {
        if (m_goTo != null) // estado 2
        {
            // Sigue al agente con un pequeño desfase en la posición 'z'
            Vector3 followPosition = m_goTo.position - m_goTo.forward * 2.0f; // Ajusta la distancia deseada aquí
            followPosition.y = transform.position.y; // Mantén la altura del objeto que sigue

            transform.position = Vector3.Lerp(transform.position, followPosition, Time.deltaTime * 2.0f); // Ajusta la velocidad de seguimiento aquí
            transform.LookAt(m_goTo); // Opcional: para que el objeto mire al agente
        }

        // mover a
        if (m_isMoving) // estado 3
        {
            m_elapsedTime += Time.deltaTime;
            float t = m_elapsedTime / m_moveDuration;

            // Mueve el objeto de A a B usando Lerp
            transform.position = Vector3.Lerp(m_fromPoint, m_toPoint, t);

            // Verifica si el movimiento ha terminado
            if (t >= 1.0f)
            {
                m_isMoving = false;
                ResetTarget();
                m_spawn.SpawnObject(transform);
            }
        }
    }
    public void FollowTo(Transform other) // activa estado 2
    {
        //if (m_move == true)
        //{
            //m_move = false;
        m_goTo = other;
        m_sc.enabled = false;
        m_rb.useGravity = false;
        m_rb.constraints |= RigidbodyConstraints.FreezePositionY;
        //}
    }
    public void MoveTo(Vector3 newPoint) // activa estado 3
    {
        // separa de hunted
        m_goTo = null;
        // mover
        m_toPoint = newPoint;
        m_fromPoint = transform.position;
        m_isMoving = true;
    }

    public void ResetTarget()
    {
        m_isMoving = false; // ignora animacion estado 3
        //timer
        m_elapsedTime = 0f;
        m_goTo = null;
        //m_move = true;
        //boxcollider - rigidbody
        m_sc.enabled = true;
        m_rb.velocity = Vector3.zero;
        m_rb.useGravity = true;
        m_rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
    }
}
