using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicSpell : MonoBehaviour
{

    public float speed = 10.0f;

    public static Action<Transform> MagicSpellHit;

    void Start()
    {
        Destroy(this.gameObject, 10f);
    }

    void Update()
    {
        this.transform.position += this.transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Magic spell Hit: " + other.name);
        
        // if (other.gameObject.name.Contains("Monster"))
        // {
        //     MagicSpellHit?.Invoke(this.transform);
        //     Destroy(this.gameObject);
        // }
        
    }   

}
