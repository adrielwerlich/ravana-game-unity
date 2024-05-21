using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private AudioClip attackAndHitAudioClip;
    [SerializeField] private AudioSource audioSource;

    // public static event Action hitEnemyEvent;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other) {
        // Debug.Log("Sword OnTriggerEnter =>" + other.gameObject.name);
        // audioSource.PlayOneShot(attackAndHitAudioClip, 1f);
    }
}
