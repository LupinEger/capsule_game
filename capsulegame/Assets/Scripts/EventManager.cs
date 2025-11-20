using System;
using UnityEngine;

public static class EventManager
{
    // События для левой и правой руки
    public static Action<string, float, float> OnDinosaurTouchedLeft;
    public static Action<string, float, float> OnDinosaurTouchedRight;

    // Методы для вызова событий
    public static void InvokeOnDinosaurTouchedLeft(string patternId, float intensity, float durationMillis)
    {
        OnDinosaurTouchedLeft?.Invoke(patternId, intensity, durationMillis);
    }

    public static void InvokeOnDinosaurTouchedRight(string patternId, float intensity, float durationMillis)
    {
        OnDinosaurTouchedRight?.Invoke(patternId, intensity, durationMillis);
    }
}