using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Класс для настройки каждой точки
[System.Serializable]
public class AnkyWaypoint
{
    public Transform point;
    public WaypointState state;
    public MovementType movementType;
    public float minStayTime = 3f;
    public float maxStayTime = 8f;
    public float priority = 1f;
}

public enum WaypointState
{
    Stand,      // Просто стоять
    Eat,        // Есть
    Sleep       // Спать
}

public enum MovementType
{
    Walk,       // Шаг
    Run         // Бег
}

public class AnkyWaypointController : MonoBehaviour
{
    private Anky anky;
    private Animator anim;
    private Rigidbody body;

    [Header("Waypoint System")]
    public List<AnkyWaypoint> waypoints = new List<AnkyWaypoint>();
    public float arrivalDistance = 3f;
    public float turnSpeed = 2f;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float acceleration = 8f;

    [Header("Behavior Settings")]
    public float minDelayBetweenPoints = 1f;
    public float maxDelayBetweenPoints = 3f;
    public float maxMoveTime = 30f;

    [Header("Animation Settings")]
    public int standAnimation = 0;    // Простая стойка
    public int eatAnimation = 1;      // Еда
    public int sleepAnimation = 6;    // Сон

    [Header("Debug")]
    public bool showDebug = true;

    private int currentWaypointIndex = -1;
    private bool isMoving = false;
    private bool isPerformingAction = false;
    private Coroutine behaviorCoroutine;
    private Coroutine movementCoroutine;
    private List<AnkyWaypoint> sortedWaypoints = new List<AnkyWaypoint>();
    private Vector3 currentTargetPosition;

    // Переменные для управления анимациями
    private int currentMoveAnimation = 0;
    private int currentIdleAnimation = -1;
    private Coroutine animationTransitionCoroutine;

    // Новые параметры для улучшенного управления анимациями
    private float animationTransitionTime = 0.3f;
    private bool isTransitioning = false;

    void Start()
    {
        anky = GetComponent<Anky>();
        anim = anky.anm;
        body = anky.body;

        if (body == null)
        {
            Debug.LogError("Rigidbody not found!");
            return;
        }

        anky.useAI = false;
        body.WakeUp();

        // Инициализируем анимации в спокойном состоянии
        ResetToCalmState();

        InitializeWaypointSystem();
    }

    void ResetToCalmState()
    {
        // Плавный сброс всех анимаций к спокойному состоянию
        if (animationTransitionCoroutine != null)
            StopCoroutine(animationTransitionCoroutine);

        animationTransitionCoroutine = StartCoroutine(SmoothAnimationReset());
    }

    IEnumerator SmoothAnimationReset()
    {
        isTransitioning = true;

        // Сначала сбрасываем движение
        anim.SetInteger("Move", 0);
        currentMoveAnimation = 0;

        // Ждем завершения перехода
        yield return new WaitForSeconds(animationTransitionTime);

        // Затем сбрасываем idle
        anim.SetInteger("Idle", -1);
        currentIdleAnimation = -1;

        // Убеждаемся что нет других параметров
        anim.SetBool("Attack", false);
        anim.SetFloat("Speed", 0f);
        anim.SetFloat("Turn", 0f);

        // Принудительно обновляем аниматор
        anim.Update(0.1f);

        isTransitioning = false;
        animationTransitionCoroutine = null;
    }

    void InitializeWaypointSystem()
    {
        sortedWaypoints.Clear();

        // Фильтруем только валидные точки
        var validWaypoints = waypoints.Where(w => w.point != null).ToList();

        if (validWaypoints.Count == 0)
        {
            Debug.LogWarning("No valid waypoints assigned!");
            return;
        }

        // Сортируем точки по приоритету (от меньшего к большему)
        var groupedByPriority = validWaypoints
            .GroupBy(w => w.priority)
            .OrderBy(g => g.Key)  // Сортируем группы по приоритету
            .ToList();

        // Для каждой группы приоритета перемешиваем точки случайным образом
        foreach (var group in groupedByPriority)
        {
            var shuffledGroup = group.OrderBy(x => Random.value).ToList();
            sortedWaypoints.AddRange(shuffledGroup);
        }

        DebugLog($"Waypoint system started with {sortedWaypoints.Count} waypoints");
        DebugLog($"Priorities: {string.Join(", ", sortedWaypoints.Select(w => w.priority.ToString()))}");

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
        // Даем время на инициализацию
        yield return new WaitForSeconds(2f);

        int currentIndex = -1;

        while (sortedWaypoints.Count > 0)
        {
            // Выбираем следующую точку в отсортированном списке
            currentIndex = GetNextWaypointIndex(currentIndex);
            currentWaypointIndex = currentIndex;

            if (currentWaypointIndex == -1) break;

            AnkyWaypoint targetWaypoint = sortedWaypoints[currentWaypointIndex];
            currentTargetPosition = targetWaypoint.point.position;

            DebugLog($"Selected waypoint: {targetWaypoint.state} (Priority: {targetWaypoint.priority}) at {currentTargetPosition}");

            yield return StartCoroutine(MoveToWaypoint(targetWaypoint));

            if (currentWaypointIndex != -1)
            {
                yield return StartCoroutine(PerformWaypointAction());
            }

            if (sortedWaypoints.Count > 1)
            {
                float delay = Random.Range(minDelayBetweenPoints, maxDelayBetweenPoints);
                yield return new WaitForSeconds(delay);
            }
        }

        DebugLog("No more waypoints available!");
    }

