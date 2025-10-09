using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class AnkyWanderNavMesh : MonoBehaviour
{
    private Anky anky;
    private Animator anim;
    private NavMeshAgent agent;

    [Header("Wander Settings")]
    public float wanderRadius = 50f;
    public float minWanderDistance = 15f;
    public float minWanderTime = 8f;
    public float maxWanderTime = 25f;
    public float minIdleTime = 4f;
    public float maxIdleTime = 10f;

    [Header("Animation Settings")]
    public float turnSpeed = 60f;

    [Header("Behavior Settings")]
    [Range(0f, 1f)] public float eatingChance = 0.15f;
    [Range(0f, 1f)] public float rareIdleChance = 0.08f;

    // Система состояний
    private enum DinoState { Moving, Idle, Eating }
    private DinoState currentState = DinoState.Idle;

    private Vector3 startPosition;
    private int lastIdleType = -1;
    private float stateTimer = 0f;
    private bool canChangeState = true;

    void Start()
    {
        anky = GetComponent<Anky>();
        anim = anky.anm;
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        // Настройка агента
        agent.speed = 2f;
        agent.angularSpeed = turnSpeed;
        agent.acceleration = 3f;
        agent.stoppingDistance = 1f;
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.autoBraking = true;

        startPosition = transform.position;

        // Начинаем с idle состояния
        SetState(DinoState.Idle);
    }

    void Update()
    {
        if (!canChangeState) return;

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            switch (currentState)
            {
                case DinoState.Moving:
                    // После движения выбираем следующее состояние
                    ChooseNextStateAfterMovement();
                    break;

                case DinoState.Idle:
                    // После idle можем пойти двигаться
                    SetState(DinoState.Moving);
                    break;

                case DinoState.Eating:
                    // После еды обязательно idle перед движением
                    SetState(DinoState.Idle);
                    break;
            }
        }

        // Обновление анимации в зависимости от состояния
        UpdateAnimation();
    }

    void SetState(DinoState newState)
    {
        if (!canChangeState) return;

        currentState = newState;

        switch (newState)
        {
            case DinoState.Moving:
                StartCoroutine(MovementRoutine());
                break;

            case DinoState.Idle:
                stateTimer = Random.Range(minIdleTime, maxIdleTime);
                SetIdleAnimation();
                break;

            case DinoState.Eating:
                stateTimer = Random.Range(minIdleTime + 3f, maxIdleTime + 5f);
                anim.SetInteger("Idle", 3); // Eating animation
                break;
        }
    }

    IEnumerator MovementRoutine()
    {
        canChangeState = false;

        // Сбрасываем ВСЕ анимации перед движением
        ResetAllAnimations();

        // Ждем 1 кадр чтобы анимации сбросились
        yield return null;

        Vector3 targetPoint = FindGoodMovementPoint();

        // Плавный поворот к цели
        yield return StartCoroutine(SmoothTurnToTarget(targetPoint));

        // Запускаем анимацию ходьбы ДО начала движения
        anim.SetInteger("Move", 1);

        // Ждем немного чтобы анимация началась
        yield return new WaitForSeconds(0.3f);

        // Теперь начинаем движение
        agent.SetDestination(targetPoint);

        // Ждем пока не начнем двигаться
        yield return new WaitUntil(() => agent.velocity.magnitude > 0.1f);

        float moveTimer = 0f;
        float maxMoveTime = Random.Range(minWanderTime, maxWanderTime);

        // Двигаемся в течение времени
        while (moveTimer < maxMoveTime && agent.remainingDistance > agent.stoppingDistance)
        {
            moveTimer += Time.deltaTime;
            yield return null;
        }

        // Останавливаем движение
        agent.ResetPath();

        // Останавливаем анимацию ходьбы
        anim.SetInteger("Move", 0);

        // Короткая пауза после движения
        yield return new WaitForSeconds(1f);

        canChangeState = true;
        stateTimer = 0f; // Сразу переходим к выбору состояния
    }

    void ChooseNextStateAfterMovement()
    {
        float randomValue = Random.Range(0f, 1f);

        if (randomValue < eatingChance)
        {
            SetState(DinoState.Eating);
        }
        else
        {
            // После движения обычно отдыхаем
            SetState(DinoState.Idle);
        }
    }

    void SetIdleAnimation()
    {
        // Всегда сбрасываем предыдущие анимации
        ResetAllAnimations();

        // Выбираем тип анимации
        int idleType = GetRandomIdleType();
        anim.SetInteger("Idle", idleType);
        lastIdleType = idleType;
    }

    void ResetAllAnimations()
    {
        // Сбрасываем ВСЕ параметры аниматора
        anim.SetInteger("Move", 0);
        anim.SetInteger("Idle", -1);
        anim.SetBool("Attack", false);
        // Добавьте другие параметры если есть
    }

    int GetRandomIdleType()
    {
        // Базовые анимации (очень часто)
        if (Random.Range(0f, 1f) > rareIdleChance)
        {
            return 0; // Самая простая анимация
        }
        // Редкие анимации (очень редко)
        else
        {
            int rareIdle = Random.Range(1, 3); // 1 или 2
            if (rareIdle == lastIdleType)
            {
                // Если та же анимация, берем другую
                rareIdle = rareIdle == 1 ? 2 : 1;
            }
            return rareIdle;
        }
    }

    IEnumerator SmoothTurnToTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float angle = Quaternion.Angle(transform.rotation, targetRotation);
            float turnTime = Mathf.Clamp(angle / turnSpeed, 0.5f, 2f);

            float timer = 0f;
            Quaternion startRotation = transform.rotation;

            while (timer < turnTime)
            {
                timer += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timer / turnTime);
                yield return null;
            }
        }
    }

    void UpdateAnimation()
    {
        // Дополнительная синхронизация анимации с движением
        if (currentState == DinoState.Moving)
        {
            if (agent.velocity.magnitude < 0.1f && anim.GetInteger("Move") == 1)
            {
                // Если стоим, но анимация ходьбы идет
                anim.SetInteger("Move", 0);
            }
        }
    }

    Vector3 FindGoodMovementPoint()
    {
        Vector3 bestPoint = startPosition;
        float bestDistance = 0f;

        for (int i = 0; i < 8; i++)
        {
            Vector3 randomPoint = GetRandomNavMeshPoint();
            float distance = Vector3.Distance(transform.position, randomPoint);

            if (distance >= minWanderDistance && distance > bestDistance)
            {
                bestPoint = randomPoint;
                bestDistance = distance;
            }
        }

        return bestDistance >= minWanderDistance ? bestPoint : GetRandomNavMeshPoint();
    }

    Vector3 GetRandomNavMeshPoint()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomPoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, wanderRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return startPosition;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, wanderRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minWanderDistance);

        // Визуализация текущего состояния
        if (Application.isPlaying)
        {
            string stateText = currentState.ToString();
            Vector3 textPos = transform.position + Vector3.up * 3f;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(textPos, $"State: {stateText}\nTimer: {stateTimer:F1}");
#endif
        }
    }
}