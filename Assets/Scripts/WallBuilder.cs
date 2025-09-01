using UnityEngine;

public class WallBuilder : MonoBehaviour
{
    public Board board;
    public float wallHeight = 2.0f;
    public float wallThickness = 0.1f;
    public float ceilingY = 1.8f;     
    public Material wallMat;
    public Material glassMat;        
    public bool buildOnStart = true;

    void Start()
    {
        if (buildOnStart) Build();
    }

    public void Build()
    {
        if (board == null) return;

        float w = board.sizeX * board.cellSize;
        float d = board.sizeZ * board.cellSize;

        Vector3 origin = board.transform.position;
        Vector3 center = new Vector3(origin.x, 0f, origin.z);

        MakeWall("Wall_N", center + new Vector3(0, wallHeight * 0.5f, d * 0.5f + wallThickness * 0.5f),
                 new Vector3(w, wallHeight, wallThickness));
        MakeWall("Wall_S", center + new Vector3(0, wallHeight * 0.5f, -d * 0.5f - wallThickness * 0.5f),
                 new Vector3(w, wallHeight, wallThickness));
        MakeWall("Wall_E", center + new Vector3(w * 0.5f + wallThickness * 0.5f, wallHeight * 0.5f, 0),
                 new Vector3(wallThickness, wallHeight, d));
        MakeWall("Wall_W", center + new Vector3(-w * 0.5f - wallThickness * 0.5f, wallHeight * 0.5f, 0),
                 new Vector3(wallThickness, wallHeight, d));

        var ceil = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceil.name = "Ceiling_Glass";
        ceil.transform.SetParent(transform);
        ceil.transform.position = center + new Vector3(0, ceilingY, 0);
        ceil.transform.localScale = new Vector3(w, 0.05f, d);
        if (glassMat) ceil.GetComponent<Renderer>().sharedMaterial = glassMat;
    }

    void MakeWall(string name, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.position = pos;
        go.transform.localScale = scale;

        var col = go.GetComponent<BoxCollider>(); col.isTrigger = false;
        var rend = go.GetComponent<Renderer>(); if (wallMat) rend.sharedMaterial = wallMat;
    }
}