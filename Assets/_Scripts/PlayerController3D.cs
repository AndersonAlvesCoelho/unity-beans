using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float sprintForce = 12f;
    public float sprintDuration = 0.25f;
    public float sprintCooldown = 1.2f;

    [Header("Combate")]
    public Transform attackPoint;
    [Range(0.1f, 3f)] public float attackRange = 1.5f;
    public float attackDamage = 2.5f;
    public LayerMask enemyLayer;
    public float attackAnimationDuration = 0.4f;

    // Componentes
    private Rigidbody rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Estado interno
    private Vector2 rawInput;            // -1,0,1 input cru
    private Vector2 moveInput;           // input processado (normalizado se necessário)
    private bool canSprint = true;
    private bool isSprinting = false;
    private bool isAttacking = false;
    private float lastNonZeroHorizontal = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (attackPoint == null) Debug.LogError("PlayerController3D: AttackPoint não atribuído!");

        // Trava rotações físicas (evita girar ao colidir)
        // rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // Coleta e processa input (apenas leitura em Update)
        HandleMovementInput();
        HandleSprintInput();
        HandleAttackInput();

        // Flip do sprite (visual) - permitido mesmo durante ataque para manter a direção
        HandleFlip();

        // Atualiza parâmetros do animator com base em moveInput (consistente com a física)
        UpdateAnimator();
    }

    void HandleMovementInput()
    {
        // Pegamos input cru independente de estar sprintando; a aplicação do movimento é bloqueada se isSprinting/isAttacking
        rawInput.x = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        rawInput.y = Input.GetAxisRaw("Vertical");   // -1, 0, 1

        // Processamento: normaliza para evitar magnitude>1 em diagonais
        moveInput = rawInput;
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();
    }

    void HandleSprintInput()
    {
        // Apenas inicia sprint se permitido e não estivermos atacando
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space)) && canSprint && !isAttacking)
        {
            StartCoroutine(SprintDash());
        }
    }

    void HandleAttackInput()
    {
        // Só inicia ataque se não estivermos sprintando e não estivermos já atacando
        if (Input.GetButtonDown("Fire1") && !isSprinting && !isAttacking)
        {
            StartCoroutine(AttackSequence());
        }
    }

    void HandleFlip()
    {
        // Atualiza a direção apenas se houver input horizontal
        if (Mathf.Abs(rawInput.x) > 0.01f)
        {
            lastNonZeroHorizontal = rawInput.x;
            spriteRenderer.flipX = (rawInput.x < 0);
        }
        else
        {
            // Mantém última direção conhecida quando parado/vertical
            spriteRenderer.flipX = (lastNonZeroHorizontal < 0);
        }
    }

    IEnumerator SprintDash()
    {
        canSprint = false;
        isSprinting = true;
        animator.SetBool("IsSprinting", true);

        // Usa a direção atual (se zero, usa última horizontal)
        Vector3 dashDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dashDir.sqrMagnitude < 0.001f)
            dashDir = new Vector3(lastNonZeroHorizontal > 0 ? 1f : -1f, 0f, 0f);

        dashDir.Normalize();

        // Aplica velocidade instantânea (substitui velocity)
        Vector3 newVel = new Vector3(dashDir.x * sprintForce, rb.linearVelocity.y, dashDir.z * sprintForce);
        rb.linearVelocity = newVel;

        yield return new WaitForSeconds(sprintDuration);

        // Para o dash (mantém Y)
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        isSprinting = false;
        animator.SetBool("IsSprinting", false);

        yield return new WaitForSeconds(sprintCooldown);
        canSprint = true;
    }

    void FixedUpdate()
    {
        // Aplica movimento físico apenas se não estiver sprintando nem atacando
        if (!isSprinting && !isAttacking)
        {
            MoveCharacter();
        }
        else if (isAttacking)
        {
            // Durante ataque, bloqueia movimento horizontal mantendo Y intacto
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
        // se isSprinting: já foi tratado no SprintDash aplicando velocity diretamente
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        // Speed: magnitude real (0..1) com clamp
        float speed = Mathf.Clamp01(moveInput.magnitude);
        // Caso esteja atacando ou sprintando, force Speed = 0 para que blend tree vá para Idle/Attack
        float effectiveSpeed = (isAttacking || isSprinting) ? 0f : speed;

        // Horizontal no BlendTree: usamos ABS para manter apenas valores positivos (BlendTree com sprites à direita)
        animator.SetFloat("Horizontal", Mathf.Abs(moveInput.x));

        // Vertical: dependendo do seu BlendTree pode ser necessário inverter (-moveInput.y).
        // Se seus sprites consideram +Y = cima, deixe como abaixo. Se estavam invertidos, use -moveInput.y.
        animator.SetFloat("Vertical", moveInput.y); // ou animator.SetFloat("Vertical", -moveInput.y);

        animator.SetFloat("Speed", effectiveSpeed);
    }

    void MoveCharacter()
    {
        // Aplica velocidade baseada em moveInput processado
        Vector3 target = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        // Preserva componente y atual (gravidade)
        target.y = rb.linearVelocity.y;
        rb.linearVelocity = target;
    }

    IEnumerator AttackSequence()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");

        // Opcional: esperar até o frame de hit via AnimationEvent é melhor, mas esse delay funciona
        yield return new WaitForSeconds(0.1f);

        PerformDamageCheck();

        // Aguarda o tempo definido pra duração da animação de ataque
        yield return new WaitForSeconds(attackAnimationDuration);

        isAttacking = false;
    }

    void PerformDamageCheck()
    {
        if (attackPoint == null) return;

        Collider[] hit = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        foreach (Collider c in hit)
        {
            HealthSystem hs = c.GetComponent<HealthSystem>();
            if (hs != null) hs.TakeDamage(attackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
