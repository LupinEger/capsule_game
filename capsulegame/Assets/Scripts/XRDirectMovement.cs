using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class XRDirectMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float deadZone = 0.15f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference rotateAction;

    [Header("References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Camera xrCamera;

    private Rigidbody rb;
    private Vector2 currentMoveInput;
    private Vector2 currentRotateInput;
    private bool movementEnabled = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Или false, в зависимости от нужной физики
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        FindXRReferences();
        DisableDefaultXRMovement();
    }

    private void FindXRReferences()
    {
        if (xrOrigin == null)
        {
            xrOrigin = transform;
        }

        if (xrCamera == null)
        {
            xrCamera = GetComponentInChildren<Camera>();
        }
    }

    private void DisableDefaultXRMovement()
    {
        // Отключаем стандартные системы движения XR Toolkit
        var moveProvider = GetComponent<ContinuousMoveProvider>();
        if (moveProvider != null) moveProvider.enabled = false;

        var turnProvider = GetComponent<ContinuousTurnProvider>();
        if (turnProvider != null) turnProvider.enabled = false;

        var snapProvider = GetComponent<SnapTurnProvider>();
        if (snapProvider != null) snapProvider.enabled = false;
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        rotateAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        rotateAction?.action.Disable();
    }

    private void Update()
    {
        // Получаем ввод
        currentMoveInput = GetInputWithDeadZone(moveAction);
        currentRotateInput = GetInputWithDeadZone(rotateAction);

        // Обработка поворота
        HandleRotation();
    }

    private void FixedUpdate()
    {
        // Обработка движения в FixedUpdate для плавности
        HandleMovement();
    }


    private void HandleMovement()
    {
        if (currentMoveInput.magnitude > deadZone && xrCamera != null)
        {
            // Движение относительно направления камеры
            Vector3 cameraForward = xrCamera.transform.forward;
            Vector3 cameraRight = xrCamera.transform.right;

            // Игнорируем вертикальную составляющую
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Рассчитываем направление движения
            Vector3 moveDirection = (cameraForward * currentMoveInput.y +
                                   cameraRight * currentMoveInput.x).normalized;

            // Применяем движение
            if (rb != null && !rb.isKinematic)
            {
                // Физическое движение
                rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                // Прямое движение
                transform.position += moveDirection * moveSpeed * Time.fixedDeltaTime;
            }

            if (!IsBlockedByDinosaur(moveDirection))
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }

            bool IsBlockedByDinosaur(Vector3 direction)
            {
                float checkDistance = 0.5f;
                RaycastHit hit;

                if (Physics.Raycast(transform.position, direction, out hit, checkDistance))
                {
                    if (hit.collider.CompareTag("Creature"))
                    {
                        return true; // Не двигаться если впереди динозавр
                    }
                }
                return false;
            }
        }
    }

    private void HandleRotation()
    {
        if (Mathf.Abs(currentRotateInput.x) > deadZone)
        {
            // ПРАВИЛЬНЫЙ ПОВОРОТ: 
            // currentRotateInput.x > 0 (стик вправо) -> поворот вправо
            // currentRotateInput.x < 0 (стик влево) -> поворот влево
            float rotationAmount = currentRotateInput.x * rotationSpeed * Time.deltaTime;
            xrOrigin.Rotate(0f, rotationAmount, 0f);
        }
    }

    private Vector2 GetInputWithDeadZone(InputActionReference action)
    {
        Vector2 input = action?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        return input.magnitude < deadZone ? Vector2.zero : input;
    }

    // Методы для внешнего контроля
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled)
        {
            currentMoveInput = Vector2.zero;
            currentRotateInput = Vector2.zero;

            // Также обнуляем физику если нужно
            var rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void Teleport(Vector3 position)
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(position);
        }
        else
        {
            transform.position = position;
        }
    }
}
