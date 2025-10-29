using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death Settings")]
    public GameObject deathUI; // Перетащите сюда UI объект для смерти

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        // Скрываем UI смерти при старте
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

        // Проверка смерти
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

        // Можно добавить звук смерти или другие эффекты
    }

    // Метод для возрождения из UI
    public void Respawn()
    {
        Debug.Log("RESPAWNING PLAYER...");

        // Восстанавливаем время
        Time.timeScale = 1f;

        // Скрываем UI смерти
        if (deathUI != null)
            deathUI.SetActive(false);

        // Восстанавливаем здоровье
        currentHealth = maxHealth;
        isDead = false;

        Debug.Log("Player respawned! Health: " + currentHealth + "/" + maxHealth);
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