    int GetNextWaypointIndex(int currentIndex)
    {
        if (sortedWaypoints.Count == 0) return -1;

        // Если это первая точка или мы достигли конца списка, начинаем с начала
        if (currentIndex == -1 || currentIndex >= sortedWaypoints.Count - 1)
        {
            return 0; // Возвращаемся к первой точке
        }

        // Иначе переходим к следующей точке
        return currentIndex + 1;
    }

    IEnumerator MoveToWaypoint(AnkyWaypoint waypoint)
    {
        // ПЛАВНЫЙ ПОВОРОТ К ЦЕЛИ ПЕРЕД НАЧАЛОМ ДВИЖЕНИЯ
        yield return StartCoroutine(SmoothTurnToTarget());

        // ПЛАВНЫЙ ПЕРЕХОД К ДВИЖЕНИЮ
        yield return StartCoroutine(TransitionToMovement(waypoint.movementType));

        // Скорость движения
        float targetSpeed = (waypoint.movementType == MovementType.Walk) ? walkSpeed : runSpeed;

        DebugLog($"Starting movement. Type: {waypoint.movementType}, Speed: {targetSpeed}");

        // Запускаем физическое движение
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(MovementRoutine(targetSpeed));

        // Ждем достижения цели
        float moveTimer = 0f;
        bool reachedTarget = false;

        while (isMoving && moveTimer < maxMoveTime && !reachedTarget)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTargetPosition);

            if (distanceToTarget <= arrivalDistance)
            {
                DebugLog($"Reached waypoint! Distance: {distanceToTarget:F1}");
                reachedTarget = true;
                break;
            }

