using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    int highScore;
    public int score=0;
    public TextMeshProUGUI scoreText, highScoreText;
    public static Inventory instance;
    private void Awake()
    {
        if (instance != null)
            return;
        instance = this;
        highScore = PlayerPrefs.GetInt("highScore", 0);
        highScoreText.text = highScore.ToString();
    }

    private void Update()
    {
        scoreText.text = score.ToString();
    }

    public void AddToScore(int toAdd)
    {
        score += toAdd;
        if(score > highScore)
        {
            PlayerPrefs.SetInt("highScore", score);
            highScore = score;
            highScoreText.text = highScore.ToString();
        }
    }
    
}
