using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class JumpDebug : MonoBehaviour
{
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (characterController != null)
        {
            Debug.Log($"Grounded: {characterController.isGrounded}");
            Debug.Log($"Velocity Y: {characterController.velocity.y}");
        }
    }
}