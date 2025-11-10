using System;

public static class EventManager
{
    // Определяем делегат и событие
    public static Action<string, float, float> OnDinosaurTouched;

    // Метод для вызова события
    public static void InvokeOnDinosaurTouched(string patternId, float intensity, float durationMillis)
    {
        OnDinosaurTouched?.Invoke(patternId, intensity, durationMillis);
    }
}