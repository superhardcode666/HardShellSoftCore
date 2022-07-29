using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class WaveManager : MonoBehaviour
{
    public bool waveManagerRunning;
    
    [SerializeField] private Wave[] wavesAvailable;             // contains all waves to choose from, set in inspector

    private Wave currentWave;                                   // contains the currently selected wave to run
    private int currentWaveIndex;                               // wave index

    private int totalWaveCount;                                 // number of total waves available
    [SerializeField] private float timeBetweenWaves;            // brief respite in seconds between the last cleared wave and the next
    [SerializeField] private float waveCountDown;
    
    private float enemySearchCountdown = 1f;                    // frequency in which we check for enemies left alive
    
    private WaveTimer waveClearTimer;                           // records the total time from start to finish (i.e. boss kill) used for mission ranking
    
    [SerializeField] private List<SpawnPoint> spawnPoints;      // all SpawnPoints in the current scene to choose from
    private List<SpawnPoint> currentSpawnPointSelection;        // the current set of n randomly selected SpawnPoints
    
    [SerializeField] private List<GameObject> activeEnemies;    // all currently non-dead enemies used to determine when a wave is cleared
    [SerializeField] private Transform enemyTransform;          // just an empty transform containing all spawned enemies in the scene

    private bool allWavesCleared;

    
    private enum WaveState
    {
        COUNTDOWN, SPAWNING, WAITING, RANKING
    }

    [SerializeField] private WaveState waveState = WaveState.COUNTDOWN;
    
    private void Awake()
    {
        Enemy.onEnemyDeath += RemoveEnemy;
        SpawnPoint.onEnemyBirth += AddEnemy;
        
        totalWaveCount = wavesAvailable.Length;
        
        // Initialize System with first Wave
        currentWaveIndex = 0;
        SetNextWave(currentWaveIndex);
    }

    private void OnDestroy()
    {
        Enemy.onEnemyDeath -= RemoveEnemy;
        SpawnPoint.onEnemyBirth -= AddEnemy;
    }

    private void Start()
    {
        waveCountDown = timeBetweenWaves;
    }

    private void Update()
    {
        if (waveManagerRunning)
        {
            if (waveState == WaveState.RANKING)
            {
                // Invoke Mission Ranking Event here
                waveManagerRunning = false;
                return;
            }
        
            if (waveState == WaveState.WAITING)
            {
                if (IsWaveCleared())
                {
                    WaveCleared();
                }
                else
                {
                    return;
                }
            }
        
            if (waveCountDown <= 0)
            {
                if (waveState != WaveState.SPAWNING)
                {
                    StartWave(currentWave);     
                }       
            }
            else
            {
                waveCountDown -= Time.deltaTime;
            }
        }
    }

    private void WaveCleared()
    {
        Debug.Log($"Current Wave: {currentWave.waveUIMessage} cleared");
        currentWave.isCleared = true;

        waveState = WaveState.COUNTDOWN;
        waveCountDown = timeBetweenWaves;
        
        IncrementWave();
        SetNextWave(currentWaveIndex);
    }

    private bool IsWaveCleared()
    {
        enemySearchCountdown -= Time.deltaTime;
        
        if (enemySearchCountdown <= 0f)
        {
            enemySearchCountdown = 1f;
            if (activeEnemies.Count == 0)
            {
                return true;
            }
        }
        return false;
    }

    private void SetNextWave(int waveNumber)
    {
        if(waveNumber <= wavesAvailable.Length)
        {
            currentWave = wavesAvailable[waveNumber];
        }
    }
    
    private void StartWave(Wave wave)
    {
        waveState = WaveState.SPAWNING;
        
        var spawners = GetRandomSpawnPoints();
        SpawnEnemies(wave, spawners);
        
        waveState = WaveState.WAITING;
    }

    private void IncrementWave()
    {
        if (currentWaveIndex < totalWaveCount -1)
        {
            currentWaveIndex++;
        }
        else
        {
            // TODO Call Ui Event
            waveState = WaveState.RANKING;
            allWavesCleared = true;
        }
    }
    
    private List<SpawnPoint> SelectRandomSpawnPoints(int count)
    {
        return spawnPoints.OrderBy(point => Guid.NewGuid()).Take(count).ToList();
    }

    private List<SpawnPoint> GetRandomSpawnPoints()
    {
        int randomCount = UnityEngine.Random.Range(1, spawnPoints.Count);

        var random = new Random();
        var randomSpawnPoints = new List<SpawnPoint>();
        
        // shuffle the original list
        spawnPoints.OrderBy(point => random.Next());

        for (int i = 0; i < randomCount; i++)
        {
            randomSpawnPoints.Add(spawnPoints[i]);
        }
        
        return randomSpawnPoints;
    }

    private void SpawnEnemies(Wave wave, List<SpawnPoint> spawners)
    {
        foreach (var point in spawners)
        {
            point.SpawnMultipleWithDelay(wave, enemyTransform, wave.spawnRate);
        }
    }

    private void AddEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }
    
    private void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
}

[Serializable]
public class Wave
{
    public string waveUIMessage;
    public GameObject enemyType;
    public int enemyCountPerSpawnPoint;
    public float spawnRate;
    public bool isCleared;
}