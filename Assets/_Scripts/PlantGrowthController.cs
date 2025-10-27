using UnityEngine;

public class PlantGrowthController : MonoBehaviour
{
    private Animator animator;
    private Collider plantCollider;

    private bool isGrown = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        plantCollider = GetComponent<Collider>();
    }

    public void GrowPlant()
    {
        if(isGrown) return;
        isGrown = true;

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
