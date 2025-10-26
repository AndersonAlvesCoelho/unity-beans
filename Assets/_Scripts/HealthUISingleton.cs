using UnityEngine;

public class HealthUISingleton : MonoBehaviour
{

    // A instância estática e pública que pode ser acessada de qualquer lugar
    public static HealthUISingleton Instance { get; private set; }

    // Variáveis de exemplo
    private void Awake()
    {
        // Verifica se já existe uma instância
        if (Instance == null)
        {
            // Se não, define esta como a instância
            Instance = this;
            
            // (Opcional) Impede que este objeto seja destruído ao carregar uma nova cena
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Se uma instância já existe e não é esta, destrói este objeto
            // Isso garante que apenas uma instância exista.
            Debug.LogWarning("Uma segunda instância do GameManager foi detectada. Destruindo-a.");
            Destroy(gameObject);
        }
    }
    public void funcaoexemplo()
    {
        print("funcionou");
    }

}