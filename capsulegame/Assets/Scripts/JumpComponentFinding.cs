using UnityEngine;

public class JumpComponentFinder : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== FINDING JUMP COMPONENTS ===");

        foreach (var component in GetComponents<Component>())
        {
            Debug.Log($"Component: {component.GetType().Name}");

            // Ищем поля связанные с прыжком
            var fields = component.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.Name.ToLower().Contains("jump"))
                {
                    Debug.Log($"  Field: {field.Name} = {field.GetValue(component)}");
                }
            }
        }
    }
}