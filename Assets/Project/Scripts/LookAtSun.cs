using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtSun : MonoBehaviour
{

    [SerializeField] private Light sun;

    void Update()
    {
        this.transform.forward = -sun.transform.forward;        
    }
}
