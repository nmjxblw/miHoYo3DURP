using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
public class Health : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    public int maxHealth => _maxHealth;
    public int currentHealth;
    public float currentHealthPercent => (float)currentHealth / (float)maxHealth;
    public float displayHealth;
    public float displayHealthPercent => displayHealth / (float)maxHealth;
    public Coroutine healthChangeCoroutine;
    public float changeDuration = 1f;
    [SerializeField] private int testDamage = 10;
    public HealthDisplayUI healthDisplayUI;
    public UnityEvent<DealDamage> onDamageTakenEvent;
    public UnityEvent onDeathEvent;
    public void Start()
    {
        currentHealth = maxHealth;
        displayHealth = (float)currentHealth;
        if (healthDisplayUI != null)
        {
            healthDisplayUI.health = this;
            healthDisplayUI.currentHealthImage.fillAmount = currentHealthPercent;
            healthDisplayUI.displayHealthImage.fillAmount = displayHealthPercent;
        }
    }
    public void TakeDamage(DealDamage dealDamage)
    {
        onDamageTakenEvent?.Invoke(dealDamage);
    }

    public void HandleHealthChange(int damage)
    {
        currentHealth = Math.Clamp(currentHealth - damage, 0, maxHealth);
        if (healthDisplayUI != null) healthDisplayUI.currentHealthImage.fillAmount = currentHealthPercent;
        float targetHealth = (float)currentHealth;
        if (healthChangeCoroutine != null)
        {
            StopCoroutine(healthChangeCoroutine);
        }
        healthChangeCoroutine = StartCoroutine(HealthChange(displayHealth, targetHealth));
        if (currentHealth <= 0) { onDeathEvent?.Invoke(); }
    }

    public IEnumerator HealthChange(float startHealth, float targetHealth)
    {
        float time = 0;
        while (time < changeDuration)
        {
            displayHealth = Mathf.Lerp(startHealth, targetHealth, time / changeDuration);
            if (healthDisplayUI != null) healthDisplayUI.displayHealthImage.fillAmount = displayHealthPercent;
            time += Time.deltaTime;
            yield return null;
        }
        displayHealth = targetHealth;
        if (healthDisplayUI != null) healthDisplayUI.displayHealthImage.fillAmount = displayHealthPercent;
    }
#if UNITY_EDITOR
    [ContextMenu("reset")]
    public void Reset()
    {
        currentHealth = maxHealth;
        displayHealth = (float)currentHealth;
        if (healthDisplayUI != null)
        {
            healthDisplayUI.health = this;
            healthDisplayUI.currentHealthImage.fillAmount = currentHealthPercent;
            healthDisplayUI.displayHealthImage.fillAmount = displayHealthPercent;
        }
    }
    [ContextMenu("test")]
    public void Test()
    {
        HandleHealthChange(testDamage);
    }
#endif
}
