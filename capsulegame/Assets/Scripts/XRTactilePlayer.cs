using UnityEngine;
using Bhaptics.SDK2;

public class XRTactilePlayer : MonoBehaviour
{
    [Header("Bhaptics Gloves")]
    [SerializeField] private bool isGlovesEnabled = true;
    [SerializeField] private string handEventId; // Добавляем поле для ID руки

    void OnEnable()
    {
        // Подписываемся на событие в зависимости от тега объекта
        if (gameObject.CompareTag("LeftHand"))
        {
            EventManager.OnDinosaurTouchedLeft += PlayTactileFeedback;
        }
        else if (gameObject.CompareTag("RightHand"))
        {
            EventManager.OnDinosaurTouchedRight += PlayTactileFeedback;
        }
    }

    void OnDisable()
    {
        // Отписываемся от события
        if (gameObject.CompareTag("LeftHand"))
        {
            EventManager.OnDinosaurTouchedLeft -= PlayTactileFeedback;
        }
        else if (gameObject.CompareTag("RightHand"))
        {
            EventManager.OnDinosaurTouchedRight -= PlayTactileFeedback;
        }
    }

    // Основной метод для воспроизведения тактильной обратной связи
    public void PlayTactileFeedback(string patternId, float intensity, float durationMillis)
    {
        if (!isGlovesEnabled)
        {
            return;
        }

        // Ваш изначальный код - просто меняем eventId на handEventId
        BhapticsLibrary.Play(eventId: handEventId, duration: 1);
    }
}