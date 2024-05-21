using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OuterPerimeter : MonoBehaviour
{
    [SerializeField] private GameObject brahma;

    private void OnTriggerStay(Collider other) {
        if (other.gameObject.name.Contains("Player"))
        {
            brahma.transform.LookAt(other.transform);
        }
    }
}
