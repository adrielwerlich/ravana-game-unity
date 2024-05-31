using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtSun : MonoBehaviour
{

    [SerializeField] private Light sun;

    void Update()
    {
        if (sun != null) {
            this.transform.forward = -sun.transform.forward;        
        }
    }
}
