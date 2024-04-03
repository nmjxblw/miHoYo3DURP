using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRingDrawer : MonoBehaviour
{
    public float innerRadius = 50f;
    public float outerRadius = 100f;
    public int segments = 60;

    [Range(0, 1)]
    public float progress = 0f;

    private Image ringImage;

    void Start()
    {
        CreateRingUI();
    }

    void CreateRingUI()
    {
        GameObject ringGameObject = new GameObject("Ring");
        ringGameObject.transform.SetParent(transform, false);

        ringImage = ringGameObject.AddComponent<Image>();
        ringImage.sprite = Sprite.Create(CreateRingTexture(), new Rect(0, 0, outerRadius * 2, outerRadius * 2), new Vector2(0.5f, 0.5f));
        ringImage.type = Image.Type.Filled;
        ringImage.fillMethod = Image.FillMethod.Radial360;
        ringImage.fillOrigin = (int)Image.Origin360.Top;
    }

    Texture2D CreateRingTexture()
    {
        Texture2D texture = new Texture2D((int)outerRadius * 2, (int)outerRadius * 2);
        Color[] colors = new Color[(int)(outerRadius * 2) * (int)(outerRadius * 2)];

        float angleIncrement = 360f / segments;
        float innerRadiusSqr = innerRadius * innerRadius;
        float outerRadiusSqr = outerRadius * outerRadius;

        for (int y = 0; y < outerRadius * 2; y++)
        {
            for (int x = 0; x < outerRadius * 2; x++)
            {
                float dx = x - outerRadius;
                float dy = y - outerRadius;
                float distSqr = dx * dx + dy * dy;

                if (distSqr < outerRadiusSqr && distSqr > innerRadiusSqr)
                {
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    float progressAngle = 360f * progress;
                    if (angle < progressAngle)
                    {
                        colors[y * (int)(outerRadius * 2) + x] = Color.white;
                    }
                    else
                    {
                        colors[y * (int)(outerRadius * 2) + x] = Color.clear;
                    }
                }
                else
                {
                    colors[y * (int)(outerRadius * 2) + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    void Update()
    {
        ringImage.fillAmount = progress; // Update the fill amount of the image based on progress
    }
}
