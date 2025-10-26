using UnityEngine;
using System.Collections;

public class PlantGrowthController : MonoBehaviour
{
    [Header("Vida da Planta")]
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private bool invulnerableAfterGrowth = true;
    [SerializeField] private float fadeOutDurationOnDeath = 1.5f;

    [Header("Lógica de Crescimento")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float checkInterval = 0.25f;

    [Header("Referências")]
    [SerializeField] private HealthSystem playerHealth; // Para matar o Player

    // Componentes Internos
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider plantCollider;

    // Estado
    public int CurrentHealth { get; private set; } // Propriedade pública para UI ler
    private bool isGrown = false;
    private bool isDead = false;

    private void Awake()
    {
        CurrentHealth = maxHealth;

        animator = GetComponentInChildren<Animator>(true);
        spriteRenderer = GetComponent<SpriteRenderer>();
        plantCollider = GetComponent<Collider>();

        if (!playerHealth)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO) playerHealth = playerGO.GetComponent<HealthSystem>();
        }
    }

    private void OnEnable()
    {
        if (!isGrown && !isDead)
        {
            StartCoroutine(WatchEnemiesCoroutine());
        }
    }

    private IEnumerator WatchEnemiesCoroutine()
    {
        yield return null; 

        while (!isGrown && !isDead)
        {
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
        if (isGrown || isDead) return;
        isGrown = true;
        Debug.Log("Planta cresceu!");

        if (animator) animator.SetTrigger("Grow"); // Dispara a animação "isGrow" (deve ser "Grow")

        if (invulnerableAfterGrowth)
        {
            if (plantCollider != null) plantCollider.enabled = false;
        }
    }

    // Função pública para a planta receber dano
    public void DamagePlant(int amount = 1)
    {
        if ((isGrown && invulnerableAfterGrowth) || isDead) return;

        CurrentHealth -= Mathf.Max(1, amount);
        Debug.Log($"Planta tomou {amount} de dano. Vida: {CurrentHealth}/{maxHealth}");

        // Verifica se morreu
        if (CurrentHealth <= 0 && !isDead)
        {
            CurrentHealth = 0;
            StartCoroutine(HandlePlantDeath());
        }
    }

    // Corrotina para Morte da Planta
    private IEnumerator HandlePlantDeath()
    {
        isDead = true;
        Debug.Log("Planta morreu.");

        // Toca a animação "Die"
        if (animator != null)
        {
             bool hasDieTrigger = false;
             foreach (var param in animator.parameters) if (param.name == "Die") hasDieTrigger = true;
             if (hasDieTrigger) animator.SetTrigger("Die");
        }
        
        // Mata o Player
        KillPlayerViaHealthSystem();

        // Desativa o colisor
        if (plantCollider != null) plantCollider.enabled = false;

        // Inicia o Fade Out
        if (spriteRenderer != null)
        {
            float timer = 0f;
            Color startColor = spriteRenderer.color;
            while (timer < fadeOutDurationOnDeath)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, timer / fadeOutDurationOnDeath);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(fadeOutDurationOnDeath);
        }

        // Desativa o GameObject
        gameObject.SetActive(false);
    }

    // Lógica original para matar o player
    private void KillPlayerViaHealthSystem()
    {
        if (!playerHealth)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO) playerHealth = playerGO.GetComponent<HealthSystem>();
        }

        if (playerHealth != null && !playerHealth.IsDead())
        {
            float fatalDamage = Mathf.Max(1f, playerHealth.CurrentHealth);
            playerHealth.TakeDamage(fatalDamage, attackerTransform: transform);
        }
    }
    
    // Getter público para a vida máxima (para UI)
    public int GetMaxHealth()
    {
        return maxHealth;
    }
}