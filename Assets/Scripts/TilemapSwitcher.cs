using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapSwitcher : MonoBehaviour
{
    [Header("Tile maps")]
    [SerializeField] private Tilemap[] tileMaps;
    private int currentIndex = 0;

    private void Start()
    {
        // without this, no tilemap is shown until the first wave clears -
        // UpdateTileMaps was previously only ever called reactively, never on load
        if (tileMaps != null && tileMaps.Length > 0)
        {
            UpdateTileMaps(currentIndex);
        }
    }

    // subscribe to shop closed broadcast - switching here (rather than on wave cleared)
    // means the shop opens over the CURRENT tilemap, and closing it reveals the new one,
    // instead of the switch happening instantly under the "Wave Cleared!" text before the
    // shop even opens.
    private void OnEnable()
    {
        GameEvents.OnShopClosed += NextTileMap;

    }

    // unsubscribe to avoid leaks
    private void OnDisable()
    {
        GameEvents.OnShopClosed -= NextTileMap;

    }

    private void NextTileMap()
    {
        if (tileMaps == null || tileMaps.Length == 0) return;

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