using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Configuração")]
    public float maxHealth = 10f;
    public float disappearDelayAfterDeath = 0.8f;

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.18f; // tempo que o controle ficará bloqueado após o empurrão

    [Header("Efeitos de Dano")]
    public float invulnerabilityTime = 0.6f;
    public float blinkInterval = 0.08f;

    // Leitura externa da vida atual
    public float CurrentHealth { get; private set; }

    // Estados
    private bool isDead = false;
    private bool isInvulnerable = false;
    private bool isKnockedBack = false;

    // Componentes
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

        CurrentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} tomou {damageAmount} de dano. Vida: {CurrentHealth}/{maxHealth}");

        // 1) Knockback (se houver atacante)
        if (attackerTransform != null && knockbackForce > 0f && rb != null && !rb.isKinematic)
        {
            StartCoroutine(ApplyKnockbackCoroutine(attackerTransform));
        }

        // 2) Piscar e invulnerabilidade curta
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlashAndInvulnerability());
        }
        else
        {
            // Garante que exista invulnerabilidade mesmo sem sprite (evita hits múltiplos)
            StartCoroutine(SimpleInvulnerability());
        }

        // 3) Trigger de animação de dano
        if (animator != null && CurrentHealth > 0)
        {
            animator.SetTrigger("Damage");
        }

        // 4) Checar morte
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            if (!isDead) StartCoroutine(HandleDeath());
        }
    }

    // Aplica knockback e impede controle por um tempo curto (isKnockedBack)
    private IEnumerator ApplyKnockbackCoroutine(Transform attacker)
    {
        if (rb == null || attacker == null) yield break;

        // Marca knockback
        isKnockedBack = true;

        // Direção do empurrão: sai do atacante
        Vector3 dir = (transform.position - attacker.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
        {
            // fallback para evitar NaN (se estiver exatamente na mesma posição)
            dir = transform.forward;
            dir.y = 0f;
        }
        dir.Normalize();

        // Zera velocidade horizontal atual para tornar o impulso consistente
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        // Aplica impulso ignorando massa (mais responsivo)
        rb.AddForce(dir * knockbackForce, ForceMode.VelocityChange);

        // Tempo em que o personagem fica "fora de controle" (não deve ser movido por jogador/AI)
        float t = 0f;
        while (t < knockbackDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // opcional: não zera a velocidade aqui para preservar física natural, apenas libera controle
        isKnockedBack = false;
    }

    // Piscar + invulnerabilidade
    private IEnumerator DamageFlashAndInvulnerability()
    {
        if (spriteRenderer == null) yield break;

        isInvulnerable = true;
        Color original = spriteRenderer.color;
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invulnerabilityTime)
        {
            spriteRenderer.color = visible ? original : new Color(1f, 0.3f, 0.3f, 0.8f); // alterna para vermelho translúcido
            visible = !visible;
            elapsed += blinkInterval;
            yield return new WaitForSeconds(blinkInterval);
        }

        // restaura e libera
        spriteRenderer.color = original;
        isInvulnerable = false;
    }

    // Fallback caso não haja spriteRenderer
    private IEnumerator SimpleInvulnerability()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }

    // Morte (piscar e desativar)
    private IEnumerator HandleDeath()
    {
        isDead = true;
        Debug.Log(gameObject.name + " morreu.");

        bool isPlayer = TryGetComponent<PlayerController3D>(out var playerController);
        bool isEnemy = TryGetComponent<EnemyController>(out var enemyController);

        // Dispara a animação de morte se existir
        bool hasDieTrigger = false;
        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                if (p.name == "Die")
                {
                    animator.SetTrigger("Die");
                    hasDieTrigger = true;
                    break;
                }
            }
        }

        // Desativa controladores de movimento/IA
        if (isPlayer) playerController.enabled = false;
        if (isEnemy) enemyController.enabled = false;

        // Para movimento
        if (rb != null) rb.linearVelocity = Vector3.zero;

        if (animator == null || !hasDieTrigger)
        {
            yield return new WaitForSeconds(disappearDelayAfterDeath);
        }
        else
        {
            // 1) Espera o Animator entrar no estado "Die" (ou timeout)
            float enterTimeout = 0.5f; // evita loop infinito se a transição não ocorrer
            float waited = 0f;
            // Wait one frame to allow transitions to start
            yield return null;

            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Die") && waited < enterTimeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // 2) Se entrou no estado Die, espera até que normalizedTime >= 1 (animação completa)
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Die"))
            {
                // Espera até o fim da animação de morte
                while (animator.GetCurrentAnimatorStateInfo(0).IsName("Die") &&
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                {
                    yield return null;
                }
            }
            else
            {
                // Se não entrou no Die por timeout, tenta usar o length como fallback
                float fallback = animator.GetCurrentAnimatorStateInfo(0).length;
                yield return new WaitForSeconds(fallback);
            }

            // 3) Ações pós-animação:
            if (isEnemy)
            {
                // congela no último frame (impede voltar para Idle)
                animator.speed = 0f;

                // espera um pouco congelado antes de sumir (usa disappearDelayAfterDeath)
                yield return new WaitForSeconds(disappearDelayAfterDeath);
            }
            else // isPlayer
            {
                // Para o player: não congelar o animator — deixa correr normalmente até o fim (já esperámos)
                // Opcional: mantém um pequeno delay antes de sumir para garantir feedback visual
                yield return new WaitForSeconds(disappearDelayAfterDeath);
            }
        }

        // Desativa física e colisão
        if (rb != null) rb.isKinematic = true;
        if (charCollider != null) charCollider.enabled = false;

        // Remove/Destrói o objeto (a escolha original era SetActive(false))
        gameObject.SetActive(false);
    }


    // Getters auxiliares para outros scripts saberem o estado
    public bool IsDead() => isDead;
    public bool IsInvulnerable() => isInvulnerable;
    public bool IsKnockedBack() => isKnockedBack;
}
