using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DealDamage : MonoBehaviour
{
    public int damage = 0;
    protected int deltaDamage = 0;
    public int totalDamage => damage + deltaDamage;
    public bool isHeavyAttack = false;

    public virtual void OnCollisionEnter(Collision other)
    {
        // Debug.Log(transform.name + " collided with " + other.gameObject.name);
        if ((other.gameObject.CompareTag("Player") && gameObject.CompareTag("PlayerAttack")) ||
            (other.gameObject.CompareTag("Boss") && gameObject.CompareTag("BossAttack"))) return;
        other.gameObject.GetComponent<Health>()?.TakeDamage(this);
    }
    public virtual void OnTriggerEnter(Collider other)
    {
        // Debug.Log(transform.name + " collided with " + other.name);
        if (other.gameObject.CompareTag(gameObject.tag)) return;
        other.GetComponent<Health>()?.TakeDamage(this);
    }
}
