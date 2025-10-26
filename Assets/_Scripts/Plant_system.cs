using UnityEngine;
using System.Collections;

public class PlantGrowthController : MonoBehaviour
{
    [Header("Vida da Planta (muda)")]
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private bool invulnerableAfterGrowth = true;

    [Header("Crescimento ao limpar inimigos")]
    [SerializeField] private string enemyTag = "Enemy";  // marque os inimigos com essa tag
    [SerializeField] private float checkInterval = 0.25f; // frequência para checar se acabou inimigo

    [Header("Refs")]
    [SerializeField] private Animator animator;          // Precisa de um Trigger "Grow"
    [SerializeField] private HealthSystem playerHealth;  // HealthSystem do Player

    private int currentHealth;
    private bool isGrown;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (!animator) animator = GetComponentInChildren<Animator>(true);

        if (!playerHealth)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO) playerHealth = playerGO.GetComponent<HealthSystem>();
        }
    }

    private void OnEnable()
    {
        // começa a observar os inimigos da cena
        StartCoroutine(WatchEnemiesCoroutine());
    }

    private IEnumerator WatchEnemiesCoroutine()
    {
        // Espera um frame para garantir que a cena já instanciou tudo
        yield return null;

        while (!isGrown)
        {
            // Se não há nenhum GameObject com a tag de inimigo, a planta cresce
            if (GameObject.FindGameObjectsWithTag(enemyTag).Length == 0)
            {
                HandleAllEnemiesCleared();
                yield break;
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void HandleAllEnemiesCleared()
    {
        if (isGrown) return;
        isGrown = true;

        if (animator) animator.SetTrigger("Grow");

        if (invulnerableAfterGrowth)
        {
            // Ex.: GetComponent<Collider>()?.enabled = false; // se quiser parar de tomar dano
        }
    }

    /// <summary>
    /// Causa dano na planta (apenas enquanto é muda).
    /// Chame este método quando inimigos/tiros atingirem a planta.
    /// </summary>
    public void DamagePlant(int amount = 1)
    {
        if (isGrown) return; // após crescer, não toma dano (opcional)

        currentHealth -= Mathf.Max(1, amount);
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            KillPlayerViaHealthSystem();
        }
    }

    private void KillPlayerViaHealthSystem()
    {
        if (!playerHealth)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO) playerHealth = playerGO.GetComponent<HealthSystem>();
        }

        if (playerHealth != null && !playerHealth.IsDead())
        {
            // Dano fatal = vida atual -> dispara HandleDeath() no seu HealthSystem
            float fatalDamage = Mathf.Max(1f, playerHealth.CurrentHealth);
            playerHealth.TakeDamage(fatalDamage, attackerTransform: transform);
        }
        else
        {
            Debug.LogWarning("PlantGrowthController: HealthSystem do Player não encontrado ou já morto.");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Damage 1")]
    private void DebugDamage() => DamagePlant(1);
#endif
}

