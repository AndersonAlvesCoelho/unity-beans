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
    public float rangedAttackDistance = 120f;
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

        // LOG: Confirmação de inicialização e estado inicial
        Debug.Log($"[{gameObject.name}] Script iniciado. Estado inicial: {currentState}");
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

    void HandleIdleState() => rb.velocity = new Vector3(0, rb.velocity.y, 0);

    void HandlePatrolState()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) { currentState = AIState.Idle; return; }

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        float distance = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(targetPoint.position));

        if (distance < patrolPointThreshold)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            if (waitCoroutine == null)
            {
                // LOG: Início da espera na patrulha
                Debug.Log($"[{gameObject.name}] Chegou ao ponto de patrulha {currentPatrolIndex}. Iniciando espera.");
                waitCoroutine = StartCoroutine(WaitAndMoveToNextPoint());
            }
        }
        else
        {
            Vector3 dir3D = targetPoint.position - transform.position;
            lookDirection = new Vector2(dir3D.x, dir3D.z).normalized;
            Vector3 targetVelocity = new Vector3(lookDirection.x, 0, lookDirection.y) * patrolSpeed;
            targetVelocity.y = rb.velocity.y;
            rb.velocity = targetVelocity;
        }
    }

    void HandleFlip()
    {
        float horizontalVel = rb.velocity.x;

        if (Mathf.Abs(horizontalVel) > 0.01f)
            lastNonZeroHorizontal = horizontalVel;

        float targetYRotation = (lastNonZeroHorizontal < 0) ? 0f : 180f;
        Vector3 currentEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(currentEuler.x, targetYRotation, currentEuler.z);
    }

    IEnumerator WaitAndMoveToNextPoint()
    {
        isWaitingAtPatrolPoint = true;
        yield return new WaitForSeconds(patrolWaitTime);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaitingAtPatrolPoint = false;
        waitCoroutine = null;

        // LOG: Fim da espera, movendo para o próximo ponto
        Debug.Log($"[{gameObject.name}] Espera terminada. Movendo para ponto {currentPatrolIndex}.");
    }

    void HandleChaseState()
    {
        StopWaitingCoroutineIfNeeded();
        if (playerTransform == null) { GoBackToDefaultState(); return; }

        // usar posições planas para comparação de distância
        float distance = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(playerTransform.position));

        //Debug para ajudar no dev (JÁ EXISTENTE E MANTIDO)
        Debug.Log($"{name} - State: Chasing | Dist (planar): {distance:F2}");

        // RANGED
        if (enemyType == BehaviorType.Ranged && distance <= rangedAttackDistance)
        {
            // LOG: Transição de Chase para RangedAttack
            Debug.Log($"[{gameObject.name}] [CHASE] -> Em alcance Ranged (Dist: {distance:F2}). Mudando para RANGED_ATTACKING.");
            currentState = AIState.RangedAttacking;
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }   
        // MELEE 
        else if (enemyType != BehaviorType.Ranged && distance <= Mathf.Max(stoppingDistance, attackRange))
        {
            Debug.Log($"[{gameObject.name}] [CHASE] -> Em alcance Melee (Dist: {distance:F2}). Mudando para ATTACKING.");
            currentState = AIState.Attacking;
            rb.velocity = new Vector3(0, rb.velocity.y, 0); // força parada antes de atacar
            return;
        }

        // Ainda longe: persegue normalmente
        Vector3 targetVelocity = new Vector3(lookDirection.x, 0, lookDirection.y) * chaseSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.velocity = targetVelocity;
    }

    void HandleAttackState()
    {
        StopWaitingCoroutineIfNeeded();
        if (playerTransform == null) { GoBackToDefaultState(); return; }

        float distance = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(playerTransform.position));

        // se afastar mais do que o alcance + margem -> volta a perseguir
        if (distance > attackRange + 0.5f)
        {
            // LOG: Transição de Attack para Chase (jogador fugiu)
            Debug.Log($"[{gameObject.name}] [ATTACK] -> Player saiu do alcance (Dist: {distance:F2}). Voltando para CHASING.");
            currentState = AIState.Chasing;
            return;
        }

        // mantém parado enquanto ataca
        rb.velocity = new Vector3(0, rb.velocity.y, 0);

        if (Time.time >= nextAttackTime)
        {
            // LOG: Execução do ataque melee
            Debug.Log($"[{gameObject.name}] [ATTACK] -> Executando ataque MELEE.");
            PerformMeleeAttack();
            nextAttackTime = Time.time + attackRate;
        }
    }

    void HandleRangedAttackState()
    {
        StopWaitingCoroutineIfNeeded();
        if (playerTransform == null) { GoBackToDefaultState(); return; }

        float distance = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(playerTransform.position));

        if (distance < retreatDistance)
        {
            // LOG: Ranged está recuando
            Debug.Log($"[{gameObject.name}] [RANGED] -> Player muito perto (Dist: {distance:F2}). Recuando.");
            Vector3 dirAway = (transform.position - playerTransform.position).normalized;
            Vector3 targetVelocity = new Vector3(dirAway.x, 0, dirAway.y) * chaseSpeed;
            targetVelocity.y = rb.velocity.y;
            rb.velocity = targetVelocity;
        }
        else if (distance > rangedAttackDistance)
        {
            // LOG: Transição de RangedAttack para Chase (jogador muito longe)
            Debug.Log($"[{gameObject.name}] [RANGED] -> Player saiu do alcance máximo (Dist: {distance:F2}). Voltando para CHASING.");
            currentState = AIState.Chasing;
        }
        else
        {
            if (distance > rangedStoppingDistance)
            {
                // LOG: Ranged está se aproximando (dentro do alcance, mas fora do stopping distance)
                Debug.Log($"[{gameObject.name}] [RANGED] -> Em alcance, aproximando para 'stopping distance' (Dist: {distance:F2}).");
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                lookDirection = new Vector2(dirToPlayer.x, dirToPlayer.z);
                Vector3 targetVelocity = new Vector3(lookDirection.x, 0, lookDirection.y) * chaseSpeed;
                targetVelocity.y = rb.velocity.y;
                rb.velocity = targetVelocity;
            }
            else
            {
                // LOG: Ranged está parado e pronto para atirar
                Debug.Log($"[{gameObject.name}] [RANGED] -> Em posição de ataque (Dist: {distance:F2}). Parado.");
                rb.velocity = new Vector3(0, rb.velocity.y, 0);

                if (Time.time >= nextAttackTime)
                {
                    // LOG: Execução do ataque ranged
                    Debug.Log($"[{gameObject.name}] [RANGED] -> Executando ataque RANGED.");
                    FireProjectile();
                    nextAttackTime = Time.time + rangedAttackRate;
                }
            }
        }
    }

    void UpdateAnimator()
    {
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
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

        // Draw gizmo sphere apenas no editor via OnDrawGizmosSelected, aqui deixei para debug
        foreach (Collider playerCollider in hitPlayers)
        {
            // LOG: Confirmação de acerto melee
            Debug.Log($"[{gameObject.name}] Ataque Melee ACERTOU: {playerCollider.name}");
            HealthSystem playerHealth = playerCollider.GetComponent<HealthSystem>();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
        }
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        animator.SetTrigger("Attack"); // Ranged attack (cuspir)

        // LOG: Disparo de projétil
        Debug.Log($"[{gameObject.name}] Projétil disparado de {firePoint.position}!");

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Vector3 dirToPlayer = (playerTransform.position - firePoint.position);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            if (projRb.isKinematic) projRb.isKinematic = false;
            projRb.velocity = dirToPlayer * projectileSpeed;
        }

        Destroy(projectile, 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            // LOG: Detecção inicial do player
            Debug.Log($"[{gameObject.name}] PLAYER DETECTADO (OnTriggerEnter por {other.name}). Mudando para CHASING.");
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
                // LOG: Perda do player pelo trigger
                Debug.Log($"[{gameObject.name}] PLAYER PERDIDO (OnTriggerExit por {other.name}). Voltando ao estado padrão.");
                // perda imediata do alvo removida — volte ao default com tolerância
                playerTransform = null;
                GoBackToDefaultState();
            }
        }
    }

    void GoBackToDefaultState()
    {
        AIState newState = (enemyType == BehaviorType.Patrol && patrolPoints != null && patrolPoints.Length > 0)
                                ? AIState.Patrolling : AIState.Idle;
        
        // LOG: Confirmação do estado de retorno
        Debug.Log($"[{gameObject.name}] GoBackToDefaultState. Novo estado: {newState}");
        currentState = newState;
    }

    void StopWaitingCoroutineIfNeeded()
    {
        if (waitCoroutine != null)
        {
            // LOG: Interrupção da coroutine de espera (provavelmente por ver o player)
            Debug.Log($"[{gameObject.name}] Coroutine de espera (Patrulha) interrompida.");
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
            isWaitingAtPatrolPoint = false;
        }
    }

    Vector3 GetPlanarPosition(Vector3 pos) => new Vector3(pos.x, 0, pos.z);

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}