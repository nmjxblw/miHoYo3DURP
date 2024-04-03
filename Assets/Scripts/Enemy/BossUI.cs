using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BossUI : MonoBehaviour
{
    public List<Image> images;
    public List<float> imagesTargetAlpha;
    public List<TextMeshProUGUI> texts;
    public List<float> textsTargetAlpha;
    public float fadeInDuration = 1f;
    public Coroutine fadeInCoroutine;
    public Coroutine fadeOutCoroutine;
    void Start()
    {
        InitializedAlpha();
    }
    public void InitializedAlpha()
    {
        for (int i = 0; i < images.Count; i++)
        {
            imagesTargetAlpha.Add(images[i].color.a);
            images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, 0f);
        }
        for (int i = 0; i < texts.Count; i++)
        {
            textsTargetAlpha.Add(texts[i].color.a);
            texts[i].color = new Color(texts[i].color.r, texts[i].color.g, texts[i].color.b, 0f);
        }
    }
    public void StartFadeIn()
    {
        if (fadeInCoroutine == null)
            fadeInCoroutine = StartCoroutine(FadeIn());
    }
    public IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            for (int i = 0; i < images.Count; i++)
            {
                images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, alpha * imagesTargetAlpha[i]);
            }
            for (int i = 0; i < texts.Count; i++)
            {
                texts[i].color = new Color(texts[i].color.r, texts[i].color.g, texts[i].color.b, alpha * textsTargetAlpha[i]);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public void StartFadeOut()
    {
        if (fadeOutCoroutine == null)
            fadeOutCoroutine = StartCoroutine(FadeOut());
    }
    public IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeInDuration);
            for (int i = 0; i < images.Count; i++)
            {
                images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, alpha * imagesTargetAlpha[i]);
            }
            for (int i = 0; i < texts.Count; i++)
            {
                texts[i].color = new Color(texts[i].color.r, texts[i].color.g, texts[i].color.b, alpha * textsTargetAlpha[i]);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
