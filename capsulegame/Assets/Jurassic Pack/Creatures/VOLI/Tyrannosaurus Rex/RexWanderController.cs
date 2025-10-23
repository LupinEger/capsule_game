using UnityEngine;
using System.Collections;

public class RexWanderController : MonoBehaviour
{
    private Rex rex;
    private Animator anim;
    private Rigidbody body;
    private Transform player;

    [Header("Wander Settings")]
    public float wanderRadius = 20f;
    public float minWanderDistance = 8f;
    public float wanderInterval = 5f;
    public float turnSpeed = 2f;

    [Header("Movement Settings")]
    public float walkForce = 300f;
    public float runForce = 800f;
    public float maxWalkSpeed = 8f;
    public float maxRunSpeed = 15f;

    [Header("Chase Settings")]
    public float detectionRange = 15f;
    public float attackRange = 4f;
    public LayerMask obstacleLayers = -1;

    [Header("Debug")]
    public bool showDebug = true;
    public bool drawGizmos = true;

    private Vector3 currentWanderTarget;
    private bool isWandering = false;
    private bool isChasing = false;
    private bool isAttacking = false;
    private Coroutine behaviorCoroutine;
    private float lastWanderTime;

    private int currentMoveType = 0;
    private bool wantsToAttack = false;
    private Vector3 desiredDirection = Vector3.zero;
    private float desiredSpeed = 0f;

    void Start()
    {
        rex = GetComponent<Rex>();
        anim = rex.anm;
        body = rex.body;

        if (rex == null || body == null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        rex.useAI = false;
        body.linearDamping = 1f;
        body.angularDamping = 1f;

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
            if (!isAttacking)
            {
                bool canSeePlayer = CheckPlayerVisibility();

                if (canSeePlayer)
                {
                    if (!isChasing) StartChase();
                    yield return StartCoroutine(ChaseRoutine());
                }
                else
                {
                    if (isChasing) StopChase();
                    if (!isWandering) yield return StartCoroutine(WanderRoutine());
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    bool CheckPlayerVisibility()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up * 2f, directionToPlayer, out hit, detectionRange, obstacleLayers))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return false;
    }

    void StartChase()
    {
        isChasing = true;
        isWandering = false;
    }

    void StopChase()
    {
        isChasing = false;
        ResetMovement();
    }

    IEnumerator ChaseRoutine()
    {
        float chaseTimer = 0f;
        float maxChaseTime = 30f;

        while (isChasing && chaseTimer < maxChaseTime)
        {
            if (player == null) break;

            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }

            if (distanceToPlayer <= attackRange)
            {
                yield return StartCoroutine(AttackRoutine());
            }
            else
            {
                SetMovement(2, transform.forward, runForce, maxRunSpeed);
            }

            if (!CheckPlayerVisibility()) break;

            chaseTimer += Time.deltaTime;
            yield return null;
        }

        isChasing = false;
        ResetMovement();
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        ResetMovement();
        wantsToAttack = true;

        yield return new WaitForSeconds(0.3f);
        ApplyDamageToPlayer();
        yield return new WaitForSeconds(1.2f);

        wantsToAttack = false;
        isAttacking = false;
        yield return new WaitForSeconds(0.5f);
    }

    void ApplyDamageToPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange * 1.5f)
        {
            DebugLog("Player takes damage!");
        }
    }

    IEnumerator WanderRoutine()
    {
        isWandering = true;

        while (!isChasing && isWandering && !isAttacking)
        {
            if (Time.time - lastWanderTime < wanderInterval)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            currentWanderTarget = GetRandomWanderPoint();
            yield return StartCoroutine(MoveToWanderTarget());
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            lastWanderTime = Time.time;
        }

        isWandering = false;
    }

    Vector3 GetRandomWanderPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minWanderDistance, wanderRadius);
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (Vector3.Distance(transform.position, randomPoint) >= minWanderDistance)
                return randomPoint;
        }

        return transform.position + transform.forward * minWanderDistance;
    }

    IEnumerator MoveToWanderTarget()
    {
        float moveTimer = 0f;
        float maxMoveTime = 20f;

        while (isWandering && moveTimer < maxMoveTime)
        {
            Vector3 direction = (currentWanderTarget - transform.position).normalized;
            direction.y = 0;

            float distanceToTarget = Vector3.Distance(transform.position, currentWanderTarget);

            if (distanceToTarget <= 2f) break;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                SetMovement(1, transform.forward, walkForce, maxWalkSpeed);
            }

            if (moveTimer > 3f && body.linearVelocity.magnitude < 0.5f) break;

            moveTimer += Time.deltaTime;
            yield return null;
        }

        ResetMovement();
    }

    void SetMovement(int moveType, Vector3 direction, float force, float maxSpeed)
    {
        currentMoveType = moveType;
        desiredDirection = direction;
        desiredSpeed = force;
    }

    void ResetMovement()
    {
        currentMoveType = 0;
        desiredDirection = Vector3.zero;
        desiredSpeed = 0f;
        wantsToAttack = false;
    }

    void UpdateAnimations()
    {
        if (anim == null) return;
        anim.SetInteger("Move", currentMoveType);
        anim.SetBool("Attack", wantsToAttack);
    }

    void ApplyMovement()
    {
        if (body == null || desiredDirection == Vector3.zero || desiredSpeed <= 0) return;

        float currentMaxSpeed = (currentMoveType == 2) ? maxRunSpeed : maxWalkSpeed;

        if (body.linearVelocity.magnitude < currentMaxSpeed)
        {
            Vector3 force = desiredDirection * desiredSpeed * Time.fixedDeltaTime;
            body.AddForce(force, ForceMode.Force);
        }
        else
        {
            body.linearVelocity = body.linearVelocity.normalized * currentMaxSpeed;
        }
    }

    void FixedUpdate() => ApplyMovement();
    void Update() => UpdateAnimations();

    void DebugLog(string message)
    {
        if (showDebug) Debug.Log($"[RexWander] {message}");
    }

    public void StopBehavior()
    {
        if (behaviorCoroutine != null) StopCoroutine(behaviorCoroutine);
        isWandering = isChasing = isAttacking = false;
        ResetMovement();
        if (body != null) body.linearVelocity = Vector3.zero;
    }

    void OnDestroy() => StopBehavior();
    void OnDisable() => StopBehavior();
}
