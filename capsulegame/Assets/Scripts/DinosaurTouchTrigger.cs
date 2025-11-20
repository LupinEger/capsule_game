using UnityEngine;

public class DinosaurTouchTrigger : MonoBehaviour
{
    [Header("Tactile Settings")]
    [SerializeField] private string tactPatternId = "dinosaur_impact";
    [SerializeField] private float durationMillis = 500;
    [SerializeField] private float intensity = 1.0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // Определяем какая рука коснулась по тегу
            if (other.CompareTag("LeftHand"))
            {
                EventManager.InvokeOnDinosaurTouchedLeft(tactPatternId, intensity, durationMillis);
            }
            else if (other.CompareTag("RightHand"))
            {
                EventManager.InvokeOnDinosaurTouchedRight(tactPatternId, intensity, durationMillis);
            }
        }
    }
}
