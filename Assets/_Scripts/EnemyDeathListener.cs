using UnityEngine;

public class EnemyDeathListener : MonoBehaviour
{
    public RandomSpawnerEnemy spawner; // Referência ao Spawner (definida pelo Spawner)
    private HealthSystem healthSystem;

    void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("EnemyDeathListener: HealthSystem não encontrado em " + gameObject.name);
            Destroy(this); // Remove este componente se não houver HealthSystem
        }
    }

    // É chamado uma vez no frame após o objeto ser desativado
    void OnDisable()
    {
        // Se o spawner existe E o HealthSystem existe E está morto...
        if (spawner != null && healthSystem != null && healthSystem.IsDead())
        {
            // Notifica o spawner que este inimigo foi derrotado
            spawner.ReportEnemyDeath(gameObject);
        }
        // Poderíamos adicionar uma verificação aqui: se foi desativado sem estar morto (ex: despawn),
        // talvez precise notificar o spawner de forma diferente se isso for relevante.
    }
}