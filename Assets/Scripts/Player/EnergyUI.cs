using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergyUI : MonoBehaviour
{
    public Image currentEnergyImage;
    public void Awake()
    {
        currentEnergyImage = transform.Find("TrueEnergy").GetComponent<Image>();
    }
}
