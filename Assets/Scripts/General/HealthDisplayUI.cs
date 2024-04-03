using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthDisplayUI : MonoBehaviour
{
    public Health health;
    public Image currentHealthImage;
    public Image displayHealthImage;

    public virtual void Awake()
    {
        displayHealthImage = transform.Find("DispalyHp").GetComponent<Image>();
        currentHealthImage = transform.Find("TrueHp").GetComponent<Image>();
    }
}
