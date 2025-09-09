using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 音频管理器
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    /// <summary>
    /// 音频管理器单例
    /// </summary>
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
            }
            return _instance;
        }
    }
    /// <summary>
    /// bgm音频源
    /// </summary>
    public AudioSource bgmAudioSource;
    /// <summary>
    /// sfx音频源
    /// </summary>
    public AudioSource sfxAudioSource;

    public void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
    }
}
