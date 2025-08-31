using UnityEngine;

public class Board : MonoBehaviour
{
    public int sizeX = 5;
    public int sizeZ = 5;
    public float cellSize = 1f;

    public float yHeight = 0f;     // altura para agentes y gemas
    public float tileYHeight = -0.2f; // altura del piso

    public bool generateTiles = true;
    public GameObject tilePrefab;

    public Vector3 IndexToWorld(Vector2Int ij)
    {
        float ox = transform.position.x;
        float oz = transform.position.z;
        float wx = ox + (ij.x - (sizeX - 1) * 0.5f) * cellSize;
        float wz = oz + (ij.y - (sizeZ - 1) * 0.5f) * cellSize;
        return new Vector3(wx, yHeight, wz); 
    }

    public bool InBounds(Vector2Int ij) =>
        ij.x >= 0 && ij.x < sizeX && ij.y >= 0 && ij.y < sizeZ;

    void Start()
    {
        if (generateTiles && tilePrefab != null)
        {
            for (int j = 0; j < sizeZ; j++)
                for (int i = 0; i < sizeX; i++)
                {
                    var p = IndexToWorld(new Vector2Int(i, j));
                    p.y = tileYHeight; 
                    Instantiate(tilePrefab, p, Quaternion.identity, transform);
                }
        }
    }
}
