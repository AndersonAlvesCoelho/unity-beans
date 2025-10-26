// PlantGrowthController.cs (Refatorado para usar HealthSystem)

using UnityEngine;
using System.Collections;

/// <summary>
/// Controla a lógica de CRESCIMENTO da planta e REAGE à sua própria morte.
/// DELEGA toda a lógica de vida/dano/morte para o HealthSystem.
/// </summary>
[RequireComponent(typeof(HealthSystem))] // Garante que o HealthSystem está presente
public class PlantGrowthController : MonoBehaviour
{
    [Header("Configuração de Crescimento")]
    [Tooltip("Vida máxima que a planta terá. Este valor será passado para o HealthSystem.")]
    [SerializeField] private float maxHealth = 6f;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float checkInterval = 0.25f;

    [Header("Comportamento Pós-Crescimento")]
    [SerializeField] private bool invulnerableAfterGrowth = true;

    [Header("Referências")]
    [SerializeField] private HealthSystem playerHealth; // Referência ao HealthSystem do Player

    // Componentes Internos
    private Animator animator;
    private Collider plantCollider;
    private HealthSystem healthSystem; // Referência ao HealthSystem DESTA planta

    // Estados
    private bool isGrown = false;
    private bool isCheckingEnemies = false;
    private bool playerKillTriggered = false; // Garante que só matamos o player uma vez

    // Propriedades públicas para a UI ler (opcional, mas bom)
    public int CurrentHealth => healthSystem != null ? Mathf.CeilToInt(healthSystem.CurrentHealth) : 0;
    public int MaxHealth => Mathf.CeilToInt(maxHealth);

    private void Awake()
    {
        // Pega componentes locais
        animator = GetComponentInChildren<Animator>(true);
        plantCollider = GetComponent<Collider>();
        healthSystem = GetComponent<HealthSystem>(); // Pega o HealthSystem neste objeto

        // --- DELEGAÇÃO ---
        // Configura o HealthSystem com os valores desta planta
        healthSystem.maxHealth = this.maxHealth;
        // healthSystem.CurrentHealth já é setado no Awake do HealthSystem
    }

    private void OnEnable()
    {
        // Inicia monitoramento dos inimigos se aplicável
        if (!isGrown && !isCheckingEnemies && !healthSystem.IsDead())
        {
            StartCoroutine(WatchEnemiesCoroutine());
        }
    }

    // Update é usado para checar o estado de morte
    private void Update()
    {
        // --- REAÇÃO À MORTE ---
        // Se o HealthSystem disser que estamos mortos, e ainda não matamos o player...
        if (healthSystem.IsDead() && !playerKillTriggered)
        {
            playerKillTriggered = true; // Marca como feito
            KillPlayerViaHealthSystem(); // Executa a ação única de morte da planta
        }
    }

    private IEnumerator WatchEnemiesCoroutine()
    {
        isCheckingEnemies = true;
        yield return null; // Espera 1 frame

        while (!isGrown && !healthSystem.IsDead()) // Checa se a planta morreu
        {
            if (GameObject.FindGameObjectsWithTag(enemyTag).Length == 0)
            {
                GrowPlant();
                yield break;
            }
            yield return new WaitForSeconds(checkInterval);
        }
        isCheckingEnemies = false;
    }

    private void GrowPlant()
    {
        if (isGrown || healthSystem.IsDead()) return;
        isGrown = true;
        Debug.Log("🌱 Planta cresceu!");

        if (animator) animator.SetTrigger("Grow"); // Dispara animação de crescimento

        if (invulnerableAfterGrowth)
        {
            // Desativa o colisor principal da planta
            if (plantCollider) plantCollider.enabled = false;
        }
    }

    /// <summary>
    /// Função pública para inimigos causarem dano.
    /// Agora ela APENAS repassa a chamada para o HealthSystem.
    /// </summary>
    public void DamagePlant(float amount = 1f, Transform attacker = null)
    {
        if ((isGrown && invulnerableAfterGrowth) || healthSystem.IsDead()) return;

        // --- DELEGAÇÃO ---
        healthSystem.TakeDamage(amount, attacker);
    }

    /// <summary>
    /// Ação única da planta: matar o jogador quando ela morre.
    /// Esta função é chamada pelo Update() quando healthSystem.IsDead() se torna true.
    /// </summary>
    private void KillPlayerViaHealthSystem()
    {
        if (playerHealth == null) // Tenta encontrar de novo por segurança
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO) playerHealth = playerGO.GetComponent<HealthSystem>();
        }

        if (playerHealth != null && !playerHealth.IsDead())
        {
            // Dano fatal = vida atual do player
            float fatalDamage = Mathf.Max(1f, playerHealth.CurrentHealth); 
            playerHealth.TakeDamage(fatalDamage, attackerTransform: transform);
            Debug.Log($"Planta morreu e levou o Player junto!");
        }
    }
}