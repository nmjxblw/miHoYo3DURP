using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class LockUI : MonoBehaviour
{
    public Transform lookAtTarget;
    private Coroutine fadeCoroutine;
    public float fadeTime = 0.5f;
    private void OnEnable()
    {
        Image image = GetComponent<Image>();
        Color color = image.color;
        color.a = 1f;
        image.color = color;
    }
    public void Update()
    {
        if (!CameraManager.Instance.lookAtTarget)
        {
            if (fadeCoroutine == null)
                fadeCoroutine = StartCoroutine(FadeOut());
        }
        else
        {
            transform.position = Camera.main.WorldToScreenPoint(lookAtTarget.position);
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

    }
    public IEnumerator FadeOut()
    {
        float timer = 0;
        Image image = GetComponent<Image>();
        Color color = image.color;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float alpha = 1 - timer / fadeTime;
            color.a = alpha;
            image.color = color;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
