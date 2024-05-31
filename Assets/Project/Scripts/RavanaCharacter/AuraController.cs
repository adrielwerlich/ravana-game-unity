using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AuraController : MonoBehaviour
{
    private Material auraMaterial;
    private float initialGlowIntensity;
    private Color initialGlowColor;
    private bool isGlowing = false;
    private Bloom bloom;


    [SerializeField] private Volume volume;
    public Renderer auraRenderer; // Reference to the renderer of the aura mesh
    public float maxGlowIntensity = 10.0f;
    public float minGlowIntensity = 1.0f;
    public float glowDuration;
    void Start()
    {
        volume = GameObject.Find("GlobalVolume").GetComponent<Volume>();
        auraRenderer = GetComponent<Renderer>();
        auraRenderer.enabled = false;
        auraMaterial = auraRenderer.material;

        if (volume != null && volume.profile != null)
        {
            // Try to get the Bloom override from the volume profile
            if (volume.profile.TryGet(out bloom))
            {
                // Debug.Log("Bloom component found.");
            }
            else
            {
                // Debug.LogError("Bloom component not found in the Volume profile.");
            }
        }
        else
        {
            // Debug.LogError("Volume component not found in the scene.");
        }

        glowDuration = Random.Range(2f, 4f);

        initialGlowIntensity = auraMaterial.GetFloat("_GlowIntensity");
        initialGlowColor = auraMaterial.GetColor("_GlowColor");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isGlowing)
        {
            StartCoroutine(GlowEffect());
        }
    }

    public IEnumerator GlowEffect()
    {
        isGlowing = true;

        float elapsedTime = 0.0f;
        while (elapsedTime < glowDuration)
        {
            // Calculate the intensity based on elapsed time
            float t = elapsedTime / glowDuration;

            SetGlow(t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset glow intensity after the effect
        auraMaterial.SetFloat("_GlowIntensity", minGlowIntensity);
        isGlowing = false;
    }

    public void SetGlow(float t)
    {
        float glowIntensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, Mathf.PingPong(t * 2, 1.0f));
        // Color glowColor = Color.Lerp(Color.green, Color.blue, Mathf.PingPong(t * 2, 1.0f));

        Debug.Log("Glow intensity: " + glowIntensity);

        float hue = Mathf.PingPong(t, 1.0f);
        Color glowColor = Color.HSVToRGB(hue, 1, 1);

        auraMaterial.SetFloat("_GlowIntensity", glowIntensity);
        auraMaterial.SetColor("_GlowColor", glowColor);

        if (bloom != null)
        {
            bloom.intensity.value = glowIntensity;
            // bloom.tint.value = glowColor;
        }
    }

    public void ResetGlow()
    {
        auraMaterial.SetFloat("_GlowIntensity", initialGlowIntensity);
        auraMaterial.SetColor("_GlowColor", initialGlowColor);

        if (bloom != null)
        {
            bloom.intensity.value = initialGlowIntensity;
            // bloom.tint.value = initialGlowColor;
        }
    }
}