            moveTimer += Time.deltaTime;
            yield return null;
        }

        // ПЛАВНЫЙ ПЕРЕХОД К ПОКОЮ
        yield return StartCoroutine(TransitionToIdle());

        if (moveTimer >= maxMoveTime)
        {
            DebugLogWarning($"Movement timeout after {moveTimer:F1}s");
        }

        yield return new WaitForSeconds(0.3f);
    }

    // Плавный поворот к цели перед началом движения
    IEnumerator SmoothTurnToTarget()
    {
        Vector3 directionToTarget = (currentTargetPosition - transform.position).normalized;
        directionToTarget.y = 0;

        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

            // Если угол поворота значительный, делаем плавный поворот
            if (angleDifference > 10f)
            {
                DebugLog($"Плавный поворот на {angleDifference:F1} градусов");

                // Включаем анимацию ходьбы для плавного поворота
                anim.SetInteger("Move", 1);
                currentMoveAnimation = 1;

                float turnProgress = 0f;
                Quaternion startRotation = transform.rotation;

                while (turnProgress < 1f)
                {
                    turnProgress += Time.deltaTime * turnSpeed * 0.5f; // Медленнее для плавности
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnProgress);

                    // Анимация поворота на месте
                    anim.SetFloat("Turn", Mathf.Clamp(angleDifference / 180f, -1f, 1f));

                    yield return null;
                }

                // Сбрасываем анимацию поворота
                anim.SetFloat("Turn", 0f);
                anim.SetInteger("Move", 0);
                currentMoveAnimation = 0;

                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    IEnumerator TransitionToMovement(MovementType movementType)
    {
        // Ждем завершения текущего перехода
        while (isTransitioning)
            yield return null;

        isTransitioning = true;
        isMoving = true;

        // Определяем целевую анимацию движения
        int targetMoveAnimation = (movementType == MovementType.Walk) ? 1 : 2;

        // Шаг 1: Сначала сбрасываем idle анимацию если она активна
        if (currentIdleAnimation != -1)
        {
            anim.SetInteger("Idle", -1);
            currentIdleAnimation = -1;

            // Ждем выхода из idle состояния
            yield return new WaitForSeconds(animationTransitionTime * 0.7f);
        }

        // Шаг 2: Устанавливаем движение с дополнительной проверкой
        if (currentMoveAnimation != targetMoveAnimation)
        {
            anim.SetInteger("Move", targetMoveAnimation);
            currentMoveAnimation = targetMoveAnimation;

            // Ждем пока анимация установится
            yield return new WaitForSeconds(animationTransitionTime);

            // Дополнительная проверка и принудительное обновление
            if (anim.GetInteger("Move") != targetMoveAnimation)
            {
                anim.SetInteger("Move", targetMoveAnimation);
                anim.Update(0.1f);
            }
        }

        isTransitioning = false;
    }

    IEnumerator TransitionToIdle()
    {
        // Ждем завершения текущего перехода
        while (isTransitioning)
            yield return null;

        isTransitioning = true;

        // Останавливаем физическое движение
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        body.linearVelocity = Vector3.zero;

        // Плавно сбрасываем анимацию движения
        if (currentMoveAnimation != 0)
        {
            anim.SetInteger("Move", 0);
            currentMoveAnimation = 0;

            // Ждем завершения перехода к idle
            yield return new WaitForSeconds(animationTransitionTime);

            // Принудительное обновление
            if (anim.GetInteger("Move") != 0)
            {
                anim.SetInteger("Move", 0);
                anim.Update(0.1f);
            }
        }

        isMoving = false;
        isTransitioning = false;
    }

    IEnumerator MovementRoutine(float targetSpeed)
    {
        while (isMoving)
        {
            Vector3 direction = (currentTargetPosition - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

                Vector3 targetVelocity = direction * targetSpeed;
                body.linearVelocity = Vector3.Lerp(body.linearVelocity, targetVelocity, Time.deltaTime * acceleration);
            }

            yield return null;
        }
    }

    IEnumerator PerformWaypointAction()
    {
        AnkyWaypoint waypoint = sortedWaypoints[currentWaypointIndex];
        isPerformingAction = true;

        DebugLog($"Performing action: {waypoint.state}");

        // ПЛАВНЫЙ ПЕРЕХОД К IDLE АНИМАЦИИ
        yield return StartCoroutine(TransitionToSpecificIdle(waypoint.state));

        // Ждем указанное время
        float stayTime = Random.Range(waypoint.minStayTime, waypoint.maxStayTime);
        DebugLog($"Will stay for {stayTime:F1}s");

        float timer = 0f;
        while (timer < stayTime && isPerformingAction)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Плавно сбрасываем idle анимацию
        if (!isMoving)
        {
            anim.SetInteger("Idle", -1);
            currentIdleAnimation = -1;
            yield return new WaitForSeconds(animationTransitionTime * 0.5f);
        }

        isPerformingAction = false;
    }

    IEnumerator TransitionToSpecificIdle(WaypointState state)
    {
        // Ждем завершения текущего перехода
        while (isTransitioning)
            yield return null;

        isTransitioning = true;

        // Определяем целевую анимацию
        int targetIdleAnimation = -1;
        switch (state)
        {
            case WaypointState.Stand: targetIdleAnimation = standAnimation; break;
            case WaypointState.Eat: targetIdleAnimation = eatAnimation; break;
            case WaypointState.Sleep: targetIdleAnimation = sleepAnimation; break;
        }

        // Убеждаемся что движение сброшено
        if (currentMoveAnimation != 0)
        {
            anim.SetInteger("Move", 0);
            currentMoveAnimation = 0;
            yield return new WaitForSeconds(animationTransitionTime * 0.7f);
        }

        // Устанавливаем idle анимацию
        anim.SetInteger("Idle", targetIdleAnimation);
        currentIdleAnimation = targetIdleAnimation;

        // Ждем установки анимации
        yield return new WaitForSeconds(animationTransitionTime);

        // Принудительное обновление
        anim.Update(0.1f);

        isTransitioning = false;
    }

    void Update()
    {
        // Визуальная отладка пути
        if (isMoving && currentWaypointIndex != -1)
        {
            Debug.DrawLine(transform.position, currentTargetPosition, Color.green);
        }
    }

    void DebugLog(string message)
    {
        if (showDebug)
        {
            Debug.Log($"[AnkyWaypoint] {message}");
        }
    }

    void DebugLogWarning(string message)
    {
        if (showDebug)
        {
            Debug.LogWarning($"[AnkyWaypoint] {message}");
        }
    }

    public void StartPatrol()
    {
        InitializeWaypointSystem();
    }

    public void StopPatrol()
    {
        if (behaviorCoroutine != null)
        {
            StopCoroutine(behaviorCoroutine);
            behaviorCoroutine = null;
        }

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        if (animationTransitionCoroutine != null)
        {
            StopCoroutine(animationTransitionCoroutine);
            animationTransitionCoroutine = null;
        }

        // Плавный сброс к спокойному состоянию
        StartCoroutine(SmoothAnimationReset());

        body.linearVelocity = Vector3.zero;
        isMoving = false;
        isPerformingAction = false;
    }
}
