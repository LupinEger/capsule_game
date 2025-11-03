using UnityEngine;
using UnityEngine.InputSystem;

public class ReliableJump : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpHeight = 5.0f; // Увеличил в 2.5 раза
    public float gravity = -25.0f;  // Усилил гравитацию для более резкого прыжка

    [Header("Input")]
    public InputActionProperty jumpAction;

    private CharacterController characterController;
    private Vector3 playerVelocity;
    private bool isGrounded;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (jumpAction.action != null)
        {
            jumpAction.action.Enable();
        }
    }

    void Update()
    {
        if (characterController == null) return;

        // Обновляем grounded статус
        isGrounded = characterController.isGrounded;

        // Сбрасываем velocity только когда на земле
        if (isGrounded)
        {
            // Если мы на земле и velocity все еще отрицательная - сбрасываем
            if (playerVelocity.y < 0)
            {
                playerVelocity.y = -1f; // Минимальная отрицательная скорость для прижатия к земле
            }
        }

        // Обработка прыжка
        if (jumpAction.action != null && jumpAction.action.triggered && isGrounded)
        {
            Jump();
        }

        // Применяем гравитацию (только если не на земле)
        if (!isGrounded)
        {
            playerVelocity.y += gravity * Time.deltaTime;
        }

        // Применяем движение
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    private void Jump()
    {
        // Формула прыжка: v = ?(h * -2 * g)
        playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        Debug.Log($"Jumped! Velocity: {playerVelocity.y}, Height: {jumpHeight}");
    }

    // Метод для отладки - можно вызвать из других скриптов
    public void SetJumpHeight(float newHeight)
    {
        jumpHeight = newHeight;
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