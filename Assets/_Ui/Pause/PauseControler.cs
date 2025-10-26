using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenuController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject pauseMenuUI;               // painel do pause
    [SerializeField] private string nomeCenaMenu = "Menu";

    [Header("Opções")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool gerenciarCursor = true;

    private bool isPaused;
    private CanvasGroup pauseCG;
    private EventSystem evt;

    void Awake()
    {
        // CanvasGroup é a forma mais segura de garantir clique nos botões
        if (pauseMenuUI)
        {
            pauseCG = pauseMenuUI.GetComponent<CanvasGroup>();
            if (!pauseCG) pauseCG = pauseMenuUI.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        EnsureEventSystem();                 // garante EventSystem + InputModule
        HidePausePanel();                    // esconde com raycasts desativados
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (gerenciarCursor) SetCursor(false);
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
        HidePausePanel();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (gerenciarCursor) SetCursor(false);
    }

    // ---------- Botão: MENU PRINCIPAL ----------
    public void ReturnToMenu()
    {
       
        SceneManager.LoadScene(nomeCenaMenu);
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
        ShowPausePanel();
        Time.timeScale = 0f;                // pausar jogo não bloqueia UI
        AudioListener.pause = true;
        if (gerenciarCursor) SetCursor(true);
    }

    private void ShowPausePanel()
    {
        if (!pauseMenuUI) return;
        pauseMenuUI.SetActive(true);
        if (pauseCG)
        {
            pauseCG.alpha = 1f;
            pauseCG.interactable = true;
            pauseCG.blocksRaycasts = true;  // <- essencial para os cliques passarem
        }
    }

    private void HidePausePanel()
    {
        if (!pauseMenuUI) return;
        if (pauseCG)
        {
            pauseCG.alpha = 0f;
            pauseCG.interactable = false;
            pauseCG.blocksRaycasts = false; // <- evita bloquear jogo quando escondido
        }
        pauseMenuUI.SetActive(false);
    }

    private void SetCursor(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void EnsureEventSystem()
    {
        evt = EventSystem.current;
        if (!evt)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            evt = go.GetComponent<EventSystem>();
        }

        // Garante que há um InputModule compatível com o seu sistema de Input
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // Novo Input System
        if (!evt.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>())
        {
            evt.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#else
        // Legacy (Standalone) Input Manager
        if (!evt.GetComponent<StandaloneInputModule>())
        {
            evt.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
}
