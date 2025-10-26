using UnityEngine;

public class HealthUISingleton : MonoBehaviour
{

    // A inst�ncia est�tica e p�blica que pode ser acessada de qualquer lugar
    public static HealthUISingleton Instance { get; private set; }

    // Vari�veis de exemplo
    private void Awake()
    {
        // Verifica se j� existe uma inst�ncia
        if (Instance == null)
        {
            // Se n�o, define esta como a inst�ncia
            Instance = this;
            
            // (Opcional) Impede que este objeto seja destru�do ao carregar uma nova cena
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Se uma inst�ncia j� existe e n�o � esta, destr�i este objeto
            // Isso garante que apenas uma inst�ncia exista.
            Debug.LogWarning("Uma segunda inst�ncia do GameManager foi detectada. Destruindo-a.");
            Destroy(gameObject);
        }
    }
    public void funcaoexemplo()
    {
        print("funcionou");
    }

}