using Futurift;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private FuturiftController playerController;
    [SerializeField] public Canvas menu;

    private void Awake()
    {
        Time.timeScale = 0f;
        if (playerController != null)
        {
            playerController.OnPlayerDeath();
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1.0f;

        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("Player controller re-enabled");
        }

        menu.enabled = false;
    }

    public void Exit()
    {
        Application.Quit();
    }
}
