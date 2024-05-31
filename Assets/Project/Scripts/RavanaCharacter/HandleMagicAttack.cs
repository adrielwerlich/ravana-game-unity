using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HandleMagicAttack : MonoBehaviour
{

    private bool isHoldingRightMouseButton = false;
    private float holdStartTime;
    private RavanaInputActions ravanaInputActions;
    private bool isMagicAttack = false;
    private bool magicAnimationOn = false;
    private LevelController levelController;
    private Animator animator;
    private AudioSource audioSource;
    private AuraController auraController;
    private PlayerScoreEvolutionController playerScoreController;
    private Material MammonSigilMaterial;
    private Material PaimonSigilMaterial;
    private GameObject SpellPositions;
    private int specialSpellRandomIndex;
    private bool holdingRandomMaterial = false;

    private float elapsedTime = 0.0f;

    [SerializeField] private GameObject[] spells;
    [SerializeField] private GameObject[] specialSpells;
    [SerializeField] private AudioClip magicSpellAudioClip;



    private void Awake()
    {
        ravanaInputActions = InputActionsSingleton.Instance;

        // ravanaInputActions.Ravana.MagicAttack.performed += ctx =>
        // {

        // };

        ravanaInputActions.Ravana.SpecialMagicAttack.started += ctx =>
        {
            isHoldingRightMouseButton = true;
            holdStartTime = Time.time;
        };

        ravanaInputActions.Ravana.SpecialMagicAttack.canceled += ctx =>
        {
            isHoldingRightMouseButton = false;
            if (!isHoldingRightMouseButton && Time.time - holdStartTime < .3)
            {
                MagicAttack();
            }

            if (Time.time - holdStartTime >= 1)
            {
                MagicAttack(true);
            }
            holdStartTime = Time.time;
        };
    }

    private void OnEnable()
    {
        ravanaInputActions.Enable();
    }

    private void OnDisable()
    {
        ravanaInputActions.Ravana.MagicAttack.performed -= ctx => MagicAttack();

        ravanaInputActions.Disable();
    }

    void Start()
    {
        levelController = GameObject.Find("LevelController").GetComponent<LevelController>();
        animator = GetComponent<Animator>();

        magicSpellAudioClip = Resources.Load<AudioClip>("Audio/Magic_Spell_Electricity");
        audioSource = GameObject.Find("MainAudioSource").GetComponent<AudioSource>();
        audioSource.volume = 0.2f;

        auraController = this.transform.Find("AuraMesh").GetComponent<AuraController>();

        playerScoreController = this.gameObject.GetComponent<PlayerScoreEvolutionController>();

        SpellPositions = this.transform.Find("SpellPositions").gameObject;

        MammonSigilMaterial = Resources.Load<Material>("Materials/PlayerAura/AuraMaterial_MammonSigil");
        PaimonSigilMaterial = Resources.Load<Material>("Materials/PlayerAura/AuraMaterial_PaimonSigil");

        var auraRenderer = auraController.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isHoldingRightMouseButton && Time.time - holdStartTime >= .3)
        {

            animator.SetBool("MagicHoldPosition", true);
            auraController.GetComponent<Renderer>().enabled = true;

            if (!holdingRandomMaterial) {
                GetRandomIndexAndSetMaterial();
                holdingRandomMaterial = true;
                elapsedTime = 0.0f;
            }

            elapsedTime += Time.deltaTime;

            auraController.SetGlow(elapsedTime / 10);
        }
        else
        {
            animator.SetBool("MagicHoldPosition", false);
            auraController.GetComponent<Renderer>().enabled = false;
            auraController.ResetGlow();
            holdingRandomMaterial = false;
        }
    }


    private void MagicAttack(bool isSpecialMagicAttack = false)
    {
        if (levelController.currentLevel > 1 && !magicAnimationOn && animator)
        {
            animator.SetBool("MagicAttack", true);
            magicAnimationOn = true;
            if (magicSpellAudioClip)
            {
                audioSource.clip = magicSpellAudioClip;
                audioSource.pitch = 2.0f;
                audioSource.Play();
            }


            GameObject spellInstance = null;
            if (isSpecialMagicAttack)
            {
                spellInstance = Instantiate(
                    specialSpells[specialSpellRandomIndex],
                    SpellPositions.transform.position + transform.forward + Vector3.up,
                    transform.rotation
                );

                if (spellInstance.name.Contains("Mammon"))
                {
                    // materials[0] = MammonSigilMaterial;
                    SetAuraMaterial(MammonSigilMaterial);
                }
                else if (spellInstance.name.Contains("Paimon"))
                {
                    // materials[0] = PaimonSigilMaterial;
                    SetAuraMaterial(PaimonSigilMaterial);
                }

                // auraRenderer.materials = materials;

                auraController.GetComponent<Renderer>().enabled = true;
                auraController.StartCoroutine(auraController.GlowEffect());



                StartCoroutine(DisableRenderer());
            }
            else
            {
                spellInstance = Instantiate(
                    spells[Random.Range(0, spells.Length)],
                    SpellPositions.transform.position + transform.forward + Vector3.up,
                    transform.rotation
                );
            }


            spellInstance.gameObject.SetActive(true);
            playerScoreController.ReduceScore(Random.Range(1, 3));

        }
    }

    private void GetRandomIndexAndSetMaterial()
    {
        specialSpellRandomIndex = Random.Range(0, specialSpells.Length);
        SetAuraMaterial(specialSpells[specialSpellRandomIndex].name.Contains("Mammon") ? MammonSigilMaterial : PaimonSigilMaterial);

    }

    private void SetAuraMaterial(Material material)
    {
        var auraRenderer = auraController.GetComponent<Renderer>();
        Material[] materials = auraRenderer.materials;
        materials[0] = material;
        auraRenderer.materials = materials;
    }

    IEnumerator DisableRenderer()
    {
        float waitTime = Random.Range(2f, 4f);
        yield return new WaitForSeconds(.1f);

        float fadeDuration = 3f; // Duration of the fade effect
        Renderer renderer = auraController.GetComponent<Renderer>();
        string colorPropertyName = "_GlowColor"; // The name of the color property in the shader
        Color originalColor = renderer.material.GetColor(colorPropertyName);

        DOTween.To(() => originalColor.a, x =>
        {
            Color newColor = originalColor;
            newColor.a = x;
            renderer.material.SetColor(colorPropertyName, newColor);
        }, 0, fadeDuration).OnComplete(() =>
        {
            renderer.enabled = false;
        });

        // DOTween.To(() => renderer.material.color.a, x =>
        // {
        //     renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, x);
        // }, 0, fadeDuration).OnComplete(() =>
        // {
        //     renderer.enabled = false;
        // });
    }


    public void MagicAttackAnimationEnd()
    {
        magicAnimationOn = false;
        animator.SetBool("MagicAttack", false);
    }



}
