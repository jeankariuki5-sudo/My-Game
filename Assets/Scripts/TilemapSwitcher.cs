using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapSwitcher : MonoBehaviour
{
    [Header("Tile maps")]
    [SerializeField] private Tilemap[] tileMaps;
    private int currentIndex = 0;
    // subscribe to wave cleared broadcast
    private void OnEnable()
    {
        // listen to the wave cleared event
        GameEvents.OnWaveCleared += NextTileMap;

    }

    // unsubscribe to wave cleared broadcat
    private void OnDisable()
    {
        GameEvents.OnWaveCleared -= NextTileMap;

    }

    private void NextTileMap()
    {
        // move to the next tilemap. loop back to 0 when at the end
        currentIndex = (currentIndex + 1) % tileMaps.Length;
        UpdateTileMaps(currentIndex);
    }

    public void UpdateTileMaps(int currentIndex)
    {
        // Hide all tilemaps.show only the current one
        for(int i =0; i < tileMaps.Length; i++)
        {
            tileMaps[i].gameObject.SetActive(i == currentIndex);
        }

    }
}
