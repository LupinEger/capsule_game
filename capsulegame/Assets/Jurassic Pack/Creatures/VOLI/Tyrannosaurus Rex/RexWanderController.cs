using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RexWanderController : MonoBehaviour
{
    private Rex rex;
    private Animator anim;
    private Rigidbody body;
    private Transform player;

    [Header("Wander Settings")]
    public float wanderRadius = 20f;
    public float minWanderDistance = 8f;
    public float maxWanderDistance = 15f;
    public float wanderInterval = 5f;
    public float turnSpeed = 1f;

    [Header("Chase Settings")]
    public float detectionRange = 15f;
    public float attackRange = 4f;
    public float chaseSpeed = 128f;
    public float walkSpeed = 50f;
    public LayerMask obstacleLayers = -1;

    [Header("Animation Settings")]
    public float animationTransitionTime = 0.3f;

    [Header("Debug")]
    public bool showDebug = true;
    public bool drawGizmos = true;

    private Vector3 currentWanderTarget;
    private bool isWandering = false;
    private bool isChasing = false;
    private bool isAttacking = false;
    private Coroutine behaviorCoroutine;
    private Coroutine movementCoroutine;
    private float lastWanderTime;

    // Переменные для интеграции с системой Rex
    private bool wasUsingAI;
    private float originalAnimSpeed;

    void Start()
    {
        rex = GetComponent<Rex>();
        anim = rex.anm;
        body = rex.body;

        if (body == null)
        {
            Debug.LogError("Rigidbody not found!");
            return;
        }

        // Сохраняем оригинальные настройки
        wasUsingAI = rex.useAI;
        originalAnimSpeed = rex.animSpeed;

        // Находим игрока по тегу
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure player has 'Player' tag.");
        }

        // Отключаем встроенный AI и включаем наш контроллер
        rex.useAI = false;
        rex.animSpeed = 1.0f; // Убеждаемся что анимации работают
        body.WakeUp();

        InitializeWanderBehavior();
    }

    void InitializeWanderBehavior()
    {
        DebugLog("Wander behavior initialized");
        StartBehavior();
    }

    void StartBehavior()
    {
        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);

        behaviorCoroutine = StartCoroutine(BehaviorLoop());
    }

    IEnumerator BehaviorLoop()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            // Проверяем видимость игрока
            bool canSeePlayer = CheckPlayerVisibility();

            if (canSeePlayer && !isAttacking)
            {
                // Начинаем преследование
                if (!isChasing)
                {
                    StartChase();
                }
                yield return StartCoroutine(ChaseRoutine());
            }
            else
            {
                // Возвращаемся к блужданию
                if (isChasing)
                {
                    StopChase();
                }

                if (!isWandering && !isAttacking)
                {
                    yield return StartCoroutine(WanderRoutine());
                }
            }

            yield return null;
        }
    }

    bool CheckPlayerVisibility()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Проверяем дистанцию
        if (distanceToPlayer > detectionRange) return false;

        // Проверяем прямую видимость
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up * 2f, directionToPlayer, out hit, detectionRange, obstacleLayers))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                return true;
            }
        }

        return false;
    }

    void StartChase()
    {
        DebugLog("Starting chase!");
        isChasing = true;
        StopWander();
    }

    void StopChase()
    {
        DebugLog("Stopping chase");
        isChasing = false;

        // Сбрасываем анимации через систему Rex
        if (!isAttacking)
        {
            ResetRexAnimations();
        }
    }

    IEnumerator ChaseRoutine()
    {
        while (isChasing && CheckPlayerVisibility())
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Поворачиваемся к игроку
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

            if (distanceToPlayer <= attackRange)
            {
                // Атака!
                yield return StartCoroutine(AttackRoutine());
            }
            else
            {
                // Бег к игроку - используем прямую установку анимаций
                SetRexRunAnimation();

                // Двигаемся вперед
                rex.Move(transform.forward, chaseSpeed);
            }

            yield return null;
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        DebugLog("Attacking player!");

        // Останавливаем движение
        body.linearVelocity = Vector3.zero;

        // Устанавливаем анимацию атаки через систему Rex
        anim.SetBool("Attack", true);
        anim.SetInteger("Move", 0); // Останавливаем движение

        // Ждем немного перед нанесением урона
        yield return new WaitForSeconds(0.5f);

        // Наносим урон игроку
        ApplyDamageToPlayer();

        // Ждем завершения атаки
        yield return new WaitForSeconds(1.0f);

        // Сбрасываем атаку
        anim.SetBool("Attack", false);
        isAttacking = false;

        // Короткая пауза между атаками
        yield return new WaitForSeconds(0.5f);
    }

    void ApplyDamageToPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange * 1.5f)
        {
            // Здесь должна быть логика нанесения урона игроку
            // Например: 
            // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            // if (playerHealth != null) playerHealth.TakeDamage(10);
            DebugLog("Player takes damage! Distance: " + distanceToPlayer.ToString("F1"));
        }
    }

    IEnumerator WanderRoutine()
    {
        isWandering = true;

        while (!isChasing && isWandering && !isAttacking)
        {
            // Ждем перед следующим блужданием
            if (Time.time - lastWanderTime < wanderInterval)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Выбираем случайную точку для блуждания
            currentWanderTarget = GetRandomWanderPoint();
            DebugLog($"New wander target: {currentWanderTarget}");

            // Двигаемся к точке
            yield return StartCoroutine(MoveToWanderTarget());

            // Короткая пауза на точке
            yield return new WaitForSeconds(Random.Range(1f, 3f));

            lastWanderTime = Time.time;
        }

        isWandering = false;
    }

    Vector3 GetRandomWanderPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Проверяем навигационную доступность
        if (CheckPositionReachable(randomPoint))
        {
            return randomPoint;
        }

        // Если точка недоступна, пробуем другую
        for (int i = 0; i < 5; i++)
        {
            randomCircle = Random.insideUnitCircle * wanderRadius;
            randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            if (CheckPositionReachable(randomPoint))
            {
                return randomPoint;
            }
        }

        // Если все точки недоступны, возвращаем оригинальную
        return transform.position + transform.forward * minWanderDistance;
    }

    bool CheckPositionReachable(Vector3 position)
    {
        // Простая проверка - можно добавить NavMesh проверку если используется AI
        float distance = Vector3.Distance(transform.position, position);
        return distance >= minWanderDistance && distance <= maxWanderDistance;
    }

    IEnumerator MoveToWanderTarget()
    {
        float moveTimer = 0f;
        float maxMoveTime = 25f;

        // Устанавливаем анимацию ходьбы
        SetRexWalkAnimation();

        while (isWandering && moveTimer < maxMoveTime)
        {
            Vector3 direction = (currentWanderTarget - transform.position).normalized;
            direction.y = 0;

            float distanceToTarget = Vector3.Distance(transform.position, currentWanderTarget);

            if (distanceToTarget <= 3f)
            {
                DebugLog("Reached wander target");
                break;
            }

            if (direction != Vector3.zero)
            {
                // Плавный поворот
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

                // Двигаемся вперед используя систему Rex
                rex.Move(transform.forward, walkSpeed);
            }

            // Проверяем не застряли ли мы
            if (moveTimer > 5f && body.linearVelocity.magnitude < 0.5f)
            {
                DebugLog("Seems stuck, choosing new target");
                break;
            }

            moveTimer += Time.deltaTime;
            yield return null;
        }

        // Останавливаем движение
        ResetRexAnimations();
        body.linearVelocity = Vector3.zero;
    }

    void StopWander()
    {
        isWandering = false;
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
    }

    void SetRexWalkAnimation()
    {
        // Устанавливаем анимацию ходьбы через параметры аниматора
        anim.SetInteger("Move", 1);
        anim.SetBool("Attack", false);
    }

    void SetRexRunAnimation()
    {
        // Устанавливаем анимацию бега через параметры аниматора
        anim.SetInteger("Move", 2);
        anim.SetBool("Attack", false);
    }

    void ResetRexAnimations()
    {
        // Сбрасываем все анимации к спокойному состоянию
        anim.SetInteger("Move", 0);
        anim.SetBool("Attack", false);
        anim.SetInteger("Idle", -1);
    }

    void Update()
    {
        // Визуальная отладка
        if (showDebug)
        {
            if (isWandering)
            {
                Debug.DrawLine(transform.position, currentWanderTarget, Color.yellow);
            }
            if (isChasing && player != null)
            {
                Debug.DrawLine(transform.position, player.position, Color.red);
            }
        }

        // Логирование состояния для отладки
        if (showDebug && Time.frameCount % 60 == 0)
        {
            DebugLog($"State: Wandering={isWandering}, Chasing={isChasing}, Attacking={isAttacking}, Velocity={body.linearVelocity.magnitude:F1}");
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Радиус блуждания
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // Радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Текущая цель блуждания
        if (Application.isPlaying && isWandering)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentWanderTarget, 0.5f);
            Gizmos.DrawLine(transform.position, currentWanderTarget);
        }
    }

    void DebugLog(string message)
    {
        if (showDebug)
        {
            Debug.Log($"[RexWander] {message}");
        }
    }

    public void StopBehavior()
    {
        if (behaviorCoroutine != null)
        {
            StopCoroutine(behaviorCoroutine);
            behaviorCoroutine = null;
        }

        StopWander();
        StopChase();

        // Восстанавливаем оригинальные настройки Rex
        rex.useAI = wasUsingAI;
        rex.animSpeed = originalAnimSpeed;

        // Сбрасываем анимации
        ResetRexAnimations();

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
        }

        isWandering = false;
        isChasing = false;
        isAttacking = false;
    }

    void OnDestroy()
    {
        StopBehavior();
    }

    void OnDisable()
    {
        StopBehavior();
    }
}
