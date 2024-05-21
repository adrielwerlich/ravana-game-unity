using System;
using System.Collections;
using System.Collections.Generic;
using RavanaGame;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [SerializeField] Transform backToMountMeruPosition;

    private Transform player;

    private void Start()
    {
        player = GameObject.Find("RavanaPlayer").transform;
    }


    private void OnEnable()
    {
        InnerPerimeter.GoBackToMountMeru += TeleportToMountMeru;
    }

    private void OnDisable()
    {
        InnerPerimeter.GoBackToMountMeru -= TeleportToMountMeru;
    }

    private void TeleportToMountMeru()
    {
            StartCoroutine(Teleport());
    }

    float tolerance = 2f;

    private IEnumerator Teleport()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // Wait for 100 ms

            if (Vector3.Distance(player.transform.position, backToMountMeruPosition.position) > tolerance)
            {
                player.transform.position = backToMountMeruPosition.position;
            }
            else
            {
                break;
            }
        }
    }
}
