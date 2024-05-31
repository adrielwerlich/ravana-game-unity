using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using RavanaGame;
using UnityEngine;

public class InnerPerimeter : MonoBehaviour
{
    [SerializeField] private AudioClip RavanaPrayersAskingBlessings;
    [SerializeField] private AudioClip BrahmasReply;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private GameObject brahma;

    private bool isPlaying = false;

    public static Action CloseToBrahmaEvent;
    public static Action BrahmaBlessingFinishedEvent;


    [SerializeField] CinemachineVirtualCamera prayerAnimationCamera;
    [SerializeField] Animator prayerAnimationCameraAnimator;
    [SerializeField] private RavanaPlayerController ravanaPlayerController;
    [SerializeField] GameObject mountMeruPortal;

    private Camera mainCamera;

    Cinemachine.CinemachineBrain brain;

    Vector3 originalPosition;
    Vector3 originalRotation;

    public static Action GoBackToMountMeru;

    void Start()
    {
        mainCamera = Camera.main;
        brain = mainCamera.GetComponent<Cinemachine.CinemachineBrain>();
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name.Contains("Ravana") && !isPlaying)
        {
            audioSource.pitch = 1f;
            isPlaying = true;
            CloseToBrahmaEvent?.Invoke();
            audioSource.PlayOneShot(RavanaPrayersAskingBlessings, 1f);
            LockAndSetCameraPosition();
            StartCoroutine(CheckIfAudioFinished(audioSource, "ravana-speaking"));
        }
    }

    private void LockAndSetCameraPosition()
    {
        originalPosition = mainCamera.transform.localPosition;
        originalRotation = mainCamera.transform.eulerAngles;

        //brain.enabled = false;

        //mainCamera.transform.LookAt(brahma.transform.Find("BrahmaLookAtPoint").transform);
        //mainCamera.transform.localPosition = new Vector3(
        //    mainCamera.transform.localPosition.x, 
        //    mainCamera.transform.localPosition.y - 5, 
        //    mainCamera.transform.localPosition.z + 3);

        //Vector3 rotation = new Vector3(338.959991f,357,-5.71725387e-08f);
        //mainCamera.transform.eulerAngles = rotation;
        //ravanaPlayerController.LockCameraPosition = true;

        prayerAnimationCamera.Priority = 11;
        StartCoroutine(PlayAnimationAndWait());

    }

    IEnumerator PlayAnimationAndWait()
    {
        prayerAnimationCameraAnimator.SetBool("prayerAnimation", true);
        yield return new WaitForSeconds(prayerAnimationCameraAnimator.GetCurrentAnimatorStateInfo(0).length);
    }

    IEnumerator CheckIfAudioFinished(AudioSource audioSource, string audioName)
    {
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        if (audioName == "ravana-speaking")
        {
            audioSource.PlayOneShot(BrahmasReply, 1f);
            StartCoroutine(CheckIfAudioFinished(audioSource, "brahma-speaking"));
        }
        else if (audioName == "brahma-speaking")
        {
            BrahmaBlessingFinishedEvent?.Invoke();
            prayerAnimationCamera.Priority = 9;
            isPlaying = true;

            Sequence sequence = DOTween.Sequence();

            // Move the object upwards
            sequence.Append(brahma.transform.DOMove(brahma.transform.position + new Vector3(0, 10, 0), 1f));

            // Fade out the object
            var renderer = brahma.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                Color color = material.color;
                color.a = 0f;
                sequence.Append(material.DOColor(color, 1f));
            }

            // Scale the object down
            sequence.Append(brahma.transform.DOScale(0, 1f));

            // Deactivate the object after the animations are finished
            sequence.OnComplete(() =>
            {
                brahma.SetActive(false);
                prayerAnimationCameraAnimator.SetBool("prayerAnimation", false);
                
                StartCoroutine(WaitAndExecute());
            });

        }
    }

    IEnumerator WaitAndExecute()
    {
        // Wait for 1 second
        yield return new WaitForSeconds(1f);

        ResetCamera();
        GoBackToMountMeru?.Invoke();
        //ravanaPlayerController.TeleportTo(backToMountMeruPosition);
        mountMeruPortal.SetActive(false);
        ravanaPlayerController.playerControllerPublicProperties.LockCameraPosition = false;
    }

    private void ResetCamera()
    {
        mainCamera.transform.localPosition = originalPosition;
        mainCamera.transform.eulerAngles = originalRotation;
        brain.enabled = true;
    }
}
