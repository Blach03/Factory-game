using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject ironPrefab;
    public GameObject coalPrefab;
    public GameObject copperPrefab;
    public GameObject oilPrefab;
    public GameObject waterPrefab;
    public GameObject sulfurPrefab;

    [Header("Visuals - Tilemap")]
    public Tilemap groundTilemap; // Przeci¹gnij tutaj obiekt GroundTilemap z hierarchii
    public Tile groundTile;

    [Header("World Settings")]
    public int chunkSize = 100;
    public int renderDistanceChunks = 5;
    public Transform worldContainer;

    // Szanse bazowe
    private float chanceCoal = 0.40f;
    private float chanceIron = 0.30f;
    private float chanceCopper = 0.25f;
    private float chanceWater = 0.20f;
    private float chanceOil = 0.10f;
    private float chanceSulfur = 0.05f;

    public void InitializeWorld()
    {
        if (worldContainer == null) worldContainer = new GameObject("--WORLD--").transform;

        // Czyœcimy tilemapê przed now¹ generacj¹
        if (groundTilemap != null) groundTilemap.ClearAllTiles();

        for (int cx = -renderDistanceChunks; cx <= renderDistanceChunks; cx++)
        {
            for (int cy = -renderDistanceChunks; cy <= renderDistanceChunks; cy++)
            {
                GenerateChunk(new ChunkCoords(cx, cy));
            }
        }
    }

    private void GenerateChunk(ChunkCoords coords)
    {
        FillChunkWithGroundTilemap(coords);
        bool isStartingChunk = (coords.x == 0 && coords.y == 0);
        float distFromCenter = Mathf.Sqrt(coords.x * coords.x + coords.y * coords.y);

        var resourceConfigs = new[] {
        new { prefab = coalPrefab, chance = isStartingChunk ? 1f : chanceCoal, type = "coal" },
        new { prefab = ironPrefab, chance = isStartingChunk ? 1f : chanceIron, type = "iron" },
        new { prefab = copperPrefab, chance = isStartingChunk ? 1f : chanceCopper, type = "copper" },
        new { prefab = waterPrefab, chance = isStartingChunk ? 1f : chanceWater, type = "water" },
        new { prefab = oilPrefab, chance = distFromCenter < 3 ? 0f : chanceOil, type = "oil" },
        new { prefab = sulfurPrefab, chance = distFromCenter < 3 ? 0f : chanceSulfur, type = "sulfur" }
    };

        foreach (var config in resourceConfigs)
        {
            if (Random.value <= config.chance)
            {
                // Losujemy pozycjê wewn¹trz chunka (0 do 99)
                Vector2Int localPoint = new Vector2Int(Random.Range(20, chunkSize - 20), Random.Range(20, chunkSize - 20));

                // --- POPRAWKA WYCENTROWANIA ---
                // Zamiast: (coords.x * 100) + local
                // Robimy: (coords.x * 100) + local - 50
                // Dziêki temu dla chunka (0,0) zakres to -50 do +49, a œrodek to (0,0)
                int offset = chunkSize / 2;
                Vector2Int globalPos = new Vector2Int(
                    (coords.x * chunkSize) + localPoint.x - offset,
                    (coords.y * chunkSize) + localPoint.y - offset
                );

                SpawnResource(config.prefab, globalPos, distFromCenter, config.type);
            }
        }
    }

    private void FillChunkWithGroundTilemap(ChunkCoords coords)
    {
        if (groundTilemap == null || groundTile == null) return;

        int offset = chunkSize / 2;
        int startX = (coords.x * chunkSize) - offset;
        int startY = (coords.y * chunkSize) - offset;

        // Przygotowujemy tablicê kafelków dla ca³ego chunka
        TileBase[] tileArray = new TileBase[chunkSize * chunkSize];
        for (int i = 0; i < tileArray.Length; i++)
        {
            tileArray[i] = groundTile;
        }

        // Definiujemy obszar (Bounds) dla tego chunka
        BoundsInt area = new BoundsInt(startX, startY, 0, chunkSize, chunkSize, 1);

        // Ustawiamy wszystkie kafelki jednym poleceniem (to jest klucz do wydajnoœci)
        groundTilemap.SetTilesBlock(area, tileArray);

        // Uwaga: Tilemapa nie obs³uguje ³atwo losowej rotacji na pojedynczym kafelku przez SetTilesBlock.
        // Jeœli chcesz rotacji, musia³byœ u¿yæ SetTileFlags i SetTransformMatrix dla ka¿dej komórki,
        // co nieco spowolni generowanie. Lepiej zostawiæ to jednolite dla maksymalnej prêdkoœci.
    }

    private void SpawnResource(GameObject prefab, Vector2Int center, float dist, string type)
    {
        // Rozmiar bazowy skalowany sqrt(odleg³oœæ)
        float baseSize = (Mathf.Sqrt(dist + 1) * 5f) + 3f;

        // --- TWOJE NOWE MODYFIKATORY ---
        if (type == "sulfur") baseSize *= 0.5f;
        if (type == "water" || type == "oil") baseSize *= 0.33f; // Redukcja iloœci surowca o 3 razy

        int totalTiles = Mathf.RoundToInt(baseSize * Random.Range(0.7f, 1.3f));

        // Zamiast kolejki u¿ywamy Listy, ¿eby móc losowaæ krawêdzie (bardziej naturalny kszta³t)
        List<Vector2Int> edgeTiles = new List<Vector2Int> { center };
        HashSet<Vector2Int> placedTiles = new HashSet<Vector2Int>();

        int placed = 0;
        int attempts = 0;
        int maxAttempts = totalTiles * 20;

        while (placed < totalTiles && edgeTiles.Count > 0 && attempts < maxAttempts)
        {
            attempts++;
            // Losowy wybór z listy krawêdzi sprawia, ¿e blob "wyci¹ga siê" w ró¿ne strony nieliniowo
            int randomIndex = Random.Range(0, edgeTiles.Count);
            Vector2Int current = edgeTiles[randomIndex];

            if (GridManager.Instance.GetResourceDeposit(current) == null)
            {
                bool canPlace = true;

                // --- ROZPROSZENIE P£YNÓW (Logika z Twojego rysunku) ---
                if (type == "water" || type == "oil")
                {
                    // Rozpychamy o 2 pola (Radius 2 sprawdza obszar 5x5)
                    // Dziêki temu ka¿da "kropka" ma wokó³ siebie woln¹ przestrzeñ
                    if (HasAnyResourceInRadius(current, 2))
                    {
                        canPlace = false;
                    }
                }

                if (canPlace)
                {
                    InstantiateDeposit(prefab, current);
                    placedTiles.Add(current);
                    placed++;
                }

                // Dodajemy s¹siadów do listy krawêdzi
                foreach (Vector2Int neighbor in GetNeighbors(current))
                {
                    if (!placedTiles.Contains(neighbor) && !edgeTiles.Contains(neighbor))
                    {
                        // P³yny rozprzestrzeniaj¹ siê rzadziej, co tworzy "rozstrzelony" efekt
                        float spreadChance = (type == "water" || type == "oil") ? 0.25f : 0.85f;
                        if (Random.value < spreadChance)
                        {
                            edgeTiles.Add(neighbor);
                        }
                    }
                }
            }

            // Jeœli utknêliœmy (brak miejsca przy rozproszeniu), "skaczemy" w nowe miejsce w promieniu z³o¿a
            if (edgeTiles.Count == 0 || (Random.value < 0.1f && (type == "water" || type == "oil")))
            {
                Vector2Int jumpPos = center + new Vector2Int(Random.Range(-10, 11), Random.Range(-10, 11));
                if (!placedTiles.Contains(jumpPos)) edgeTiles.Add(jumpPos);
            }

            // Usuwamy stare punkty z krawêdzi, ¿eby nie mieliæ ich w nieskoñczonoœæ
            if (attempts % 5 == 0 && edgeTiles.Count > 10) edgeTiles.RemoveAt(0);
        }
    }

    private bool HasAnyResourceInRadius(Vector2Int pos, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0) continue;
                Vector2Int checkPos = pos + new Vector2Int(x, y);
                if (GridManager.Instance.GetResourceDeposit(checkPos) != null) return true;
            }
        }
        return false;
    }

    private void InstantiateDeposit(GameObject prefab, Vector2Int pos)
    {
        GameObject go = Instantiate(prefab, worldContainer);
        ResourceDeposit deposit = go.GetComponent<ResourceDeposit>();
        deposit.Initialize(pos);
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        return new List<Vector2Int> {
            pos + Vector2Int.up, pos + Vector2Int.down, pos + Vector2Int.left, pos + Vector2Int.right
        };
    }
}