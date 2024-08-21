using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public InputControl inputControl => InputManager.Instance.inputControl;
    public bool isTutorial = false;
    public GameObject tutorialPanel;
    public static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this.gameObject);
        }
        // Time.timeScale = 0.5f;
    }
    public float ffTimer, ffTimerTotal;
    private Animator animator;


    public void FixedUpdate()
    {
        if (ffTimer > 0)
        {
            ffTimer -= Time.deltaTime;
            if (animator != null)
            {
                animator.speed = Mathf.Lerp(0.5f, 1f, (1 - ffTimer / ffTimerTotal));
            }
            else
            {
                Time.timeScale = Mathf.Lerp(0.5f, 1f, (1 - ffTimer / ffTimerTotal));
            }
        }
    }
    public static void FrameFrozen(float time)
    {
        Instance.ffTimer = time;
        Instance.ffTimerTotal = time;
    }
    public void FrameFrozen(float time, Animator animator = null)
    {
        if (animator) this.animator = animator;
        Instance.ffTimer = time;
        Instance.ffTimerTotal = time;
    }
    public void ReloadScene()
    {
        // 获取当前场景的索引并重新加载它
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
    public void Quit()
    {
        Application.Quit();
    }
    void Start()
    {
        if (isTutorial)
        {
            GameStartButtonClicked();
        }
        else
        {
            StartTutorial();
        }
        inputControl.Gameplay.Pause.started += ctx => { if (Time.timeScale != 1f) GameResume(); else GamePause(); };
    }
    public void GameStartButtonClicked()
    {
        isTutorial = true;
        Time.timeScale = 1f;
        inputControl.asset.FindActionMap("Gameplay", false).Enable();
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void StartTutorial()
    {
        Time.timeScale = 0f;
        inputControl.asset.FindActionMap("Gameplay", false).Disable();
        tutorialPanel.SetActive(true);
    }
    public void WinTheBattle()
    {
        Cursor.lockState = CursorLockMode.None;
        inputControl.asset.FindActionMap("Gameplay", false).Disable();
    }

    public void GamePause()
    {
        Time.timeScale = 0f;
    }

    public void GameResume()
    {
        Time.timeScale = 1f;
    }
}
