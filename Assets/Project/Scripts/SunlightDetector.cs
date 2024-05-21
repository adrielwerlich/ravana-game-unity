using UnityEngine;
using UnityEngine.UI;
public class SunlightDetector : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Color nightColor;
    [SerializeField] private Color dayColor;

    // Update is called once per frame
    void Update()
    {
        image.color = CheckSunlightCamera.Instance.IsCatchingSunlight() ? dayColor : nightColor;
        image.transform.LookAt(Camera.main.transform);
    }   
}
