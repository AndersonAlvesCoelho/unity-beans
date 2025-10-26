using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Referências")]
    public Image healthImage;                     // Image no Canvas
    public HealthSystem targetHealthSystem;       // Se não atribuído, tenta buscar via Tag

    [Header("Configuração de Tag (opcional)")]
    public string targetTag = "Player";           // Só usado se targetHealthSystem não estiver definido

    [Header("Sprites de Vida (Ordem: 0=Cheio, Último=Vazio)")]
    public List<Sprite> healthSprites;

    private float lastDisplayedHealth = -1f;

    void Start()
    {
        // Validações iniciais
        if (healthImage == null) { Debug.LogError("UI: Health Image missing!"); enabled = false; return; }

        if (targetHealthSystem == null)
        {
            if (!string.IsNullOrEmpty(targetTag))
            {
                GameObject targetGO = GameObject.FindWithTag(targetTag);
                if (targetGO != null) targetHealthSystem = targetGO.GetComponent<HealthSystem>();
            }

            if (targetHealthSystem == null)
            {
                Debug.LogError("UI: Target HealthSystem missing!"); 
                enabled = false; 
                return;
            }
        }

        if (healthSprites == null || healthSprites.Count == 0) { Debug.LogError("UI: Health Sprites missing!"); enabled = false; return; }

        UpdateHealthDisplay(); // Atualiza na inicialização
    }

    void Update()
    {
        if (targetHealthSystem != null && targetHealthSystem.CurrentHealth != lastDisplayedHealth)
        {
            UpdateHealthDisplay();
            lastDisplayedHealth = targetHealthSystem.CurrentHealth;
        }
    }

    void UpdateHealthDisplay()
    {
        float currentHealth = targetHealthSystem.CurrentHealth;
        float maxHealth = targetHealthSystem.maxHealth;

        float healthPercent = (maxHealth > 0) ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        int spriteIndex = CalculateSpriteIndex(healthPercent);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, healthSprites.Count - 1);

        if (healthSprites[spriteIndex] != null)
            healthImage.sprite = healthSprites[spriteIndex];
        else
            Debug.LogWarning($"UI: Sprite no índice {spriteIndex} está faltando!");
    }

    int CalculateSpriteIndex(float healthPercent)
    {
        int totalSprites = healthSprites.Count;
        if (totalSprites == 0) return 0;

        int lastIndex = totalSprites - 1;

        if (healthPercent >= 1f) return 0;
        if (healthPercent <= 0f) return lastIndex;

        int segmentIndex = Mathf.FloorToInt(healthPercent * (totalSprites - 1));
        return Mathf.Clamp(lastIndex - segmentIndex, 0, lastIndex);
    }
}
