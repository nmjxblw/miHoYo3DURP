using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UltUI : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI UltHitText;
    public PlayerControl playerControl;

    private void Awake()
    {
        image = GetComponent<Image>();
        UltHitText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update()
    {
        image.fillAmount = playerControl.ultChargePercentage;
        Color color = image.color;
        color.r = 1 - playerControl.ultChargePercentage;
        color.b = 1 - playerControl.ultChargePercentage;
        image.color = color;
        UltHitText.text = playerControl.isUltActivable ? "Ult\nReady" : $"{(playerControl.ultChargePercentage * 100f):F0}%";
    }
}
