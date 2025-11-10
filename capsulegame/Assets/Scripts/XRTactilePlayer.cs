using UnityEngine;
using Bhaptics.SDK2; // Не забудьте добавить пространство имен bhaptics

namespace Bhaptics.SDK2
{
    using System;
    using UnityEngine;

    public class BhapticsEvents
    {
        public const string touch = "touch";
    }
}
public class XRTactilePlayer : MonoBehaviour
{
    [Header("Bhaptics Gloves")]
    [SerializeField] private bool isGlovesEnabled = true;

    void OnEnable()
    {
        // Подписываемся на событие
        EventManager.OnDinosaurTouched += PlayTactileFeedback;
    }

    void OnDisable()
    {
        // Отписываемся от события
        EventManager.OnDinosaurTouched -= PlayTactileFeedback;
    }

    // Основной метод для воспроизведения тактильной обратной связи
    public void PlayTactileFeedback(string patternId, float intensity, float durationMillis)
    {
        if (!isGlovesEnabled)
        {
            return;
        }

        BhapticsLibrary.Play(eventId: BhapticsEvents.touch, duration: 1);
        BhapticsLibrary.Play(eventId: BhapticsEvents.touch, duration: 1);
    }
}
