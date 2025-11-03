using UnityEngine;

public class JumpFixAdvanced : MonoBehaviour
{
    private CharacterController characterController;
    private MonoBehaviour jumpComponent;
    private bool wasGrounded = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Ищем любой компонент с "Jump" в названии
        foreach (var component in GetComponents<MonoBehaviour>())
        {
            if (component.GetType().Name.ToLower().Contains("jump"))
            {
                jumpComponent = component;
                Debug.Log("Found jump component: " + component.GetType().Name);
                break;
            }
        }
    }

    void Update()
    {
        if (characterController == null) return;

        bool isGrounded = characterController.isGrounded;

        // Если только что приземлились после прыжка
        if (!wasGrounded && isGrounded)
        {
            Debug.Log("Successfully landed! Resetting jump ability...");
            ResetJumpAbility();
        }

        wasGrounded = isGrounded;

        // Принудительная помощь CharacterController
        if (isGrounded && characterController.velocity.y < -0.1f)
        {
            characterController.Move(Vector3.down * 0.01f);
        }
    }

    private void ResetJumpAbility()
    {
        // Метод 1: Перезапуск jump компонента если найден
        if (jumpComponent != null)
        {
            jumpComponent.enabled = false;
            jumpComponent.enabled = true;
            Debug.Log("Jump component reset");
        }

        // Метод 2: Перезапуск всех возможных компонентов движения
        var allComponents = GetComponents<MonoBehaviour>();
        foreach (var component in allComponents)
        {
            var typeName = component.GetType().Name.ToLower();
            if (typeName.Contains("move") || typeName.Contains("locomotion"))
            {
                component.enabled = false;
                component.enabled = true;
            }
        }

        // Метод 3: Сброс через небольшое движение
        if (characterController != null)
        {
            characterController.Move(Vector3.down * 0.02f);
        }
    }
}