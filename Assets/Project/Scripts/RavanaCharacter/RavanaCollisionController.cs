using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using RavanaGame;
using Unity.VisualScripting;
using UnityEngine;


public class RavanaCollisionController : MonoBehaviour
{
    private Animator animator;
    private bool isHit = false;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gotHurtAudioClip;
    [SerializeField] private AudioClip deadSound;

    HealthAndStrengthController healthAndStrengthController;

    private RavanaPlayerController ravanaPlayerController;

    [SerializeField] private GameObject deathExplosionEffect;
    void Start()
    {
        animator = GetComponent<Animator>();
        ravanaPlayerController = GetComponent<RavanaPlayerController>();
        healthAndStrengthController = GetComponent<HealthAndStrengthController>();

        audioSource = GameObject.Find("MainAudioSource").GetComponent<AudioSource>();
        gotHurtAudioClip = Resources.Load<AudioClip>("Audio/Male_Hurt_04");
        deadSound = Resources.Load<AudioClip>("Audio/Male_Nooo_02");

        deathExplosionEffect = Resources.Load<GameObject>("Prefabs/PlayerDieExplosionEffect");

    }

    private void OnEnable() {
        InnerPerimeter.CloseToBrahmaEvent += PlayPrayingAnimation;
        InnerPerimeter.BrahmaBlessingFinishedEvent += GoAway;
    }

    private void GoAway()
    {
        animator.SetBool("Praying", false);
        ravanaPlayerController.canMove = true;

    }

    private void PlayPrayingAnimation()
    {
        animator.SetBool("Praying", true);
        ravanaPlayerController.canMove = false;
    }

    void Update()
    {

    }

    //private void OnTriggerStay(Collider other) {
    //    if (
    //        other.gameObject.name == "SkeletonSword"
    //        && other.gameObject.GetComponent<SkeletonSword>().skeletonController.isAttacking
    //        && isHit
    //    )
    //    {
    //        isFirstHit = false;
    //    }
    //}

    //void OnTriggerExit(Collider other)
    //{
    //    if (
    //        other.gameObject.name == "SkeletonSword"
    //        && other.gameObject.GetComponent<SkeletonSword>().skeletonController.isAttacking
    //        && isHit
    //    )
    //    {
    //        isFirstHit = true;
    //    }
    //}

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("ravana player colide with => " + other.gameObject.name);
        SwordHit(other);
    }


    void SwordHit(Collider other)
    {
        if (
            !isDead
            && other.gameObject.name.Contains("CustomMonster")
            || other.gameObject.name == "SkeletonSword"
            && other.gameObject.GetComponent<SkeletonSword>().skeletonController.isAttacking
            && !isHit
        )
        {
            // AnimationLogic();
            //if (isFirstHit)
            //{
            //    isFirstHit = false;
            //    StartCoroutine(PlayAnimationAfterDelay(1f));
            //}
            //else
            //{
            //}
            AnimationLogic();
            audioSource.PlayOneShot(gotHurtAudioClip, 1f);
        }
    }

    IEnumerator PlayAnimationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        AnimationLogic();
    }

    private bool isDead = false;

    public static Action PlayerIsDead;


    void AnimationLogic()
    {
        if (healthAndStrengthController.Strength > 0)
        {
            healthAndStrengthController.ReduceStrength(1);
            if (healthAndStrengthController.Strength <= 0)
            {
                StartCoroutine(PlayDeathAnimationAndWait());
                audioSource.PlayOneShot(deadSound, 1f);
                ravanaPlayerController.canMove = false;
                isDead = true;
            } else
            {
                animator.SetBool("GotHit", true);
                isHit = true;
                StartCoroutine(GotHitAnimationEnd(1f));
            }
        }
    }

    IEnumerator PlayDeathAnimationAndWait()
    {
        animator.SetBool("IsDead", true);
        yield return new WaitForSeconds(2);
        this.gameObject.transform.DOScale(0, 1f);
        
        this.gameObject.transform.position = new Vector3(transform.position.x, transform.position.y + 2, transform.position.z);
        var go = Instantiate(deathExplosionEffect, transform.position, Quaternion.identity);
        StartCoroutine(DeactivateAfterDelay());

        Destroy(go, 2);
    }

    IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        PlayerIsDead?.Invoke();
    }

    IEnumerator GotHitAnimationEnd(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("GotHit", false);
        isHit = false;
    }


}
