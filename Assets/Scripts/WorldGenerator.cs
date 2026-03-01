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
    public GameObject dirtBackgroundPrefab;

    [Header("Visuals - Tiles")]
    public Tilemap groundTilemap;
    public Tile grassTile; // Przypisz GrassTile.asset
    public Tile sandTile;  // Przypisz SandTile.asset

    [Header("Biome Settings")]
    [Tooltip("Im mniejsza liczba, tym wiêksze plamy biomów (0.005 - 0.02)")]
    public float biomeScale = 0.01f;

    [Header("World Settings")]
    public int chunkSize = 100;
    public int renderDistanceChunks = 5;
    public Transform worldContainer;

    private HashSet<ChunkCoords> generatedChunks = new HashSet<ChunkCoords>();

    // Szanse bazowe
    private float chanceCoal = 0.40f;
    private float chanceIron = 0.30f;
    private float chanceCopper = 0.25f;
    private float chanceWater = 0.20f;
    private float chanceOil = 0.10f;
    private float chanceSulfur = 0.05f;

    [Header("Randomness")]
    private float seedX;
    private float seedY;

    public void InitializeWorld()
    {
        if (worldContainer == null) worldContainer = new GameObject("--WORLD--").transform;
        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        generatedChunks.Clear();

        // --- LOSOWANIE SEEDU ---
        // Losujemy ogromne liczby, aby "przesun¹æ" mapê szumu w zupe³nie inne miejsce
        seedX = Random.Range(-100000f, 100000f);
        seedY = Random.Range(-100000f, 100000f);

        for (int cx = -renderDistanceChunks; cx <= renderDistanceChunks; cx++)
        {
            for (int cy = -renderDistanceChunks; cy <= renderDistanceChunks; cy++)
            {
                TryGenerateChunk(new ChunkCoords(cx, cy));
            }
        }
    }
    public void TryGenerateChunk(ChunkCoords coords)
    {
        if (generatedChunks.Contains(coords)) return;
        generatedChunks.Add(coords);
        GenerateChunk(coords);
    }

    private void GenerateChunk(ChunkCoords coords)
    {
        FillChunkWithBiomes(coords); // Nowa metoda wype³niania

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
                int offset = chunkSize / 2;
                Vector2Int localPoint = new Vector2Int(Random.Range(20, chunkSize - 20), Random.Range(20, chunkSize - 20));
                Vector2Int globalPos = new Vector2Int(
                    (coords.x * chunkSize) + localPoint.x - offset,
                    (coords.y * chunkSize) + localPoint.y - offset
                );

                // --- SPRAWDZANIE BIOMU PRZED SPAWNEM ---
                float finalBiomeValue = GetCurrentBiomeValue(globalPos.x, globalPos.y);
                bool isGrass = finalBiomeValue > 0.45f;

                // Woda tylko na trawie, Ropa tylko na piasku
                if (config.type == "water" && !isGrass) continue;
                if (config.type == "oil" && isGrass) continue;

                SpawnResource(config.prefab, globalPos, distFromCenter, config.type);
            }
        }
    }

    private float GetBiomeNoiseAt(float x, float y)
    {
        // Zamiast +10000f u¿ywamy naszych zmiennych
        return Mathf.PerlinNoise(x * biomeScale + seedX, y * biomeScale + seedY);
    }

    private float GetCurrentBiomeValue(float globalX, float globalY)
    {
        float noiseValue = GetBiomeNoiseAt(globalX, globalY);

        // Obliczamy dystans od œrodka œwiata (0,0)
        float distFromZero = Vector2.Distance(new Vector2(globalX, globalY), Vector2.zero);

        // Identyczny bonus jak w FillChunkWithBiomes
        float centerBonus = Mathf.Clamp01(1f - (distFromZero / 150f));
        return noiseValue + centerBonus;
    }

    private void FillChunkWithBiomes(ChunkCoords coords)
    {
        if (groundTilemap == null || grassTile == null || sandTile == null) return;

        int offset = chunkSize / 2;
        int startX = (coords.x * chunkSize) - offset;
        int startY = (coords.y * chunkSize) - offset;

        TileBase[] tileArray = new TileBase[chunkSize * chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                float globalX = startX + x;
                float globalY = startY + y;

                // Obliczamy dystans od œrodka œwiata (0,0)
                float distFromZero = Vector2.Distance(new Vector2(globalX, globalY), Vector2.zero);

                float noiseValue = GetBiomeNoiseAt(globalX, globalY);

                // --- P£YNNE PRZEJŒCIE W CENTRUM ---
                // Jeœli jesteœmy blisko œrodka (np. w promieniu 80 kratek), 
                // sztucznie podbijamy wartoœæ szumu, ¿eby faworyzowaæ trawê.
                // Im dalej od zera, tym mniejszy wp³yw tego "bonusu".
                float centerBonus = Mathf.Clamp01(1f - (distFromZero / 150f));
                noiseValue += centerBonus;

                tileArray[y * chunkSize + x] = (noiseValue > 0.45f) ? grassTile : sandTile;
            }
        }

        BoundsInt area = new BoundsInt(startX, startY, 0, chunkSize, chunkSize, 1);
        groundTilemap.SetTilesBlock(area, tileArray);
    }

    // --- KLUCZOWA METODA DLA KAMERY ---
    // Wywo³uj to z CameraController, aby generowaæ œwiat w locie


    private void SpawnResource(GameObject prefab, Vector2Int center, float dist, string type)
    {
        float baseSize = (Mathf.Sqrt(dist) * 6f) + 5f;

        if (type == "sulfur") baseSize *= 0.5f;
        if (type == "water" || type == "oil") baseSize *= 0.33f;

        int totalTiles = Mathf.Max(1, Mathf.RoundToInt(baseSize * Random.Range(0.7f, 1.3f)));

        List<Vector2Int> edgeTiles = new List<Vector2Int> { center };
        HashSet<Vector2Int> placedTiles = new HashSet<Vector2Int>();

        List<Vector2Int> resourcePositions = new List<Vector2Int>();

        int placed = 0;
        int attempts = 0;
        int maxAttempts = totalTiles * 30;

        while (placed < totalTiles && edgeTiles.Count > 0 && attempts < maxAttempts)
        {
            attempts++;
            int randomIndex = Random.Range(0, edgeTiles.Count);
            Vector2Int current = edgeTiles[randomIndex];

            if (GridManager.Instance.GetResourceDeposit(current) == null)
            {
                bool canPlace = true;
                if (type == "water" || type == "oil")
                {
                    if (HasAnyResourceInRadius(current, 2)) canPlace = false;
                }

                if (canPlace)
                {
                    InstantiateDeposit(prefab, current);
                    placedTiles.Add(current);
                    resourcePositions.Add(current); // Zapamiêtujemy pozycjê kropki
                    placed++;
                }

                foreach (Vector2Int neighbor in GetNeighbors(current))
                {
                    if (!placedTiles.Contains(neighbor) && !edgeTiles.Contains(neighbor))
                    {
                        float spreadChance = (type == "water" || type == "oil") ? 0.25f : 0.85f;
                        if (Random.value < spreadChance) edgeTiles.Add(neighbor);
                    }
                }
            }

            if (edgeTiles.Count == 0 || (Random.value < 0.1f && (type == "water" || type == "oil")))
            {
                Vector2Int jumpPos = center + new Vector2Int(Random.Range(-10, 11), Random.Range(-10, 11));
                if (!placedTiles.Contains(jumpPos)) edgeTiles.Add(jumpPos);
            }

            if (attempts % 5 == 0 && edgeTiles.Count > 10) edgeTiles.RemoveAt(0);
        }

        if (type == "water" || type == "oil")
        {
            GenerateOrganicDirtBlob(resourcePositions, 4); // Zwiêkszony zasiêg do 3 dla lepszego t³a
        }
    }

    private void GenerateOrganicDirtBlob(List<Vector2Int> corePositions, int radius)
    {
        HashSet<Vector2Int> dirtPositions = new HashSet<Vector2Int>();

        // Dla ka¿dego punktu surowca sprawdzamy otoczenie w promieniu ko³owym
        foreach (Vector2Int core in corePositions)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int targetPos = core + new Vector2Int(x, y);

                    // Sprawdzamy dystans euklidesowy (ko³owy) zamiast kwadratu
                    float distance = Vector2.Distance(core, targetPos);

                    // Jeœli mieœci siê w promieniu, dodajemy do zbioru (HashSet zapobiega duplikatom)
                    if (distance <= radius + 0.5f)
                    {
                        dirtPositions.Add(targetPos);
                    }
                }
            }
        }

        // Teraz fizycznie stawiamy prefaby z zapamiêtanych unikalnych pozycji
        foreach (Vector2Int pos in dirtPositions)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(pos);

            // Dodajemy ma³¹ losowoœæ na krawêdziach bloba, ¿eby by³ bardziej "poszarpany"
            // Sprawdzamy czy w danym miejscu ju¿ coœ nie stoi (opcjonalnie dla optymalizacji)
            Instantiate(dirtBackgroundPrefab, worldPos, Quaternion.identity, worldContainer);
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