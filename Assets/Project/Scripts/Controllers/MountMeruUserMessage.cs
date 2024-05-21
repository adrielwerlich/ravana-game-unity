using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MountMeruUserMessage : MonoBehaviour
{
    public static Action ShowKillAllMonstersMessage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name.Contains("Player"))
        {
            ShowKillAllMonstersMessage?.Invoke();
        }
    }

}
