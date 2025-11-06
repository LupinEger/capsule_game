using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Класс для настройки каждой точки
[System.Serializable]
public class HerbivoreWaypoint
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

public class HerbivoreWaypointController : MonoBehaviour
{
    [Header("Dinosaur References")]
    public MonoBehaviour dinosaurController; // Ссылка на основной контроллер динозавра (Anky, Arge и т.д.)
    public Animator anim;
    public Rigidbody body;

    [Header("Waypoint System")]
    public List<HerbivoreWaypoint> waypoints = new List<HerbivoreWaypoint>();
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
    public bool useRandomEatAnimations = false; // Использовать случайные анимации еды
    public int maxEatAnimationVariants = 1; // Количество вариантов анимаций еды

    [Header("Dinosaur Size Settings")]
    public DinosaurSize size = DinosaurSize.Medium;

    [Header("Debug")]
    public bool showDebug = true;

    public enum DinosaurSize
    {
        Small,   // Маленький динозавр (быстрый)
        Medium,  // Средний динозавр
        Large    // Крупный динозавр (медленный)
    }

    private int currentWaypointIndex = -1;
    private bool isMoving = false;
    private bool isPerformingAction = false;
    private Coroutine behaviorCoroutine;
    private Coroutine movementCoroutine;
    private List<HerbivoreWaypoint> sortedWaypoints = new List<HerbivoreWaypoint>();
    private Vector3 currentTargetPosition;

    // Переменные для управления анимациями
    private int currentMoveAnimation = 0;
    private int currentIdleAnimation = -1;
    private Coroutine animationTransitionCoroutine;

    // Параметры для управления анимациями
    private float animationTransitionTime = 0.3f;
    private bool isTransitioning = false;

    void Start()
    {
        // Автоматическое определение компонентов если не установлены в инспекторе
        if (anim == null) anim = GetComponent<Animator>();
        if (body == null) body = GetComponent<Rigidbody>();
        if (dinosaurController == null)
            dinosaurController = GetComponent<MonoBehaviour>();

        if (body == null)
        {
            Debug.LogError("Rigidbody not found!");
            return;
        }

        // Применяем настройки размера
        ApplySizeSettings();

        // Отключаем AI основного контроллера если это возможно
        SetAIEnabled(false);

        body.WakeUp();

        // Инициализируем анимации в спокойном состоянии
        ResetToCalmState();

        InitializeWaypointSystem();
    }

    void ApplySizeSettings()
    {
        switch (size)
        {
            case DinosaurSize.Small:
                // Быстрые и резкие движения
                arrivalDistance = Mathf.Min(arrivalDistance, 2f);
                turnSpeed = Mathf.Max(turnSpeed, 3f);
                walkSpeed = Mathf.Max(walkSpeed, 4f);
                runSpeed = Mathf.Max(runSpeed, 8f);
                acceleration = Mathf.Max(acceleration, 10f);
                animationTransitionTime = 0.2f;
                break;

            case DinosaurSize.Medium:
                // Стандартные настройки (оставляем как есть)
                break;

            case DinosaurSize.Large:
                // Медленные и плавные движения
                arrivalDistance = Mathf.Max(arrivalDistance, 5f);
                turnSpeed = Mathf.Min(turnSpeed, 1.5f);
                walkSpeed = Mathf.Min(walkSpeed, 2f);
                runSpeed = Mathf.Min(runSpeed, 4f);
                acceleration = Mathf.Min(acceleration, 6f);
                animationTransitionTime = 0.4f;
                maxMoveTime = 45f;
                break;
        }
    }

    void SetAIEnabled(bool enabled)
    {
        if (dinosaurController != null)
        {
            // Пытаемся отключить AI через рефлексию (универсальный способ)
            var useAIField = dinosaurController.GetType().GetField("useAI");
            if (useAIField != null && useAIField.FieldType == typeof(bool))
            {
                useAIField.SetValue(dinosaurController, enabled);
            }
        }
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

            HerbivoreWaypoint targetWaypoint = sortedWaypoints[currentWaypointIndex];
            currentTargetPosition = targetWaypoint.point.position;

            DebugLog($"Selected waypoint: {targetWaypoint.state} (Priority: {targetWaypoint.priority}) at {currentTargetPosition}");

            yield return StartCoroutine(MoveToWaypoint(targetWaypoint));

            if (currentWaypointIndex != -1)
            {
                yield return StartCoroutine(PerformWaypointAction(targetWaypoint));
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

    IEnumerator MoveToWaypoint(HerbivoreWaypoint waypoint)
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

                // Настраиваем скорость поворота в зависимости от размера
                float turnMultiplier = size == DinosaurSize.Large ? 0.3f :
                                     size == DinosaurSize.Small ? 0.7f : 0.5f;

                while (turnProgress < 1f)
                {
                    turnProgress += Time.deltaTime * turnSpeed * turnMultiplier;
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnProgress);

                    // Анимация поворота на месте (только для средних и маленьких)
                    if (size != DinosaurSize.Large)
                    {
                        anim.SetFloat("Turn", Mathf.Clamp(angleDifference / 180f, -1f, 1f));
                    }

                    yield return null;
                }

                // Сбрасываем анимацию поворота
                anim.SetFloat("Turn", 0f);
                anim.SetInteger("Move", 0);
                currentMoveAnimation = 0;

                yield return new WaitForSeconds(size == DinosaurSize.Large ? 0.3f : 0.2f);
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

    IEnumerator PerformWaypointAction(HerbivoreWaypoint waypoint)
    {
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
        int targetIdleAnimation = GetIdleAnimationForState(state);

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

    private int GetIdleAnimationForState(WaypointState state)
    {
        switch (state)
        {
            case WaypointState.Stand:
                return standAnimation;

            case WaypointState.Eat:
                if (useRandomEatAnimations && maxEatAnimationVariants > 1)
                {
                    // Случайный выбор между различными анимациями еды
                    return Random.Range(eatAnimation, eatAnimation + maxEatAnimationVariants);
                }
                return eatAnimation;

            case WaypointState.Sleep:
                return sleepAnimation;

            default:
                return standAnimation;
        }
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
            Debug.Log($"[HerbivoreWaypoint] {message}");
        }
    }

    void DebugLogWarning(string message)
    {
        if (showDebug)
        {
            Debug.LogWarning($"[HerbivoreWaypoint] {message}");
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

        // Включаем обратно AI основного контроллера
        SetAIEnabled(true);

        // Плавный сброс к спокойному состоянию
        StartCoroutine(SmoothAnimationReset());

        body.linearVelocity = Vector3.zero;
        isMoving = false;
        isPerformingAction = false;
    }

    // Метод для добавления точек в runtime
    public void AddWaypoint(HerbivoreWaypoint waypoint)
    {
        waypoints.Add(waypoint);
        InitializeWaypointSystem();
    }

    // Метод для удаления точек в runtime
    public void RemoveWaypoint(Transform pointTransform)
    {
        waypoints.RemoveAll(w => w.point == pointTransform);
        InitializeWaypointSystem();
    }

    // Метод для очистки всех точек
    public void ClearWaypoints()
    {
        waypoints.Clear();
        sortedWaypoints.Clear();
        StopPatrol();
    }
}