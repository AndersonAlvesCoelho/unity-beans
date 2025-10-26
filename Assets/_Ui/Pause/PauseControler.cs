using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject pauseMenuUI;               // painel do pause
    [SerializeField] private string nomeCenaMenuPrincipal = "MenuPrincipal";

    [Header("Opções")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool gerenciarCursor = true;

    private bool isPaused;

    void Start()
    {
        isPaused = false;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        if (gerenciarCursor) SetCursor(false);
        AudioListener.pause = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // ---------- Botão: CONTINUAR ----------
    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (gerenciarCursor) SetCursor(false);
    }

    // ---------- Botão: MENU PRINCIPAL ----------
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (gerenciarCursor) SetCursor(false);
        SceneManager.LoadScene(nomeCenaMenuPrincipal);
    }

    // ---------- Botão: SAIR ----------
    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("Jogo encerrado.");
    }

    // ---------- Lógica de Pause ----------
    private void PauseGame()
    {
        isPaused = true;
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (gerenciarCursor) SetCursor(true);
    }

    private void SetCursor(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }
}

