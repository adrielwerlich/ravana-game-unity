using System;
using Microlight.MicroBar;
using TMPro;
using UnityEngine;

public class PlayerScoreEvolutionController : MonoBehaviour
{
    [SerializeField] private MicroBar scoreBar;
    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private LevelController levelController;

    public static event Action ScoreHundredReached;

    private float score = 120f;

    public float Score
    {
        get { return score; }
        set { score = value; }
    }

    public int HundredScoreMilestoneAchieve { get; internal set; }

    void Start()
    {
        scoreBar.Initialize(100f);
        scoreBar.UpdateHealthBar(score);
        scoreText.text = score.ToString();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void IncreaseScore(float value)
    {
        score += value;
        scoreText.text = score.ToString();

        scoreBar.UpdateHealthBar(score);

        if (score >= 100f && levelController.currentLevel == 1)
        {
            ScoreHundredReached.Invoke();
        }
    }

    public void ReduceScore(float value)
    {
        score -= value;
        scoreText.text = score.ToString();

        scoreBar.UpdateHealthBar(score);
    }
}
