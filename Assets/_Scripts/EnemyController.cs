using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class EnemyController : MonoBehaviour
{
    public enum BehaviorType { Stationary, Patrol, Ranged }
    [Header("Comportamento")]
    public BehaviorType enemyType = BehaviorType.Patrol;

    private enum AIState { Idle, Patrolling, Chasing, Attacking, RangedAttacking }
    private AIState currentState;

    [Header("Referências")]
    [SerializeField] private Transform playerTransform;

    [Header("Movimento")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3f;
    public float stoppingDistance = 0.8f;
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    public float patrolPointThreshold = 0.6f;

    [Header("Combate Melee")]
    public float attackDamage = 2f;
    public float attackRate = 3f;
    public Transform attackPoint;
    public float attackRange = 1f;

    [Header("Combate a Distância (Ranged)")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float rangedAttackDistance = 10f;
    public float rangedStoppingDistance = 8f;
    public float retreatDistance = 5f;
    public float projectileSpeed = 15f;
    public float rangedAttackRate = 2f;

    [Header("Detecção")]
    public LayerMask playerLayer;

    private Animator animator;
    private Rigidbody rb;
    private Vector2 lookDirection = Vector2.right; // padrão olhando para direita
    private float nextAttackTime = 0f;
    private int currentPatrolIndex = 0;
    private bool isWaitingAtPatrolPoint = false;
    private Coroutine waitCoroutine = null;

    private SpriteRenderer spriteRenderer;
    private float lastNonZeroHorizontal = 1f; // inicializamos olhando para direita


    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>(); 
        
        InitializeStartingState();
        ValidateSettings();
    }

    void InitializeStartingState()
    {
        switch (enemyType)
        {
            case BehaviorType.Stationary:
            case BehaviorType.Ranged:
                currentState = AIState.Idle;
                break;
            case BehaviorType.Patrol:
                currentState = (patrolPoints == null || patrolPoints.Length == 0) ? AIState.Idle : AIState.Patrolling;
                if (currentState == AIState.Idle) Debug.LogWarning(gameObject.name + ": Patrulha sem pontos. Começando Idle.");
                break;
            default:
                currentState = AIState.Idle;
                break;
        }
    }

    void ValidateSettings()
    {
        if (playerLayer == 0) Debug.LogWarning(gameObject.name + ": Player Layer não configurada!");
        if (enemyType != BehaviorType.Ranged && attackPoint == null)
            Debug.LogError(gameObject.name + " (" + enemyType + "): AttackPoint não atribuído!");
        if (enemyType == BehaviorType.Ranged)
        {
            if (projectilePrefab == null) Debug.LogError(gameObject.name + " (Ranged): Projectile Prefab não definido!");
            if (firePoint == null) Debug.LogError(gameObject.name + " (Ranged): Fire Point não definido!");
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            Vector3 dir3D = playerTransform.position - transform.position;
            lookDirection = new Vector2(dir3D.x, dir3D.z).normalized;
        }
    }

    void FixedUpdate()
    {
        if (!isWaitingAtPatrolPoint)
            ExecuteCurrentStateLogic();

        UpdateAnimator();
        HandleFlip();
    }

    void ExecuteCurrentStateLogic()
    {
        switch (currentState)
        {
            case AIState.Idle: HandleIdleState(); break;
            case AIState.Patrolling: HandlePatrolState(); break;
            case AIState.Chasing: HandleChaseState(); break;
            case AIState.Attacking: HandleAttackState(); break;
            case AIState.RangedAttacking: HandleRangedAttackState(); break;
        }
    }

    void HandleIdleState() => rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

    void HandlePatrolState()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) { currentState = AIState.Idle; return; }

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        float distance = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(targetPoint.position));

        if (distance < patrolPointThreshold)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            if (waitCoroutine == null) waitCoroutine = StartCoroutine(WaitAndMoveToNextPoint());
        }
        else
        {
            Vector3 dir3D = targetPoint.position - transform.position;
            lookDirection = new Vector2(dir3D.x, dir3D.z).normalized;
            Vector3 targetVelocity = new Vector3(lookDirection.x, 0, lookDirection.y) * patrolSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    void HandleFlip()
    {
        // Pegamos velocidade horizontal
        float horizontalVel = rb.linearVelocity.x;

        if (Mathf.Abs(horizontalVel) > 0.01f)
        {
            lastNonZeroHorizontal = horizontalVel;
            spriteRenderer.flipX = (horizontalVel < 0); // se movendo para esquerda, flipX = true
        }
        else
        {
            // mantém última direção conhecida
            spriteRenderer.flipX = (lastNonZeroHorizontal < 0);
        }
    }

    IEnumerator WaitAndMoveToNextPoint()
    {
        isWaitingAtPatrolPoint = true;
        yield return new WaitForSeconds(patrolWaitTime);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaitingAtPatrolPoint = false;
        waitCoroutine = null;
    }

    void HandleChaseState()
    {
        StopWaitingCoroutineIfNeeded();
        if (playerTransform == null) { GoBackToDefaultState(); return; }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (enemyType == BehaviorType.Ranged && distance <= rangedAttackDistance)
            currentState = AIState.RangedAttacking;
        else if (enemyType != BehaviorType.Ranged && distance <= stoppingDistance)
            currentState = AIState.Attacking;
        else
        {
            Vector3 targetVelocity = new Vector3(lookDirection.x, 0, lookDirection.y) * chaseSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    void HandleAttackState()
    {
        StopWaitingCoroutineIfNeeded();
        if (playerTransform == null) { GoBackToDefaultState(); return; }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > stoppingDistance) { currentState = AIState.Chasing; return; }

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

        if (Time.time >= nextAttackTime)
        {
            PerformMeleeAttack();
            nextAttackTime = Time.time + attackRate;
        }
    }

    void HandleRangedAttackState()
    {
        StopWaitingCoroutineIfNeeded();
        if (playerTransform == null) { GoBackToDefaultState(); return; }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance < retreatDistance)
        {
            Vector3 dirAway = (transform.position - playerTransform.position).normalized;
            Vector3 targetVelocity = new Vector3(dirAway.x, 0, dirAway.y) * chaseSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
        else if (distance > rangedAttackDistance)
        {
            currentState = AIState.Chasing;
        }
        else
        {
            if (distance > rangedStoppingDistance)
            {
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                lookDirection = new Vector2(dirToPlayer.x, dirToPlayer.z);
                Vector3 targetVelocity = new Vector3(lookDirection.x, 0, lookDirection.y) * chaseSpeed;
                targetVelocity.y = rb.linearVelocity.y;
                rb.linearVelocity = targetVelocity;
            }
            else
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

                if (Time.time >= nextAttackTime)
                {
                    FireProjectile();
                    nextAttackTime = Time.time + rangedAttackRate;
                }
            }
        }
    }

    void UpdateAnimator()
    {
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        bool moving = horizontalVel.magnitude > 0.1f;
        animator.SetFloat("Speed", moving ? 1f : 0f);
        animator.SetFloat("Horizontal", lookDirection.x);
        animator.SetFloat("Vertical", lookDirection.y);
    }

    void PerformMeleeAttack()
    {
        if (attackPoint == null) return;

        animator.SetTrigger("Attack"); // Para melee, animação de morder
        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        foreach (Collider playerCollider in hitPlayers)
        {
            HealthSystem playerHealth = playerCollider.GetComponent<HealthSystem>();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
        }
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        animator.SetTrigger("Attack"); // Ranged attack (cuspir)

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Vector3 dirToPlayer = (playerTransform.position - firePoint.position);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            if (projRb.isKinematic) projRb.isKinematic = false;
            projRb.linearVelocity = dirToPlayer * projectileSpeed;
        }

        Destroy(projectile, 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            StopWaitingCoroutineIfNeeded();
            playerTransform = other.transform;
            currentState = AIState.Chasing;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            if (other.transform == playerTransform)
            {
                playerTransform = null;
                GoBackToDefaultState();
            }
        }
    }

    void GoBackToDefaultState()
    {
        currentState = (enemyType == BehaviorType.Patrol && patrolPoints != null && patrolPoints.Length > 0)
                        ? AIState.Patrolling : AIState.Idle;
    }

    void StopWaitingCoroutineIfNeeded()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
            isWaitingAtPatrolPoint = false;
        }
    }

    Vector3 GetPlanarPosition(Vector3 pos) => new Vector3(pos.x, 0, pos.z);
}
