using UnityEngine;

public class PlantGrowthController : MonoBehaviour
{
    private Animator animator;
    private Collider plantCollider;

    private bool isGrown = false;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        plantCollider = GetComponent<Collider>();

        // Adiciona uma verificação para ter certeza
        if (animator == null)
        {
            Debug.LogError($"PlantGrowthController em '{gameObject.name}' não conseguiu encontrar o Animator!");
        }
    }

    public void GrowPlant()
    {
        if(isGrown) return;
        isGrown = true;

        Debug.Log("Mandando a planta crescer!", animator != null ? animator.gameObject : (Object)this);
        
        if(animator != null)
        {
            animator.SetTrigger("isGrow"); // dispara animação
        }

        if(plantCollider != null)
        {
            plantCollider.enabled = false; // planta não sofre mais colisão/físicas
        }

        Debug.Log("Planta cresceu!");
    }
}
