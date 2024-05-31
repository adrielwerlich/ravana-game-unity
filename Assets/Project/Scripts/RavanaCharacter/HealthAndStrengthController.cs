using UnityEngine;
using Microlight.MicroBar;

public class HealthAndStrengthController : MonoBehaviour
{
    [SerializeField] private MicroBar strengthBarController;
    [SerializeField] private float strength = 100f;

    public float Strength
    {
        get { return strength; }
        set { strength = value; }
    }
    void Start()
    {
        strengthBarController = GameObject.Find("StrengthBar_MicroBar").GetComponent<MicroBar>();
        strengthBarController.Initialize(strength);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ReduceStrength(float value)
    {
        strength -= value;
        strengthBarController.UpdateHealthBar(strength);
    }

    public void IncreaseStrength(float value)
    {
        strength += value;
        strengthBarController.UpdateHealthBar(strength);
    }
}
