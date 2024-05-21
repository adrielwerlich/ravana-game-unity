using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PortalToBrahmaController : MonoBehaviour
{

    private GameObject[] monsters;
    private int monstersKilled = 0;

    private GameObject plataform;
    
    void Start()
    {
        monsters = GameObject.FindObjectsOfType<GameObject>()
        .Where(go => go.name.Contains("CustomMonster"))
        .ToArray();

        Debug.Log("Monsters: " + monsters.Length);

        plataform = this.gameObject.transform.Find("PortalContainer").gameObject;
        plataform.SetActive(false);

    }

    private void OnEnable()
    {
        MonsterController.MonsterDied += OnMonsterDied;
    }

    private void OnMonsterDied()
    {
        monstersKilled++;
        Debug.Log("monstersKilled: " + monstersKilled);
        if (monstersKilled == monsters.Length)
        {
            plataform.SetActive(true);
        }
    }

}
