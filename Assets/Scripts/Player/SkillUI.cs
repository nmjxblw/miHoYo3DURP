using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SkillUI : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI skillHitText;
    public PlayerControl playerControl;

    private void Awake()
    {
        image = GetComponent<Image>();
        skillHitText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update()
    {
        image.fillAmount = playerControl.skillRemaining;
        Color color = image.color;
        color.b = 1 - playerControl.skillRemaining;
        image.color = color;
        skillHitText.text = playerControl.isSkillActivable ? "Skill\nReady" : $"{(playerControl.skillRemaining * 100f):F0}%";
    }
}
