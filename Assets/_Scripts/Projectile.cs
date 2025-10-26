using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 1f; // Dano deste projétil
    public string targetTag = "Player"; // Tag do alvo a acertar

    void  start()
    {
        Debug.Log("Name of the projectile: " + this.gameObject.name);

        // Destroy(gameObject, 5f);
    }

    void OnTriggerEnter(Collider other)
    {
        // Verifica se colidiu com o alvo correto
        if (other.CompareTag(targetTag))
        {
            // Tenta pegar o HealthSystem do alvo
            HealthSystem targetHealth = other.GetComponent<HealthSystem>();
            if (targetHealth != null)
            {
                // Aplica o dano
                targetHealth.TakeDamage(damage);
            }
            // Destrói o projétil ao acertar
            Destroy(gameObject);
        }
        else if (!other.isTrigger) // Se colidiu com algo sólido que não seja um trigger
        {
            Destroy(gameObject);
        }
    }
}