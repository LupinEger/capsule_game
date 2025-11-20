using Futurift;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private FuturiftController playerController;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death Settings")]
    public GameObject deathUI;

    [Header("Respawn Settings")]
    public Transform respawnPoint; // Перетащи сюда точку респавна

    private bool isDead = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        currentHealth = maxHealth;

        // Сохраняем начальную позицию и вращение
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Если назначена точка респавна, используем её
        if (respawnPoint != null)
        {
            initialPosition = respawnPoint.position;
            initialRotation = respawnPoint.rotation;
        }

        if (deathUI != null)
            deathUI.SetActive(false);
        else
            Debug.LogWarning("Death UI reference not set!");

        Debug.Log("Player Health initialized: " + currentHealth + "/" + maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log("Player takes " + damage + " damage! Health: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        currentHealth = 0;
        Debug.Log("PLAYER DIED!");

        // Останавливаем время
        Time.timeScale = 0f;

        // Показываем UI смерти
        if (deathUI != null)
            deathUI.SetActive(true);

        if (playerController != null)
        {
            playerController.OnPlayerDeath();
        }
    }

    // Метод для возрождения из UI (вызывается кнопкой)
    public void Respawn()
    {
        Debug.Log("RESPAWNING PLAYER...");

        // Телепортируем игрока на точку респавна
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        Debug.Log("Player teleported to: " + initialPosition);

        // Восстанавливаем время
        Time.timeScale = 1f;

        // Скрываем UI смерти
        if (deathUI != null)
            deathUI.SetActive(false);

        // Восстанавливаем здоровье
        currentHealth = maxHealth;
        isDead = false;

        if (playerController != null)
        {
            // Включаем управление обратно
            playerController.enabled = true;
            Debug.Log("Player controller re-enabled");
        }

        Debug.Log("Player respawned! Health: " + currentHealth + "/" + maxHealth);
    }

    // Для смены точки респавна во время игры
    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
        initialPosition = newRespawnPoint.position;
        initialRotation = newRespawnPoint.rotation;
        Debug.Log("New respawn point set: " + initialPosition);
    }

    public void Heal(float healAmount)
    {
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        Debug.Log("Player healed! Health: " + currentHealth + "/" + maxHealth);
    }

    public bool IsAlive()
    {
        return !isDead;
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    // Для отладки
    [ContextMenu("Test Take 50 Damage")]
    void TestDamage()
    {
        TakeDamage(50);
    }

    [ContextMenu("Test Kill Player")]
    void TestKill()
    {
        TakeDamage(currentHealth);
    }

    [ContextMenu("Respawn Player")]
    void TestRespawn()
    {
        Respawn();
    }
}