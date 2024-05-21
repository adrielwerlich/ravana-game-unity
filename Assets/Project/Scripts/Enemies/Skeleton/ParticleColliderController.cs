using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ParticleColliderController : MonoBehaviour
{
    private PlayerScoreEvolutionController playerScoreEvolutionController;
    private bool canTriggerEnter = false;
    private bool alreadyTouched = false;
    private List<ParticleSystem> particleSystems;

    private Transform target;

    void Start()
    {
        playerScoreEvolutionController = GameObject.Find("RavanaPlayer").GetComponent<PlayerScoreEvolutionController>();
        StartCoroutine(EnableTriggerEnterAfterDelay());

        particleSystems = new List<ParticleSystem>();

        // Get all child particle systems
        foreach (Transform child in transform)
        {
            ParticleSystem ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                particleSystems.Add(ps);
            }
        }

        Destroy(this.gameObject, 60f);
    }

    void Update()
    {
        if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, 10f * Time.deltaTime);
        }
    }

    private IEnumerator EnableTriggerEnterAfterDelay()
    {
        yield return new WaitForSeconds(3);
        canTriggerEnter = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (canTriggerEnter && other.gameObject.name.Contains("Ravana") && !alreadyTouched)
        {
            alreadyTouched = true;
            playerScoreEvolutionController.IncreaseScore(10f);
            target = other.transform;

            StartCoroutine(ReduceStartSize());

        }
    }

    private IEnumerator ReduceStartSize()
    {
        float duration = .6f; // Duration of the reduction
        float elapsed = 0f; // Time elapsed since the start of the reduction

        // Get the initial start sizes
        List<float> initialStartSizes = new List<float>();
        foreach (ParticleSystem ps in particleSystems)
        {
            initialStartSizes.Add(ps.main.startSize.constant);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Calculate the new start size

            // Set the new start size
            for (int i = 0; i < particleSystems.Count; i++)
            {
                float newStartSize = Mathf.Lerp(initialStartSizes[i], 0, elapsed / duration);
                var main = particleSystems[i].main;
                main.startSize = newStartSize;
            }

            yield return null;
        }

        // Ensure the start size is 0
        foreach (ParticleSystem ps in particleSystems)
        {
            var main = ps.main;
            main.startSize = 0;
        }

        StartCoroutine(DestroyAfterDelay(3.5f));

    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(this.gameObject);
    }
}
