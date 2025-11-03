using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleVRJump : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpImpulse = 8.0f;
    public InputActionProperty jumpAction;

    private CharacterController characterController;
    private bool isJumping;
    private float jumpTimer;
    private bool isGrounded;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (jumpAction.action != null)
            jumpAction.action.Enable();
    }

    void Update()
    {
        if (characterController == null) return;

        isGrounded = characterController.isGrounded;

        // Сбрасываем прыжок при приземлении
        if (isGrounded && isJumping)
        {
            isJumping = false;
            jumpTimer = 0f;
        }

        // Обработка прыжка
        if (jumpAction.action != null && jumpAction.action.triggered && isGrounded && !isJumping)
        {
            isJumping = true;
            jumpTimer = 0.3f; // Длительность прыжка
            Debug.Log("Jump started!");
        }

        // Применяем прыжок
        if (isJumping && jumpTimer > 0f)
        {
            float force = jumpImpulse * (jumpTimer / 0.3f); // Плавное затухание
            characterController.Move(Vector3.up * force * Time.deltaTime);
            jumpTimer -= Time.deltaTime;

            if (jumpTimer <= 0f)
            {
                Debug.Log("Jump finished");
            }
        }
    }

    void OnEnable()
    {
        if (jumpAction.action != null)
            jumpAction.action.Enable();
    }

    void OnDisable()
    {
        if (jumpAction.action != null)
            jumpAction.action.Disable();
    }
}