using UnityEngine;

public class DinosaurTouchTrigger : MonoBehaviour
{
    [Header("Tactile Settings")]
    [SerializeField] private string tactPatternId = "touch"; // ID вашего .tact файла
    [SerializeField] private float durationMillis = 500; // Длительность импульса в мс
    [SerializeField] private float intensity = 1.0f; // Интенсивность (0.0 до 1.0)

    // Этот метод вызывается автоматически, когда коллайдер входит в триггер
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что столкновение произошло с рукой игрока
        // Предполагается, что на ваших XR контроллерах есть коллайдер и они на слое "Player"
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // "Сообщаем" о событии. Можно использовать разные подходы:

            // 1. Прямой вызов (просто, но менее гибко)
            // BhapticsGlovePlayer.Instance.PlayGlove(tactPatternId, intensity, durationMillis);

            // 2. Через менеджер событий (рекомендуется для сложных проектов)
            EventManager.InvokeOnDinosaurTouched(tactPatternId, intensity, durationMillis);

            // 3. Отправка сообщения на игрока
            // other.GetComponentInParent<XRTactilePlayer>()?.PlayTactileFeedback(tactPatternId, intensity, durationMillis);
        }
    }
}
