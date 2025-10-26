using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float sprintForce = 12f;
    public float sprintDuration = 0.25f;
    public float sprintCooldown = 1.2f;

    [Header("Combate")]
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public float attackDamage = 2.5f;
    public LayerMask enemyLayer;
    public float attackAnimationDuration = 0.4f;

    // Componentes
    private Rigidbody rb;
    private Animator animator;

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

        if (attackPoint == null)
            Debug.LogError("PlayerController3D: AttackPoint não atribuído!");

        // Trava rotações físicas (evita girar ao colidir)
        // rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        HandleMovementInput();
        HandleSprintInput();
        HandleAttackInput();

        // Controla direção visual
        HandleFlip();

        UpdateAnimator();
    }

    void HandleMovementInput()
    {
        rawInput.x = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        rawInput.y = Input.GetAxisRaw("Vertical");   // -1, 0, 1

        moveInput = rawInput;
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();
    }

    void HandleSprintInput()
    {
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space)) && canSprint && !isAttacking)
        {
            StartCoroutine(SprintDash());
        }
    }

    void HandleAttackInput()
    {
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

            // Define rotação em Y (direita = 0°, esquerda = 180°)
            float targetYRotation = (rawInput.x > 0) ? 0f : 180f;
            Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);
            transform.rotation = targetRotation;
        }
        else
        {
            // Mantém última direção conhecida
            float targetYRotation = (lastNonZeroHorizontal > 0) ? 0f : 180f;
            Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    IEnumerator SprintDash()
    {
        canSprint = false;
        isSprinting = true;
        animator.SetBool("IsSprinting", true);

        Vector3 dashDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dashDir.sqrMagnitude < 0.001f)
            dashDir = new Vector3(lastNonZeroHorizontal > 0 ? 1f : -1f, 0f, 0f);

        dashDir.Normalize();

        Vector3 newVel = new Vector3(dashDir.x * sprintForce, rb.linearVelocity.y, dashDir.z * sprintForce);
        rb.linearVelocity = newVel;

        yield return new WaitForSeconds(sprintDuration);

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        isSprinting = false;
        animator.SetBool("IsSprinting", false);

        yield return new WaitForSeconds(sprintCooldown);
        canSprint = true;
    }

    void FixedUpdate()
    {
        if (!isSprinting && !isAttacking)
        {
            MoveCharacter();
        }
        else if (isAttacking)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = Mathf.Clamp01(moveInput.magnitude);
        float effectiveSpeed = (isAttacking || isSprinting) ? 0f : speed;

        animator.SetFloat("Horizontal", Mathf.Abs(moveInput.x));
        animator.SetFloat("Vertical", moveInput.y);
        animator.SetFloat("Speed", effectiveSpeed);
    }

    void MoveCharacter()
    {
        Vector3 target = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        target.y = rb.linearVelocity.y;
        rb.linearVelocity = target;
    }

    IEnumerator AttackSequence()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.1f);
        PerformDamageCheck();

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
