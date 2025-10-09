using UnityEngine;
using System.Collections;
using UnityEngine.AI; // Добавляем для использования NavMeshAgent

public class AnkyWander : MonoBehaviour
{
    private Anky anky;
    private Animator anim;
    private NavMeshAgent agent; // Компонент для навигации

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float minWanderTime = 5f;
    public float maxWanderTime = 15f;
    public float minIdleTime = 3f;
    public float maxIdleTime = 8f;
    public float eatingChance = 0.3f;

    private Vector3 startPosition;
    private bool isWandering = false;

    void Start()
    {
        anky = GetComponent<Anky>();
        anim = anky.anm;
        agent = GetComponent<NavMeshAgent>(); // Получаем компонент навигации

        // Если компонента нет - добавляем автоматически
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        // Настройки NavMeshAgent
        agent.speed = 2f; // Скорость движения
        agent.angularSpeed = 120f; // Скорость поворота
        agent.acceleration = 2f; // Ускорение
        agent.stoppingDistance = 1f; // Дистанция остановки
        agent.radius = 0.5f; // Радиус существа
        agent.height = 1f; // Высота существа

        startPosition = transform.position;
        StartCoroutine(WanderRoutine());
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            // Движение к новой точке
            yield return StartCoroutine(MoveToRandomPoint());

            // Случайный выбор действия после движения
            float actionChoice = Random.Range(0f, 1f);

            if (actionChoice < eatingChance)
            {
                // Начать есть
                yield return StartCoroutine(EatingBehavior());
            }
            else
            {
                // Обычный idle
                yield return StartCoroutine(IdleBehavior());
            }
        }
    }

    IEnumerator MoveToRandomPoint()
    {
        // Генерация случайной точки на NavMesh
        Vector3 randomPoint = GetRandomPointOnNavMesh();

        // Устанавливаем точку назначения
        agent.SetDestination(randomPoint);

        // Запускаем анимацию ходьбы
        anim.SetInteger("Move", 1);
        isWandering = true;

        // Ждем пока достигнет точки или истечет время
        float moveTime = Random.Range(minWanderTime, maxWanderTime);
        float timer = 0f;

        while (timer < moveTime && agent.remainingDistance > agent.stoppingDistance)
        {
            timer += Time.deltaTime;

            // Синхронизируем скорость анимации со скоростью движения
            float speedPercent = agent.velocity.magnitude / agent.speed;
            anim.SetFloat("Speed", speedPercent);

            yield return null;
        }

        // Останавливаемся
        anim.SetInteger("Move", 0);
        agent.isStopped = true;
    }

    Vector3 GetRandomPointOnNavMesh()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += startPosition;
        randomDirection.y = 0; // Обнуляем Y для 2D плоскости

        NavMeshHit hit;
        Vector3 finalPosition = startPosition;

        // Ищем валидную точку на NavMesh
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            finalPosition = hit.position;
        }

        return finalPosition;
    }

    IEnumerator IdleBehavior()
    {
        int idleType = Random.Range(0, 3);
        anim.SetInteger("Idle", idleType);

        float idleTime = Random.Range(minIdleTime, maxIdleTime);
        yield return new WaitForSeconds(idleTime);

        anim.SetInteger("Idle", -1);
    }

    IEnumerator EatingBehavior()
    {
        anim.SetInteger("Idle", 3);

        float eatTime = Random.Range(minIdleTime + 2f, maxIdleTime + 3f);
        yield return new WaitForSeconds(eatTime);

        anim.SetInteger("Idle", -1);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, wanderRadius);

        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.DrawSphere(agent.destination, 0.5f);

            // Рисуем путь
            Gizmos.color = Color.blue;
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
            }
        }
    }

    public void StopWandering()
    {
        StopAllCoroutines();
        anim.SetInteger("Move", 0);
        anim.SetInteger("Idle", -1);
        if (agent != null) agent.isStopped = true;
        isWandering = false;
    }

    public void ResumeWandering()
    {
        if (!isWandering)
        {
            isWandering = true;
            StartCoroutine(WanderRoutine());
        }
    }
}
