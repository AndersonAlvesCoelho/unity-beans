using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Configuração")]
    public float maxHealth = 10f;
    public float disappearDelayAfterDeath = 2f;
    public float knockbackForce = 5f;

    [Header("Efeitos de Dano")]
    public float invulnerabilityTime = 0.6f;
    public float blinkInterval = 0.1f;

    // Propriedade pública de leitura
    public float CurrentHealth { get; private set; }

    private bool isDead = false;
    private bool isInvulnerable = false;

    // Referências
    private Animator animator;
    private Rigidbody rb;
    private Collider charCollider;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        CurrentHealth = maxHealth;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        charCollider = GetComponent<Collider>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null) Debug.LogWarning("HealthSystem: Animator não encontrado em " + gameObject.name);
        if (rb == null) Debug.LogWarning("HealthSystem: Rigidbody não encontrado em " + gameObject.name);
        if (charCollider == null) Debug.LogWarning("HealthSystem: Collider não encontrado em " + gameObject.name);
    }

    // ------------------------------
    // FUNÇÃO DE DANO PRINCIPAL
    // ------------------------------
    public void TakeDamage(float damageAmount, Transform attackerTransform = null)
    {
        if (isDead || isInvulnerable) return;

        CurrentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} tomou {damageAmount} de dano. Vida: {CurrentHealth}/{maxHealth}");

        // 1️⃣ Knockback sempre que levar dano
        ApplyKnockback(attackerTransform);

        // 2️⃣ Efeito de piscar + invulnerabilidade
        StartCoroutine(DamageFlashAndInvulnerability());

        // 3️⃣ Animação de dano
        if (animator != null && CurrentHealth > 0)
            animator.SetTrigger("Damage");

        // 4️⃣ Checa morte
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            if (!isDead) StartCoroutine(HandleDeath());
        }
    }

    // ------------------------------
    // APLICA EMPURRÃO (KNOCKBACK)
    // ------------------------------
    private void ApplyKnockback(Transform attacker)
    {
        if (rb == null || attacker == null || knockbackForce <= 0f || rb.isKinematic)
            return;

        Vector3 knockbackDirection = (transform.position - attacker.position);
        knockbackDirection.y = 0;
        knockbackDirection.Normalize();

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
    }

    // ------------------------------
    // PISCAR E INVULNERABILIDADE TEMPORÁRIA
    // ------------------------------
    private IEnumerator DamageFlashAndInvulnerability()
    {
        if (spriteRenderer == null) yield break;

        isInvulnerable = true;
        Color originalColor = spriteRenderer.color;
        bool visible = true;

        float elapsed = 0f;
        while (elapsed < invulnerabilityTime)
        {
            spriteRenderer.color = visible ? originalColor : new Color(1, 0.3f, 0.3f, 0.7f);
            visible = !visible;
            elapsed += blinkInterval;
            yield return new WaitForSeconds(blinkInterval);
        }

        spriteRenderer.color = originalColor;
        isInvulnerable = false;
    }

    // ------------------------------
    // MORTE E DESAPARECIMENTO
    // ------------------------------
    private IEnumerator HandleDeath()
    {
        isDead = true;
        Debug.Log(gameObject.name + " morreu.");

        // Animação de morte (se existir)
        if (animator != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "Die")
                {
                    animator.SetTrigger("Die");
                    break;
                }
            }
        }

        // Desativa controladores
        if (TryGetComponent<PlayerController3D>(out var playerController))
            playerController.enabled = false;
        if (TryGetComponent<EnemyController>(out var enemyController))
            enemyController.enabled = false;

        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        // Piscar até sumir
        if (spriteRenderer != null)
        {
            float endTime = Time.time + disappearDelayAfterDeath;
            bool visible = true;
            Color originalColor = spriteRenderer.color;

            while (Time.time < endTime)
            {
                spriteRenderer.color = visible ? originalColor : Color.red;
                visible = !visible;
                yield return new WaitForSeconds(blinkInterval);
            }

            spriteRenderer.color = Color.gray;
        }

        if (rb != null) rb.isKinematic = true;
        if (charCollider != null) charCollider.enabled = false;

        gameObject.SetActive(false);
    }

    public bool IsDead() => isDead;
}
