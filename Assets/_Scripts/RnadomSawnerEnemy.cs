// RandomSpawnerEnemy.cs (Com Spawns Direcionais e Ondas - CORRIGIDO)

using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Para usar Listas

public class RandomSpawnerEnemy : MonoBehaviour
{
    // --- Estrutura para definir cada Onda ---
    [System.Serializable]
    public class Wave
    {
        public string waveName = "Onda";
        public int numberOfEnemies = 5;      // Quantos inimigos totais na onda
        public GameObject[] enemyPrefabs;    // Tipos de inimigos permitidos nesta onda
        public float spawnInterval = 1.0f;   // Tempo entre cada spawn DENTRO da onda
        // Poderíamos adicionar aqui: Modificadores de vida/dano para esta onda
    }

    [Header("Configuração das Ondas")]
    public List<Wave> waves;                 // Lista de todas as ondas
    public float timeBetweenWaves = 5.0f;    // Tempo de espera APÓS limpar uma onda
    public float initialSpawnDelay = 3.0f;   // Tempo antes da primeira onda começar

    [Header("Área de Spawn")]
    public Vector2 mapCenter = Vector2.zero; // Centro XZ do mapa
    public Vector2 mapSize = new Vector2(40f, 40f); // Largura (X) e Profundidade (Z) total do mapa
    public float spawnEdgeMargin = 2.0f;     // Quão longe da borda exata spawnar

    // Estado Interno
    private int currentWaveIndex = -1;
    private List<GameObject> activeEnemies = new List<GameObject>(); // Lista para rastrear inimigos vivos
    private bool isSpawningWave = false;
    private bool allWavesCompleted = false;

    // --- Inicialização ---
    void Start()
    {
        // Validações
        if (waves == null || waves.Count == 0) {
            Debug.LogError("Spawner: Nenhuma onda definida!");
            enabled = false; return;
        }
        if (mapSize.x <= spawnEdgeMargin * 2 || mapSize.y <= spawnEdgeMargin * 2) {
            Debug.LogError("Spawner: Tamanho do mapa muito pequeno para a margem definida!");
            enabled = false; return;
        }

        // Inicia o controlador das ondas após o delay inicial
        StartCoroutine(WaveController());
    }

    // --- Controlador Principal ---
    IEnumerator WaveController()
    {
        yield return new WaitForSeconds(initialSpawnDelay);

        // Loop principal: continua enquanto houver ondas para spawnar
        while (currentWaveIndex < waves.Count - 1)
        {
            // Se não estamos spawnando E a lista de inimigos ativos está vazia (onda anterior limpa)
            if (!isSpawningWave && activeEnemies.Count == 0)
            {
                // Avança para a próxima onda
                currentWaveIndex++;
                Wave currentWave = waves[currentWaveIndex];
                Debug.Log($"Iniciando Onda {currentWaveIndex + 1}/{waves.Count}: {currentWave.waveName}");

                // Inicia a rotina de spawn para a onda atual
                StartCoroutine(SpawnWave(currentWave));

                // Pequena pausa antes da próxima verificação do loop principal
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // Se ainda há inimigos ou está spawnando, espera um pouco e verifica de novo
                yield return new WaitForSeconds(1.0f);
            }
        }

        // Após sair do loop, espera a última onda ser limpa
        while (activeEnemies.Count > 0)
        {
             yield return new WaitForSeconds(1.0f);
        }

        // Todas as ondas foram completadas
        if (!allWavesCompleted)
        {
            allWavesCompleted = true;
            Debug.Log("****** TODAS AS ONDAS COMPLETADAS! ******");
            // Adicione aqui lógica de fim de jogo ou próxima fase
        }
    }

