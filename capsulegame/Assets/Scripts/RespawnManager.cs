using System.Collections;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance;

    [Header("Respawn Settings")]
    public Transform defaultSpawnPoint;
    public float respawnDelay = 2f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private void Awake()
    {
        Instance = this;

        // Сохраняем позицию спавна
        if (defaultSpawnPoint != null)
        {
            spawnPosition = defaultSpawnPoint.position;
            spawnRotation = defaultSpawnPoint.rotation;
        }
        else
        {
            // Если точка не назначена, используем текущую позицию игрока
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                spawnPosition = player.transform.position;
                spawnRotation = player.transform.rotation;
            }
        }
    }

    public void SetSpawnPoint(Transform newSpawnPoint)
    {
        spawnPosition = newSpawnPoint.position;
        spawnRotation = newSpawnPoint.rotation;
        Debug.Log($"[RespawnManager] Новая точка спавна установлена: {spawnPosition}");
    }

    public void PlayerDied()
    {
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        Debug.Log("[RespawnManager] Начинаем респавн...");

        // Ждем немного перед респавном
        yield return new WaitForSeconds(respawnDelay);

        // Респавним игрока
        RespawnPlayer();
    }

    public void RespawnPlayer()
    {
        // Находим PlayerHealth и вызываем его метод Respawn()
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            // Телепортируем игрока к точке спавна
            playerHealth.transform.position = spawnPosition;
            playerHealth.transform.rotation = spawnRotation;

            // Вызываем твой существующий метод респавна
            playerHealth.Respawn();

            Debug.Log($"[RespawnManager] Игрок респавнут в позиции: {spawnPosition}");
        }
        else
        {
            Debug.LogError("[RespawnManager] PlayerHealth не найден!");
        }
    }

    // Для вызова из других скриптов
    public static void Respawn()
    {
        Instance?.PlayerDied();
    }
}
