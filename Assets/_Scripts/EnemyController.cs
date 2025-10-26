// EnemyController.cs (VERSÃO FINAL COM TIPOS DE COMPORTAMENTO - CORRIGIDO)

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
    [Range(0.1f, 3f)] public float attackRange = 1f;

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
    private Vector2 lookDirection = new Vector2(0, -1);
    private float nextAttackTime = 0f;
    private int currentPatrolIndex = 0;
    private bool isWaitingAtPatrolPoint = false;
    private Coroutine waitCoroutine = null;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
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
        // Validações gerais
        if (playerLayer == 0) Debug.LogWarning(gameObject.name + ": Player Layer não configurada!");

        // Validações baseadas no TIPO
        switch (enemyType)
        {
            case BehaviorType.Stationary:
            case BehaviorType.Patrol:
                // Inimigos Melee PRECISAM do AttackPoint
                if (attackPoint == null)
                    Debug.LogError(gameObject.name + " (" + enemyType + "): AttackPoint não atribuído!");
                break;

            case BehaviorType.Ranged:
                // Inimigo Ranged PRECISA do Prefab e FirePoint
                if (projectilePrefab == null)
                    Debug.LogError(gameObject.name + " (Ranged): Projectile Prefab não definido!");
                if (firePoint == null)
                    Debug.LogError(gameObject.name + " (Ranged): Fire Point não definido!");
                // Validações de distância opcionais
                if (rangedStoppingDistance >= rangedAttackDistance) Debug.LogWarning(gameObject.name + ": RangedStoppingDistance deveria ser MENOR que RangedAttackDistance.");
                if (retreatDistance >= rangedStoppingDistance) Debug.LogWarning(gameObject.name + ": RetreatDistance deveria ser MENOR que RangedStoppingDistance.");
                break;
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            Vector3 directionToPlayer3D = playerTransform.position - transform.position;
            lookDirection = new Vector2(directionToPlayer3D.x, directionToPlayer3D.z).normalized;
        }
    }

    void FixedUpdate()
    {
        if (!isWaitingAtPatrolPoint)
        {
            ExecuteCurrentStateLogic();
        }
        UpdateAnimator();
    }

    void ExecuteCurrentStateLogic()
    {
        switch (currentState)
        {
            case AIState.Idle:            HandleIdleState();           break;
            case AIState.Patrolling:      HandlePatrolState();         break;
            case AIState.Chasing:         HandleChaseState();          break;
            case AIState.Attacking:       HandleAttackState();         break;
            case AIState.RangedAttacking: HandleRangedAttackState();   break;
        }
    }

    void HandleIdleState()
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }

    void HandlePatrolState()
    {
         if (patrolPoints == null || patrolPoints.Length == 0) { currentState = AIState.Idle; return; }

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        // --- USA A FUNÇÃO GetPlanarPosition ---
        float distanceToPoint = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(targetPoint.position));

        if (distanceToPoint < patrolPointThreshold)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            if (waitCoroutine == null) waitCoroutine = StartCoroutine(WaitAndMoveToNextPoint());
        }
        else
        {
            Vector3 directionToPoint3D = targetPoint.position - transform.position;
            lookDirection = new Vector2(directionToPoint3D.x, directionToPoint3D.z).normalized;
            Vector3 targetVelocity = new Vector3(lookDirection.x, 0, lookDirection.y) * patrolSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
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
        {
            currentState = AIState.RangedAttacking;
        }
        else if (enemyType != BehaviorType.Ranged && distance <= stoppingDistance)
        {
            currentState = AIState.Attacking;
        }
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

        // Decide se recua, atira ou persegue novamente
        if (distance < retreatDistance) // Jogador muito perto -> Recuar
        {
            Vector3 directionAway = (transform.position - playerTransform.position).normalized;
            Vector3 targetVelocity = new Vector3(directionAway.x, 0, directionAway.z) * chaseSpeed; // Usa chaseSpeed para recuar rápido
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;     
        }
        else if (distance > rangedAttackDistance) // Jogador muito longe -> Perseguir
        {
            currentState = AIState.Chasing;
        }
        else // Distância na zona de ataque (entre retreat e rangedAttack)
        {
            // Se ainda estiver longe demais para PARAR e atirar -> Aproximar
            if (distance > rangedStoppingDistance)
            {
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
                lookDirection = new Vector2(directionToPlayer.x, directionToPlayer.z); // Atualiza para onde olhar
                Vector3 targetVelocity = new Vector3(lookDirection.x, 0, lookDirection.y) * chaseSpeed; // Usa chaseSpeed para aproximar
                targetVelocity.y = rb.linearVelocity.y; // Mantém gravidade/velocidade Y anterior
                rb.linearVelocity = targetVelocity;     // Define nova velocidade
            }
            // Se estiver na distância ideal para PARAR e atirar (entre retreat e rangedStopping)
            else
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Para movimento horizontal/profundidade

                // Atira se cooldown permitir
                if (Time.time >= nextAttackTime)
                {
                    FireProjectile(); // Chama a função de disparo
                    nextAttackTime = Time.time + rangedAttackRate; // Define próximo tempo de ataque
                }
            }
        }
    }

    void UpdateAnimator()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        bool isEffectivelyMoving = horizontalVelocity.magnitude > 0.1f;
        animator.SetFloat("Speed", isEffectivelyMoving ? 1f : 0f);

        if (isEffectivelyMoving)
        {
            animator.SetFloat("Horizontal", lookDirection.x);
            animator.SetFloat("Vertical", lookDirection.y);
        }
        else
        {
            animator.SetFloat("LastHorizontal", lookDirection.x);
            animator.SetFloat("LastVertical", lookDirection.y);
        }
    }

    void PerformMeleeAttack()
    {
        if (attackPoint == null) return;

        animator.SetTrigger("Attack");
        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);
        foreach (Collider playerCollider in hitPlayers)
        {
            HealthSystem playerHealth = playerCollider.GetComponent<HealthSystem>();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
        }
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        animator.SetTrigger("Attack");

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        if (projectile == null)
        {
            return;
        }

        Vector3 directionToPlayer = (playerTransform.position - firePoint.position);
        directionToPlayer.y = 0;
        directionToPlayer.Normalize();

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            if(projectileRb.isKinematic) projectileRb.isKinematic = false;
            projectileRb.linearVelocity = directionToPlayer * projectileSpeed;
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

    // Helper para pegar a posição no plano XZ (ignora Y)
    Vector3 GetPlanarPosition(Vector3 position)
    {
        return new Vector3(position.x, 0, position.z); 
    }

}