    // --- Rotina de Spawn para uma Onda Específica ---
    IEnumerator SpawnWave(Wave wave)
    {
        isSpawningWave = true;
        Debug.Log($"Spawnando {wave.numberOfEnemies} inimigos...");

        // Calcula limites da área de spawn
        float minX = mapCenter.x - mapSize.x / 2f + spawnEdgeMargin;
        float maxX = mapCenter.x + mapSize.x / 2f - spawnEdgeMargin;
        float minZ = mapCenter.y - mapSize.y / 2f + spawnEdgeMargin; // mapCenter.y é Z
        float maxZ = mapCenter.y + mapSize.y / 2f - spawnEdgeMargin; // mapCenter.y é Z

        int enemiesSpawned = 0;
        List<Vector3> usedCardinalPoints = new List<Vector3>(); // Para evitar spawnar no mesmo ponto cardeal

        // 1. Tenta garantir spawns direcionais (N, S, E, W) se houver inimigos suficientes
        if (wave.numberOfEnemies >= 4)
        {
            SpawnEnemyAt(GetDirectionalSpawnPosition(minX, maxX, minZ, maxZ, 'N'), wave.enemyPrefabs, usedCardinalPoints); enemiesSpawned++; yield return new WaitForSeconds(wave.spawnInterval);
            SpawnEnemyAt(GetDirectionalSpawnPosition(minX, maxX, minZ, maxZ, 'S'), wave.enemyPrefabs, usedCardinalPoints); enemiesSpawned++; yield return new WaitForSeconds(wave.spawnInterval);
            SpawnEnemyAt(GetDirectionalSpawnPosition(minX, maxX, minZ, maxZ, 'E'), wave.enemyPrefabs, usedCardinalPoints); enemiesSpawned++; yield return new WaitForSeconds(wave.spawnInterval);
            SpawnEnemyAt(GetDirectionalSpawnPosition(minX, maxX, minZ, maxZ, 'W'), wave.enemyPrefabs, usedCardinalPoints); enemiesSpawned++; yield return new WaitForSeconds(wave.spawnInterval);
        }

        // 2. Spawna o restante aleatoriamente
        while (enemiesSpawned < wave.numberOfEnemies)
        {
            SpawnEnemyAt(GetRandomSpawnPosition(minX, maxX, minZ, maxZ), wave.enemyPrefabs, usedCardinalPoints);
            enemiesSpawned++;
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        isSpawningWave = false;
        Debug.Log($"Spawn da Onda {currentWaveIndex + 1} completo.");
    }

    // --- Funções Auxiliares de Posição ---

    Vector3 GetDirectionalSpawnPosition(float minX, float maxX, float minZ, float maxZ, char direction)
    {
        float spawnX = Random.Range(minX, maxX); // Posição X geralmente aleatória
        float spawnZ = Random.Range(minZ, maxZ); // Posição Z geralmente aleatória

        switch (direction)
        {
            case 'N': spawnZ = maxZ; break; // Norte = Z máximo
            case 'S': spawnZ = minZ; break; // Sul = Z mínimo
            case 'E': spawnX = maxX; break; // Leste = X máximo
            case 'W': spawnX = minX; break; // Oeste = X mínimo
        }
        // Assume Y=0 para o chão, ajuste se necessário
        return new Vector3(spawnX, 0f, spawnZ);
    }

    Vector3 GetRandomSpawnPosition(float minX, float maxX, float minZ, float maxZ)
    {
        float spawnX = Random.Range(minX, maxX);
        float spawnZ = Random.Range(minZ, maxZ);
        // Assume Y=0 para o chão, ajuste se necessário
        return new Vector3(spawnX, 0f, spawnZ);
    }


    // --- Função para Spawnar um Inimigo ---
    // ***** CORREÇÃO APLICADA AQUI *****
    void SpawnEnemyAt(Vector3 position, GameObject[] allowedPrefabs, List<Vector3> usedPoints /* ignore used points for now */)
    {
        if (allowedPrefabs == null || allowedPrefabs.Length == 0)
        {
            Debug.LogError("Tentativa de spawn sem prefabs definidos na onda!");
            return;
        }
        // Escolhe um tipo aleatório permitido nesta onda
        GameObject prefabToSpawn = allowedPrefabs[Random.Range(0, allowedPrefabs.Length)];

        // Instancia o inimigo
        GameObject spawnedEnemy = Instantiate(prefabToSpawn, position, Quaternion.identity); // Rotação padrão

        // Adiciona à lista de inimigos ativos
        activeEnemies.Add(spawnedEnemy);

        // --- IMPORTANTE: Conecta o evento de morte ---
        HealthSystem health = spawnedEnemy.GetComponent<HealthSystem>();
        if (health != null)
        {
            // Adiciona um "ouvinte" para quando o inimigo morrer
            // Usaremos um componente auxiliar simples para isso
            EnemyDeathListener listener = spawnedEnemy.AddComponent<EnemyDeathListener>();
            listener.spawner = this; // Dá ao ouvinte uma referência a este spawner
        } else {
             Debug.LogError($"Prefab {prefabToSpawn.name} não tem HealthSystem! Spawner não será notificado da morte.");
        }
    }

    // --- Função PÚBLICA Chamada pelo Inimigo ao Morrer (via Listener) ---
    public void ReportEnemyDeath(GameObject deadEnemy)
    {
        // Remove o inimigo da lista de ativos
        if (activeEnemies.Contains(deadEnemy))
        {
            activeEnemies.Remove(deadEnemy);
            Debug.Log($"Inimigo {deadEnemy.name} removido da lista. Restantes: {activeEnemies.Count}");

            // Se este era o último E a próxima onda ainda não começou E não é a última onda...
            // O WaveController já cuida disso, mas podemos adicionar um log
             if (activeEnemies.Count == 0 && !isSpawningWave && currentWaveIndex < waves.Count - 1)
             {
                 Debug.Log($"Onda {currentWaveIndex + 1} concluída! Aguardando {timeBetweenWaves}s para a próxima...");
                 // O WaveController vai pegar isso no próximo loop e iniciar o delay
             }
        }
    }
}