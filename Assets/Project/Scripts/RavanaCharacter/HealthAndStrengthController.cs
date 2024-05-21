using UnityEngine;
using Microlight.MicroBar;

public class HealthAndStrengthController : MonoBehaviour
{
    [SerializeField] private MicroBar strengthBarController;
    private float strength = 2f;

    public float Strength
    {
        get { return strength; }
        set { strength = value; }
    }
    void Start()
    {
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
