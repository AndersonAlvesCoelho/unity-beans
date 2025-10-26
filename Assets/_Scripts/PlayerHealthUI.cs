using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Referências")]
    public Image healthImage;
    public HealthSystem playerHealthSystem;

    [Header("Sprites de Vida (Ordem: 0=Cheio, Último=Vazio)")]
    public List<Sprite> healthSprites;

    private float lastDisplayedHealth = -1f; // Guarda a última vida exibida

    void Start()
    {
        // Validações
        if (healthImage == null) { Debug.LogError("UI: Health Image missing!"); enabled = false; return; }
        if (playerHealthSystem == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if(player != null) playerHealthSystem = player.GetComponent<HealthSystem>();
            if (playerHealthSystem == null) { Debug.LogError("UI: Player HealthSystem missing!"); enabled = false; return; }
        }
        if (healthSprites == null || healthSprites.Count == 0) { Debug.LogError("UI: Health Sprites missing!"); enabled = false; return; }

        UpdateHealthDisplay(); // Atualiza no início
    }

    void Update()
    {
        if (playerHealthSystem != null && playerHealthSystem.CurrentHealth != lastDisplayedHealth)
        {
            UpdateHealthDisplay();
            lastDisplayedHealth = playerHealthSystem.CurrentHealth; // Guarda a vida atual
        }
    }

    void UpdateHealthDisplay()
    {
        float currentHealth = playerHealthSystem.CurrentHealth;
        float maxHealth = playerHealthSystem.maxHealth;

        float healthPercent = (maxHealth > 0) ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        int spriteIndex = CalculateSpriteIndex(healthPercent);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, healthSprites.Count - 1);

        if (healthSprites[spriteIndex] != null) // Verifica se o sprite existe na lista
        {
             healthImage.sprite = healthSprites[spriteIndex];
        } else {
            Debug.LogWarning($"UI: Sprite no índice {spriteIndex} está faltando!");
        }
    }

    int CalculateSpriteIndex(float healthPercent)
    {
        int totalSprites = healthSprites.Count;
        if (totalSprites == 0) return 0; 

        int lastIndex = totalSprites - 1; // Índice do sprite VAZIO )

        // Caso 1: Vida cheia (exatamente 100%) -> Mostra o primeiro sprite (índice 0)
        if (healthPercent >= 1.0f)
        {
            return 0;
        }

        // Caso 2: Morto (vida <= 0) -> Mostra o último sprite (vazio)
        if (healthPercent <= 0f)
        {
            return lastIndex; // Ex: Health_07 (índice 6)
        }

        // Caso 3: Vida parcial (entre > 0% e < 100%)
        else
        {
            // Temos 'totalSprites - 1' segmentos visíveis de vida (ex: 6 segmentos para 7 sprites)
            // Queremos mapear a porcentagem para um índice INVERSO (0 = cheio, 6 = vazio)
            // Usamos FloorToInt para garantir que só mudamos para o próximo sprite (mais vazio)
            // quando a vida CAIR ABAIXO do limite daquele sprite.

            // Multiplica a porcentagem pelo número de SEGMENTOS
            float value = healthPercent * (totalSprites - 1); // Mapeia (0, 1) para (0, 6)

            // Floor pega o "degrau" atual baseado na porcentagem
            int segmentIndex = Mathf.FloorToInt(value); // Ex: 0.9 -> Floor(5.4) = 5; 0.1 -> Floor(0.6) = 0

            // Inverte para obter o índice do sprite (0=cheio, 6=vazio)
            // Índice = (Total de Sprites - 1) - Segmento Atual
            int spriteIndex = lastIndex - segmentIndex; // Ex: 0.9 -> 6 - 5 = 1; 0.1 -> 6 - 0 = 6

            // Clamp final apenas por segurança extrema
            return Mathf.Clamp(spriteIndex, 0, lastIndex);
        }
    }
}