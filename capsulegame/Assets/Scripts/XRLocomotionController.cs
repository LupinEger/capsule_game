using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(CharacterController))]
public class XRLocomotionController : MonoBehaviour
{
    [Header("XR Components")]
    public XROrigin xrOrigin;

    [Header("Movement Settings")]
    public float movementSpeed = 2f;
    public float rotationSpeed = 100f;
    public float jumpForce = 5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.1f;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform cameraTransform;
    private InputAction leftStick, rightStick, jumpButton;

    void Start()
    {
        InitializeComponents();
        SetupInput();

        // Автоматически находим и логируем контроллеры
        FindAndLogControllers();
    }

    void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();

        if (xrOrigin == null)
            xrOrigin = GetComponent<XROrigin>();

        cameraTransform = xrOrigin.Camera.transform;
    }

    void FindAndLogControllers()
    {
        // Находим все ActionBasedController в дочерних объектах
        var controllers = GetComponentsInChildren<ActionBasedController>();

        Debug.Log($"Found {controllers.Length} controllers:");

        foreach (var controller in controllers)
        {
            Debug.Log($"- {controller.name} (Enabled: {controller.enabled})");

            // Автоматически включаем если нужно
            controller.enableInputTracking = true;
            controller.enableInputActions = true;
        }

        if (controllers.Length == 0)
        {
            Debug.LogWarning("No ActionBasedController components found! Check your XR Origin setup.");
        }
    }

    void SetupInput()
    {
        // Создаем Input Actions без привязки к конкретным контроллерам в инспекторе
        leftStick = new InputAction("LeftStick", InputActionType.Value, "<XRController>{LeftHand}/thumbstick");
        rightStick = new InputAction("RightStick", InputActionType.Value, "<XRController>{RightHand}/thumbstick");
        jumpButton = new InputAction("Jump", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");

        leftStick.Enable();
        rightStick.Enable();
        jumpButton.Enable();
    }

    void Update()
    {
        CheckGrounded();
        HandleMovement();
        HandleRotation();
        HandleJump();
        ApplyGravity();
    }

    void CheckGrounded()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f, groundLayer);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    void HandleMovement()
    {
        Vector2 stickInput = leftStick.ReadValue<Vector2>();

        if (stickInput.magnitude > 0.1f)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * stickInput.y + cameraRight * stickInput.x).normalized;
            characterController.Move(moveDirection * movementSpeed * Time.deltaTime);
        }
    }

    void HandleRotation()
    {
        Vector2 stickInput = rightStick.ReadValue<Vector2>();

        if (Mathf.Abs(stickInput.x) > 0.1f)
        {
            float rotationAmount = stickInput.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }
    }

    void HandleJump()
    {
        if (jumpButton.ReadValue<float>() > 0.1f && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += Physics.gravity.y * Time.deltaTime;
        }
        characterController.Move(velocity * Time.deltaTime);
    }

    void OnDestroy()
    {
        leftStick?.Disable();
        rightStick?.Disable();
        jumpButton?.Disable();
    }
}