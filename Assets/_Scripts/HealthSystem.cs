using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Configuração")]
    public float maxHealth = 10f;
    public float disappearDelayAfterDeath = 2f;

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.18f; // tempo que o controle ficará bloqueado após o empurrão

    [Header("Efeitos de Dano")]
    public float invulnerabilityTime = 0.6f;
    public float blinkInterval = 0.08f;

    public float CurrentHealth { get; private set; }

    private bool isDead = false;
    private bool isInvulnerable = false;
    private bool isKnockedBack = false;

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
        if (spriteRenderer == null) Debug.LogWarning("HealthSystem: SpriteRenderer não encontrado em " + gameObject.name);
    }

    /// <summary>
    /// Aplica dano. Opcionalmente passe o transform do atacante para aplicar knockback.
    /// </summary>
    public void TakeDamage(float damageAmount, Transform attackerTransform = null)
    {
        if (isDead) return;
        if (isInvulnerable) return;

        float newHealth = CurrentHealth - damageAmount;
        bool isFatal = newHealth <= 0f; // se o golpe for o último

        CurrentHealth = Mathf.Max(newHealth, 0f);
        Debug.Log($"{gameObject.name} tomou {damageAmount} de dano. Vida: {CurrentHealth}/{maxHealth}");

        // 1) Knockback (somente se houver atacante e ainda não estiver morto)
        if (!isFatal && attackerTransform != null && knockbackForce > 0f && rb != null && !rb.isKinematic)
        {
            StartCoroutine(ApplyKnockbackCoroutine(attackerTransform));
        }

        // 2) Piscar e invulnerabilidade curta (somente se não for dano fatal)
        if (!isFatal)
        {
            if (spriteRenderer != null)
                StartCoroutine(DamageFlashAndInvulnerability());
            else
                StartCoroutine(SimpleInvulnerability());
        }

        // 3) Trigger de animação de dano (apenas se ainda vivo)
        if (animator != null && !isFatal)
        {
            animator.SetTrigger("Damage");
        }

        // 4) Checar morte
        if (isFatal && !isDead)
        {
            StartCoroutine(HandleDeath());
        }
    }

    // Aplica knockback e impede controle por um tempo curto (isKnockedBack)
    private IEnumerator ApplyKnockbackCoroutine(Transform attacker)
    {
        if (rb == null || attacker == null) yield break;

        isKnockedBack = true;

        // Direção do empurrão: sai do atacante
        Vector3 dir = (transform.position - attacker.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
            dir.y = 0f;
        }
        dir.Normalize();

        // Zera velocidade horizontal antes de aplicar impulso
        Vector3 vel = rb.velocity;
        rb.velocity = new Vector3(0f, vel.y, 0f);

        // Aplica impulso (ignora massa)
        rb.AddForce(dir * knockbackForce, ForceMode.VelocityChange);

        float t = 0f;
        while (t < knockbackDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        isKnockedBack = false;
    }

    private IEnumerator DamageFlashAndInvulnerability()
    {
        if (spriteRenderer == null) yield break;

        isInvulnerable = true;
        Color original = spriteRenderer.color;
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invulnerabilityTime)
        {
            spriteRenderer.color = visible ? original : new Color(1f, 0.3f, 0.3f, 0.8f);
            visible = !visible;
            elapsed += blinkInterval;
            yield return new WaitForSeconds(blinkInterval);
        }

        spriteRenderer.color = original;
        isInvulnerable = false;
    }

    private IEnumerator SimpleInvulnerability()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }

    private IEnumerator HandleDeath()
    {
        isDead = true;
        Debug.Log(gameObject.name + " morreu.");

        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                if (p.name == "Die")
                {
                    animator.SetTrigger("Die");
                    break;
                }
            }
        }

        if (TryGetComponent<PlayerController3D>(out var playerController)) playerController.enabled = false;
        if (TryGetComponent<EnemyController>(out var enemyController)) enemyController.enabled = false;

        if (rb != null) rb.velocity = Vector3.zero;

        // Efeito final de piscar até desaparecer
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            float end = Time.time + disappearDelayAfterDeath;
            bool visible = true;

            while (Time.time < end)
            {
                visible = !visible;
                yield return new WaitForSeconds(blinkInterval);
            }

            spriteRenderer.color = Color.grey;
        }
        else
        {
            yield return new WaitForSeconds(disappearDelayAfterDeath);
        }

        if (rb != null) rb.isKinematic = true;
        if (charCollider != null) charCollider.enabled = false;

        gameObject.SetActive(false);
    }

    public bool IsDead() => isDead;
    public bool IsInvulnerable() => isInvulnerable;
    public bool IsKnockedBack() => isKnockedBack;
}
