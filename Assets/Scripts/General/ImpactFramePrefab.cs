using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactFramePrefab : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 0.5f;
    public void OnEnable()
    {
        StartCoroutine(DeactivateCoroutine());
    }
    IEnumerator DeactivateCoroutine()
    {
        yield return new WaitForSeconds(_lifeTime);
        gameObject.SetActive(false);
    }
}
