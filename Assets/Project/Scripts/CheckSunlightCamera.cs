using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckSunlightCamera : MonoBehaviour
{
    public static CheckSunlightCamera Instance { get; private set; }

    [SerializeField] private Light directionalLight;

    private Color cameraBackgroundColor;
    private RenderTexture cameraRenderTexture;
    void Awake()
    {
        Instance = this;

        cameraBackgroundColor = this.GetComponent<Camera>().backgroundColor;
        cameraRenderTexture = this.GetComponent<Camera>().targetTexture;
    }

    public bool IsCatchingSunlight()
    {
        this.transform.forward = -directionalLight.transform.forward;

        RenderTexture.active = cameraRenderTexture;

        Texture2D texture = new Texture2D(cameraRenderTexture.width, cameraRenderTexture.height, TextureFormat.ARGB32, false);
        Rect rect = new Rect(0, 0, cameraRenderTexture.width, cameraRenderTexture.height);
        texture.ReadPixels(rect, 0, 0);

        Color skyColor = texture.GetPixel(0, 0);

        RenderTexture.active = null;

        float alphaMax = .1f;
        return skyColor.a < alphaMax;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
