using System;
using System.Collections;
using System.Collections.Generic;
using RavanaGame;
using UnityEngine;

public class TeleportToBrahma : MonoBehaviour
{
    [SerializeField] private Transform destination;

    public static Action HideArrow;

    private void OnTriggerEnter(Collider collider)
    {
        // Debug.Log("Teleporter OnTriggerEnter =>" + collider.gameObject.name);
        if (collider.gameObject.name.Contains("RavanaPlayer"))
        {
            StartCoroutine(CheckPosition(collider));
        }
    }

    float tolerance = 2f;

    private IEnumerator CheckPosition(Collider collider)
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // Wait for 100 ms

            if (Vector3.Distance(collider.transform.position, destination.position) > tolerance)
            {
                // Debug.Log("collider is not at destination, teleporting again");
                collider.transform.position = destination.position;
                HideArrow?.Invoke();
            }
            else
            {
                // Debug.Log("collider is at destination");
                break;
            }
        }
    }
}
