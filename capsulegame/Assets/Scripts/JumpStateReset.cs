using UnityEngine;
using System.Reflection;

public class JumpStateReset : MonoBehaviour
{
    private Component jumpComponent;
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Ищем компонент прыжка
        foreach (var component in GetComponents<Component>())
        {
            if (component.GetType().Name.ToLower().Contains("jump"))
            {
                jumpComponent = component;
                Debug.Log($"Found jump component: {component.GetType().Name}");
                break;
            }
        }
    }

    void Update()
    {
        if (characterController == null || jumpComponent == null) return;

        // Когда приземляемся - сбрасываем состояние прыжка
        if (characterController.isGrounded)
        {
            ResetJumpState();
        }
    }

    private void ResetJumpState()
    {
        try
        {
            var type = jumpComponent.GetType();

            // Пробуем разные возможные поля для сброса
            string[] possibleFields = { "m_CanJump", "canJump", "jumpAvailable", "isJumping" };

            foreach (string fieldName in possibleFields)
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(jumpComponent, true);
                    Debug.Log($"Reset field: {fieldName}");
                }
            }

            // Пробуем методы сброса
            var resetMethod = type.GetMethod("ResetJump", BindingFlags.NonPublic | BindingFlags.Instance);
            if (resetMethod != null)
            {
                resetMethod.Invoke(jumpComponent, null);
                Debug.Log("ResetJump method called");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Reset failed: {e.Message}");
        }
    }
}