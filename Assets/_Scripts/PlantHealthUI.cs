using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlantHealthUI : MonoBehaviour
{
    [Header("Referências")]
    public Image healthImage; // Arraste o objeto Image da UI da planta aqui
    public PlantGrowthController plantController; // Arraste o GameObject da Planta aqui

    [Header("Sprites de Vida (Ordem: 0=Cheio, Último=Vazio)")]
    public List<Sprite> healthSprites;

    private int lastDisplayedHealth = -1; // Usa int para corresponder à vida da planta

    void Start()
    {
        // Validações
        if (healthImage == null) { Debug.LogError("UI: Health Image missing!"); enabled = false; return; }
        if (plantController == null) {
            Debug.LogError("UI: Plant Controller não foi atribuído!");
            // Tenta encontrar a planta automaticamente pela Tag (opcional)
            GameObject plant = GameObject.FindWithTag("Plant"); // Assegure-se que sua planta tenha a Tag "Plant"
            if(plant != null) plantController = plant.GetComponent<PlantGrowthController>();
            if (plantController == null) { Debug.LogError("UI: Não foi possível encontrar o PlantGrowthController!"); enabled = false; return; }
        }
        if (healthSprites == null || healthSprites.Count < 2) { Debug.LogError("UI: Health Sprites missing or insufficient!"); enabled = false; return; }

        UpdateHealthDisplay(); // Atualiza no início
    }

    void Update()
    {
        // Otimização: Só atualiza se a vida REAL mudou
        if (plantController != null && plantController.CurrentHealth != lastDisplayedHealth)
        {
            UpdateHealthDisplay();
            lastDisplayedHealth = plantController.CurrentHealth; // Guarda a vida atual
        }
    }

    void UpdateHealthDisplay()
    {
        // Pega a vida (int) do script da planta
        float currentHealth = (float)plantController.CurrentHealth;
        float maxHealth = (float)plantController.GetMaxHealth(); // Usa a nova função getter

        float healthPercent = (maxHealth > 0) ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        int spriteIndex = CalculateSpriteIndex(healthPercent);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, healthSprites.Count - 1);

        if (healthSprites[spriteIndex] != null) {
             healthImage.sprite = healthSprites[spriteIndex];
        } else {
            Debug.LogWarning($"UI: Sprite no índice {spriteIndex} está faltando!");
        }
    }

    // A mesma lógica de cálculo de índice que corrigimos para o Player
    int CalculateSpriteIndex(float healthPercent)
    {
        int totalSprites = healthSprites.Count;
        if (totalSprites == 0) return 0;
        int lastIndex = totalSprites - 1; // Índice do VAZIO

        if (healthPercent >= 1.0f) return 0; // Cheio (Índice 0)
        if (healthPercent <= 0f) return lastIndex; // Vazio (Último Índice)

        float value = healthPercent * (totalSprites - 1);
        int segmentIndex = Mathf.FloorToInt(value);
        int spriteIndex = lastIndex - segmentIndex;

        if (spriteIndex == lastIndex) spriteIndex--; // Garante que > 0% não mostre o sprite vazio

        return Mathf.Clamp(spriteIndex, 0, lastIndex);
    }
}