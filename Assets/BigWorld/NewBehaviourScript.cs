using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnDrawGizmos()
    {
        Debug.LogError("NewBehaviourScript");
        Gizmos.DrawWireSphere(Vector3.one,3f);
    }
}
