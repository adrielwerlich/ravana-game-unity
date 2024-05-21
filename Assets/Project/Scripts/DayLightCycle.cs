using System;
using System.Collections;
using System.Collections.Generic;
using RavanaGame;
using UnityEngine;
using UnityEngine.Rendering;

public class DayLightCycle : MonoBehaviour
{

    public static DayLightCycle Instance { get; private set; }

    [SerializeField] private float dayTimeMultiplier = 1f;

    [SerializeField] private HealthAndStrengthController playerStrengthAndHealthController;
    [SerializeField] private GameObject particleSystem1;
    [SerializeField] private GameObject particleSystem2;
    [SerializeField] private Volume burningPostProcessingVolume;

    [SerializeField] private LevelController levelController;

    private Animator dayNightCycleAnimator;

    public float dayTime = 1f;
    public float dayTimeMax = 24f;
    private float sunlightDamageTimer;
    public float sunlightDamageTimerMax = 8f;

    private bool active = true;

    AnimationClip clip;

    private float startFrame;
    private float totalFrames;
    private float startPosition;

    private void Awake()
    {
        Instance = this;

        dayNightCycleAnimator = this.transform.Find("DayNightCycleDirectionalLight").GetComponent<Animator>();
        dayNightCycleAnimator.SetFloat("DayTimeMultiplier", dayTimeMultiplier);

        clip = dayNightCycleAnimator.runtimeAnimatorController.animationClips[0];

        totalFrames = clip.frameRate * clip.length;

        InitMidDay();

        // InitMidNight();
    }

    void InitMidDay()
    {
        dayTime = 12f;
        startFrame = 721;
        startPosition = startFrame / totalFrames;

        dayNightCycleAnimator.Play("DayNightCycle_DirectionalLight", 0, startPosition);
    }

    void InitMidNight()
    {
        dayTime = 0f;
        dayNightCycleAnimator.Play("DayNightCycle_DirectionalLight", 0, 0);
    }

    private IEnumerator DayNightCycle()
    {
        while (true)
        {
            dayNightCycleAnimator.enabled = true;
            active = true;
            // Debug.Log("DayNightCycle() enabled");
            yield return new WaitForSeconds(5);
            dayNightCycleAnimator.enabled = false;
            active = false;
            // Debug.Log("DayNightCycle() disabled");
            yield return new WaitForSeconds(15);
        }
    }

    void Update()
    {
        dayTime += Time.deltaTime * dayTimeMultiplier;
        dayTime = dayTime % dayTimeMax;
        TrySunLightDamage();
    }

    private float particleSystem1Timer = 5f;
    private float particleSystem2Timer = 15f;

    private int reduceStrengthCount = 1;

    private void TrySunLightDamage()
    {
        if (IsDaytime()
        && CheckSunlightCamera.Instance.IsCatchingSunlight()
        )
        {
            sunlightDamageTimer += Time.deltaTime;

            if (particleSystem1.activeSelf == false)
            {
                particleSystem1Timer += Time.deltaTime;
                // particleSystem1.SetActive(sunlightDamageTimer >= sunlightDamageTimerMax * .15f);
            }

            if (particleSystem2.activeSelf == false)
            {
                particleSystem2Timer += Time.deltaTime;
                // particleSystem2.SetActive(sunlightDamageTimer >= sunlightDamageTimerMax * .5f);
            }

            if (sunlightDamageTimer >= sunlightDamageTimerMax)
            {
                playerStrengthAndHealthController.ReduceStrength(reduceStrengthCount);
                reduceStrengthCount++;
                sunlightDamageTimer = 0f;

                if (levelController.missionsTextPanel.activeSelf == false && !levelController.hideWarning)
                {
                    // toggle on after debugging - REMEMBER
                    // levelController.missionsTextPanel.SetActive(true);
                    // levelController.SetAvoidSunlightText();
                }
            }
        }
        else
        {
            playerStrengthAndHealthController.IncreaseStrength(reduceStrengthCount);
            if (reduceStrengthCount > 1)
            {
                reduceStrengthCount--;
            }
            sunlightDamageTimer = 0f;
            particleSystem1Timer = 0f;
            particleSystem2Timer = 0f;
            particleSystem1.SetActive(false);
            particleSystem2.SetActive(false);
        }
    }

    public int GetHour()
    {
        return Mathf.FloorToInt(dayTime);
    }

    public float GetDayTimeNormalized()
    {
        return dayTime / dayTimeMax;
    }

    private bool IsDaytime()
    {
        return GetHour() > 5 && GetHour() < 18f;
    }
}
