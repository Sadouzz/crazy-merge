using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayManager : MonoBehaviour
{
    public GameObject pausePanel, diePanel;
    public static PlayManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        diePanel.SetActive(true);
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    public void Home()
    {
        SceneManager.LoadScene(0);
    }

    public void Retry()
    {
        SaveManager.instance.DeleteSave();
        SceneManager.LoadScene(1);
    }
}
