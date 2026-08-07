using System;
using UnityEngine;


// Events and delegates help to decouple systems Ie the wave  manager doesn't need to know about the shop manager and vice versa
public class GameEvents
{
    // Brodcast to the  entire game when a wave is cleared(all enemies destroyed)
    public static event Action OnWaveCleared;

    // Broadcast when new wave starts
    public static event Action<int> OnWaveStarted;

    //Player events
    // Broadcast when player dies
    public static event Action OnPlayerDied;

    // Broadcast when player gets damaged
    public static event Action<int> OnPlayerDamaged;

    // Broadcast when player heals
    public static event Action<int> OnPlayerHealed;



    // Enemy events
    // Broadcast When enemy dies
    public static event Action<Transform> OnEnemyDied;



    // shop events
    // Broadcast when an upgrade is purchased
    public static event Action<UpgradeSO> OnUpgradePurchased;

    // Broadcast when the shop opens
    public static event Action OnShopOpened;

        // Broadcast when the shop closes
    public static event Action OnShopClosed;



    // Materials Event
    // Broadcast when materials change
    public static event Action<int> OnMaterialsChanged;

    // Bame events
    // Broadcast when the game is over
    public static event Action OnGameOver;

    // Broadcast when thr high score is updated
    public static event Action<int> OnHighScoreUpdated;


    public static void WaveCleared()
    {
        OnWaveCleared?.Invoke();
    }

    public static void WaveStarted(int waveNumber)
    {
        OnWaveStarted?.Invoke(waveNumber);
    }

    public static void PlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    public static void PlayerDamaged(int damage)
    {
        OnPlayerDamaged?.Invoke(damage);
    }

    public static void PlayerHealed(int amount)
    {
        OnPlayerHealed?.Invoke(amount);
    }

    public static void EnemyDied(Transform enemyTransform)
    {
        OnEnemyDied?.Invoke(enemyTransform);
    }

    public static void UpgradePurchased(UpgradeSO upgrade)
    {
        OnUpgradePurchased?.Invoke(upgrade);
    }

    public static void ShopOpened()
    {
        OnShopOpened?.Invoke();
    }

    public static void ShopClosed()
    {
        OnShopClosed?.Invoke();
    }

    public static void MaterialsChanged(int amount)
    {
        OnMaterialsChanged?.Invoke(amount);
    }

    public static void GameOver()
    {
        OnGameOver?.Invoke();
    }

    public static void HighScoreUpdated(int score)
    {
        OnHighScoreUpdated?.Invoke(score);
    }

}
