using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class BulletScript : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float bulletSpeed = 0f;
    [SerializeField] private float destroyTime = 20f;
    [SerializeField] private GameObject HitVFX;
    private Coroutine disableCoroutine;

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = transform.forward * bulletSpeed;
        if (disableCoroutine != null)
            StopCoroutine(disableCoroutine);
        disableCoroutine = StartCoroutine(DisableBullet());
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(gameObject.tag)) return;
        PoolManager.Release(HitVFX, collision.GetContact(0).point, Quaternion.LookRotation(collision.GetContact(0).normal * -1f));
        gameObject.SetActive(false);
    }

    private IEnumerator DisableBullet()
    {
        float timer = destroyTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
