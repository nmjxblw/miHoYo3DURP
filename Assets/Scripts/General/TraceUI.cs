using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TraceUI : MonoBehaviour
{
    public float fadeTime = 0.3f;
    private void OnEnable()
    {
        StopAllCoroutines();
        Image image = GetComponent<Image>();
        Color color = image.color;
        color.a = 1f;
        image.color = color;
    }
    private void OnDisable()
    {
        StopAllCoroutines();
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
