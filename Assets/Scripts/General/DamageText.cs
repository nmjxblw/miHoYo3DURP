using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Random = UnityEngine.Random;
public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI tmp;
    [Range(0f, 30f)] public float fadeY = 30f;
    [Range(0f, 100f)] public float offset = 50f;
    public Vector3 randomOffset = Vector3.zero;
    public float fadeTime = 1f;
    public Color color;
    private Coroutine fadeOutCoroutine;
    [HideInInspector] public Transform followTransform;
    [HideInInspector] public Vector3 followTransformViewportPoint;
    public void OnEnable()
    {
        followTransform = null;
        followTransformViewportPoint = Vector3.zero;
        tmp = tmp ?? GetComponent<TextMeshProUGUI>();
        color = tmp.color;
        color.a = 1f;
        tmp.color = color;
        randomOffset = new Vector3(Random.Range(-offset, offset), 0f, 0f);
        if (fadeOutCoroutine != null) StopCoroutine(fadeOutCoroutine);
        fadeOutCoroutine = StartCoroutine(FadeOut());
    }

    void Update()
    {
        if (followTransform == null) return;
        Vector3 pos = Camera.main.WorldToViewportPoint(followTransform.position);
        followTransformViewportPoint = new Vector3(pos.x * Screen.width, pos.y * Screen.height, 0f);
    }

    protected IEnumerator FadeOut()
    {
        float timer = 0f;
        float scale = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float alpha = 1 - timer / fadeTime;
            color.a = alpha;
            tmp.color = color;
            transform.position = followTransformViewportPoint + new Vector3(0, Mathf.Lerp(0, fadeY, timer / fadeTime), 0) + randomOffset;
            scale = Mathf.Lerp(1f, 0.5f, timer / fadeTime);
            transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
