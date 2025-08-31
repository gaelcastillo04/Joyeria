using System.Collections.Generic;
using UnityEngine;

public class SimpleSpawner : MonoBehaviour
{
    public Board board;
    public GameObject robotPrefab;
    public GameObject gemPrefab;

    public int robots = 3;
    public int gemsPerColor = 3;

    public bool placeRobotsAtHome = false;

    public Material[] robotMaterials;
    public Material[] gemMaterials;
    public bool colorizeRobots = true;
    public bool colorizeGems = true;

    private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    void Start()
    {
        if (board == null || robotPrefab == null || gemPrefab == null) return;
        SpawnRobots();
        SpawnGems();
    }

    void SpawnRobots()
    {
        var rng = new System.Random();

        for (int k = 0; k < robots; k++)
        {
            var color = (GemColor)k;

            int zMinX = 0, zMaxX = 1;
            if (k == 1) { zMinX = 2; zMaxX = 3; }
            if (k == 2) { zMinX = 4; zMaxX = 4; }

            Vector2Int home = new Vector2Int(zMinX, 0);
            Vector2Int spawnCell = placeRobotsAtHome ? home : PickFreeCell(rng);
            occupiedCells.Add(spawnCell);

            var go = Instantiate(robotPrefab, board.IndexToWorld(spawnCell), Quaternion.identity);

            if (colorizeRobots && robotMaterials != null && robotMaterials.Length >= 3)
            {
                var r = GetRenderer(go);
                if (r != null) r.sharedMaterial = robotMaterials[(int)color];
            }

            var agent = go.GetComponent<Agent>();
            agent.board = board;
            agent.agentId = "A" + (k + 1);
            agent.Color = color;

            agent.zoneMinX = zMinX; agent.zoneMaxX = zMaxX;
            agent.zoneMinZ = 0; agent.zoneMaxZ = board.sizeZ - 1;

            agent.homeCell = home;
            agent.goHomeFirst = !placeRobotsAtHome;

            if (GameManager.I != null) GameManager.I.RegisterOwner(agent.Color, agent);
        }
    }

    void SpawnGems()
    {
        var rng = new System.Random();

        foreach (GemColor c in System.Enum.GetValues(typeof(GemColor)))
        {
            for (int g = 0; g < gemsPerColor; g++)
            {
                Vector2Int ij = PickFreeCell(rng);
                occupiedCells.Add(ij);

                var go = Instantiate(gemPrefab, board.IndexToWorld(ij), Quaternion.identity);

                if (colorizeGems && gemMaterials != null && gemMaterials.Length >= 3)
                {
                    var r = GetRenderer(go);
                    if (r != null) r.sharedMaterial = gemMaterials[(int)c];
                }

                var gem = go.GetComponent<Gem>();
                gem.color = c;
                gem.cell = ij;
            }
        }
    }

    Vector2Int PickFreeCell(System.Random rng)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            int i = rng.Next(0, board.sizeX);
            int j = rng.Next(0, board.sizeZ);
            var candidate = new Vector2Int(i, j);
            if (!occupiedCells.Contains(candidate))
                return candidate;
        }
        return new Vector2Int(0, 0);
    }

    Renderer GetRenderer(GameObject go)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null) return r;
        return go.GetComponentInChildren<Renderer>();
    }
}
