using System.Collections;
using System.Collections.Generic;
using RavanaGame;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using System;
using Random = UnityEngine.Random;

public class MonsterController : MonoBehaviour
{

    private float availableHits = 0;
    [SerializeField] private Transform player; // Reference to the player's transform
    public float lookRadius = 20f; // Detection radius for player
    public float attackRadius = 6f; // Attack radius

    NavMeshAgent agent;
    Animator animator;
    bool isWaiting = false;

    private Vector3 initialPosition;
    private float movementRadius;
    private float rotationSpeed;

    public bool isAttacking = false;

    [SerializeField] private GameObject particleSystem_boost;

    private Rigidbody rb;
    [SerializeField] private GameObject hitEffect;

    public static Action MonsterDied;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.speed = Random.Range(6f, 12f);
        animator = GetComponent<Animator>();

        movementRadius = Random.Range(15f, 40f);
        rotationSpeed = Random.Range(4f, 8f);

        initialPosition = transform.position;

        availableHits = Random.Range(4, 15);

        rb = GetComponent<Rigidbody>();

        critData.hideFlags = HideFlags.None;
        textData.hideFlags = HideFlags.None;

    }


    private void MagicSpellHit(Transform spellTransform)
    {
        GetHit(spellTransform);
    }


    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= lookRadius && !isWaiting && !underAttack)
        {
            if (agent != null && agent.enabled && gameObject.activeSelf)
            {
                agent.SetDestination(player.position);
                animator.SetBool("move", true);
            }

            if (distance <= attackRadius)
            {
                AttackPlayer();
            }
            else
            {
                animator.SetBool("attack", false);
                isAttacking = false;
            }
        }
        else if (!agent.pathPending && agent.remainingDistance < 0.1f && !isWaiting)
        {
            StartCoroutine(WaitAndMoveAround());
        }
    }

    IEnumerator WaitAndMoveAround()
    {
        isWaiting = true;
        animator.SetBool("move", false);

        yield return new WaitForSeconds(Random.Range(4, 6));
        agent.SetDestination(initialPosition + new Vector3(
            Random.Range(-movementRadius, movementRadius),
            0,
            Random.Range(-movementRadius, movementRadius)
        ));
        isWaiting = false;
        animator.SetBool("move", true);
        animator.SetBool("attack", false);
        animator.SetBool("getHit", false);
        isAttacking = false;
    }

    void AttackPlayer()
    {
        isAttacking = true;
        animator.SetBool("move", false);
        animator.SetBool("attack", true);
        animator.SetBool("getHit", false);

        // Rotate to face the player
        Vector3 directionToPlayer = player.position - transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    private bool underAttack = false;
    [SerializeField] RavanaPlayerController ravanaPlayerController;
    [SerializeField] private DynamicTextData critData;
    [SerializeField] private DynamicTextData textData;

    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioSource audioSource;

    private void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.name == "RavanaSword" && ravanaPlayerController.isSwordAttack) || other.gameObject.name.Contains("Spell") && !underAttack)
        {
            GetHit(other.transform);
        }
        if (availableHits <= 0)
        {

            var particleSystem = Instantiate(
                particleSystem_boost,
                new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z),
                Quaternion.identity
            );

            // Scale down to half size
            particleSystem.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Move the particle system up and down to create a bounce effect
            particleSystem.transform.DOJump(
                particleSystem.transform.position + new Vector3(
                Random.Range(-15f, 15f),
                Random.Range(-1f, 1f),
                Random.Range(-15f, 15f)
                ),
                Random.Range(4f, 10f),
                Random.Range(3, 12),
                Random.Range(.2f, .5f)
            ).OnComplete(() =>
            {
                // Move the particle system a little farther away
                particleSystem.transform.DOMove(
                    particleSystem.transform.position + new Vector3(
                        Random.Range(-15f, 15f),
                        0,
                        Random.Range(-15f, 15f)
                    ),
                    .5f
                );
            });

            Destroy(this.gameObject);

        }
    }

    private void GetHit(Transform other)
    {
        animator.SetBool("getHit", true);
        animator.SetBool("move", false);
        animator.SetBool("attack", true);
        underAttack = true;

        audioSource.PlayOneShot(hitSound);
        Vector3 hitDirection = GetHitDirection(other);
        ApplyHitEffect(hitDirection);


        var destination = this.transform.position + new Vector3(0, 2, 0);
        destination.x += (Random.value - 0.5f) / 3f;
        if (this.gameObject.name.Contains("Brown"))
        {
            destination.x += 3.5f;
        }
        else
        {
            // is black
            destination.y += 2.5f;
        }
        destination.z += (Random.value - 0.5f) / 3f;
        availableHits--;

        if (availableHits > 0)
        {
            DynamicTextManager.CreateText(destination, "Hit! " + availableHits.ToString(), critData);
        }
        else
        {
            destination.y += 2f;
            DynamicTextManager.CreateText(destination, "Enemy Destroyed", textData);
            MonsterDied?.Invoke();
        }
        StartCoroutine(ResetUnderAttack());
    }

    [SerializeField] private float underAttackTimer = .6f;
    IEnumerator ResetUnderAttack()
    {
        yield return new WaitForSeconds(underAttackTimer);
        underAttack = false;
    }

    private Vector3 GetHitDirection(Transform other)
    {
        Vector3 hitDirection = transform.position - other.transform.position;
        hitDirection.Normalize();
        return hitDirection;
    }

    [SerializeField] private float rangeMin = 100;
    [SerializeField] private float rangeMax = 500;
    private void ApplyHitEffect(Vector3 hitDirection)
    {
        Vector3 position = new Vector3(
                            this.transform.position.x,
                            this.transform.position.y + 1,
                            this.transform.position.z
                            );

        var explosion = Instantiate(
                hitEffect,
                position,
                Quaternion.identity
        );
        if (availableHits > 0)
        {
            explosion.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        }
        else
        {
            explosion.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
        Destroy(explosion, 1f);
        if (rb != null)
        {
            float forceMultiplier = Random.Range(rangeMin, rangeMax);
            float startForce = 0f;
            float endForce = forceMultiplier;
            float duration = .2f; // Duration of the animation in seconds

            DOVirtual.Float(startForce, endForce, duration, value =>
            {
                rb.AddForce(hitDirection * value, ForceMode.Impulse);
            });
        }

        StartCoroutine(WaitAndReset());
    }

    private IEnumerator WaitAndReset()
    {
        yield return new WaitForSeconds(.5f);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
