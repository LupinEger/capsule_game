using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DinosaurAIController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;

    [Header("Settings")]
    public float detectionRange = 15f;
    public float attackRange = 4f;
    public float chaseSpeed = 4f;
    public float walkSpeed = 2f;

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float minWanderDistance = 3f;
    public float wanderInterval = 5f;

    [Header("Attack Settings")]
    public float attackDamage = 25f;
    public float attackCooldown = 2f;
    public float attackWindupTime = 0.5f; // Время перед нанесением урона
    public float attackDuration = 1f; // Общая длительность атаки

    [Header("Animation Parameters")]
    public string moveParameter = "Move";
    public string attackTrigger = "Attack";
    public int idleState = 0;
    public int walkState = 1;
    public int runState = 2;
    public int attackState = 3;

    private NavMeshAgent agent;
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private Vector3 startPosition;
    private PlayerHealth playerHealth;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        agent.speed = walkSpeed;
        startPosition = transform.position;

        // Находим компонент здоровья игрока
        FindPlayerHealth();

        StartCoroutine(AIUpdate());
    }

    void FindPlayerHealth()
    {
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
        }
    }

    IEnumerator AIUpdate()
    {
        while (true)
        {
            if (player != null && IsPlayerAlive())
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);

                if (distanceToPlayer <= detectionRange && !isAttacking)
                {
                    if (distanceToPlayer <= attackRange && canAttack)
                    {
                        StartCoroutine(Attack());
                    }
                    else
                    {
                        ChasePlayer();
                    }
                }
                else if (!isAttacking)
                {
                    Wander();
                }
            }
            else
            {
                // Если игрок не существует или мертв, переходим в режим блуждания
                if (!isAttacking)
                    Wander();
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    bool IsPlayerAlive()
    {
        return playerHealth == null || playerHealth.IsAlive();
    }

    void ChasePlayer()
    {
        if (!isChasing) isChasing = true;

        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
        SetAnimation(runState);
    }

    IEnumerator Attack()
    {
        isAttacking = true;
        isChasing = false;
        canAttack = false;

        agent.isStopped = true;
        SetAnimation(idleState);

        // Поворот к игроку
        yield return StartCoroutine(TurnTowardsPlayer());

        // Анимация атаки
        SetAnimation(attackState);
        if (!string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        // Нанесение урона (в середине атаки)
        yield return new WaitForSeconds(attackWindupTime);
        ApplyDamageToPlayer();

        // Завершение атаки
        yield return new WaitForSeconds(attackDuration - attackWindupTime);
        agent.isStopped = false;
        isAttacking = false;

        // КД атаки
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void ApplyDamageToPlayer()
    {
        if (playerHealth != null && playerHealth.IsAlive())
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    IEnumerator TurnTowardsPlayer()
    {
        if (player == null) yield break;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            float turnTime = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < turnTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, elapsedTime / turnTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRotation;
        }
    }

    void Wander()
    {
        if (isChasing) isChasing = false;

        agent.speed = walkSpeed;

        if (!agent.hasPath || agent.remainingDistance < 1f)
        {
            SetRandomWanderDestination();
        }

        SetAnimation(walkState);
    }

    void SetRandomWanderDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomPosition = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPosition, out hit, wanderRadius, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) >= minWanderDistance)
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }
    }

    void SetAnimation(int state)
    {
        if (animator != null && !string.IsNullOrEmpty(moveParameter))
        {
            animator.SetInteger(moveParameter, state);
        }
    }

    // Метод для внешнего управления (например, при получении урона)
    public void ForceChasePlayer()
    {
        if (!isAttacking)
        {
            isChasing = true;
            ChasePlayer();
        }
    }

    // Метод для сброса состояния
    public void ResetAI()
    {
        StopAllCoroutines();
        isChasing = false;
        isAttacking = false;
        canAttack = true;
        agent.isStopped = false;
        StartCoroutine(AIUpdate());
    }
}