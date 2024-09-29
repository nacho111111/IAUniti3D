using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Targetv3 : MonoBehaviour
{
    private Rigidbody m_rb;
    private void Start()
    {
        m_rb = GetComponent<Rigidbody>();
    }
    public void ResetTarget()
    {
        m_rb.velocity = Vector3.zero;
        m_rb.angularVelocity = Vector3.zero;
    }
}
