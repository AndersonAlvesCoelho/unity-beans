// PlantGrowthController.cs (Ajustado com Delay e Lógica de Trigger/Mudança de Cena)

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // <<--- ADICIONADO: Necessário para mudar de cena

public class PlantGrowthController : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Duração aproximada da animação 'Grow' em segundos.")]
    public float growthAnimationDuration = 2.0f; // Tempo de espera para a animação

    // --- NOVA VARIÁVEL ---
    [Header("Próxima Cena")]
    [Tooltip("Nome exato da cena a ser carregada (deve estar nas Build Settings).")]
    public string nextSceneName; 
    // --- FIM DA NOVA VARIÁVEL ---

    private Animator animator;
    private Collider plantCollider;
    private bool isGrown = false;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        plantCollider = GetComponent<Collider>();

        if (animator == null)
        {
            Debug.LogError($"PlantGrowthController em '{gameObject.name}' não conseguiu encontrar o Animator!");
        }
        if (plantCollider == null)
        {
            Debug.LogWarning($"PlantGrowthController em '{gameObject.name}' não tem Collider. Não poderá se tornar um trigger.");
        }

        // Validação para a nova variável
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning($"PlantGrowthController: 'Next Scene Name' não foi definido no Inspector. A planta não carregará uma nova cena.");
        }
    }

    // Função pública chamada pelo Spawner
    public void GrowPlant()
    {
        if (isGrown) return;
        isGrown = true; 

        Debug.Log("Mandando a planta crescer!", animator != null ? animator.gameObject : (Object)this);
        
        StartCoroutine(GrowthSequence());
    }

    // Corrotina que gerencia a animação e a mudança do collider
    private IEnumerator GrowthSequence()
    {
        if (animator != null)
        {
            animator.SetBool("IsGrown", true); 
        }

        yield return new WaitForSeconds(growthAnimationDuration);

        if (plantCollider != null)
        {
            plantCollider.enabled = true; 
            plantCollider.isTrigger = true; 
            Debug.Log($"Planta '{gameObject.name}' cresceu e seu collider é agora um Trigger.");
        }

        Debug.Log("Planta cresceu!");
    }

    // --- FUNÇÃO ATUALIZADA: Detecta o Player e MUDA A CENA ---
    private void OnTriggerEnter(Collider other)
    {
        // Verifica se a planta JÁ CRESCEU E se o objeto que entrou é o Player
        if (isGrown && other.CompareTag("Player"))
        {
            Debug.Log("Player interagiu com a Planta crescida! Carregando próxima cena...");

            // --- LÓGICA DE MUDAR DE CENA ---
            // Verifica se o nome da cena foi definido no Inspector
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                // Carrega a cena
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogError($"PlantGrowthController: Impossível carregar cena. 'Next Scene Name' não foi definido no Inspector do objeto {gameObject.name}!");
            }
        }
    }
}