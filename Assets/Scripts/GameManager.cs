using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    private readonly Dictionary<GemColor, Agent> ownerByColor = new();
    private readonly Dictionary<Vector2Int, List<Gem>> gemsAtCell = new();
    private readonly Dictionary<Vector2Int, Agent> agentAtCell = new();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    public void RegisterOwner(GemColor color, Agent agent) { ownerByColor[color] = agent; }
    public Agent GetOwner(GemColor color) { ownerByColor.TryGetValue(color, out var a); return a; }

    public void ReportGemToOwner(GemColor color, Vector2Int cell, Agent reportedBy)
    {
        var owner = GetOwner(color);
        if (owner != null && owner != reportedBy) owner.EnqueueTarget(cell, reportedBy);
    }

    public void RegisterGem(Gem g)
    {
        if (!gemsAtCell.TryGetValue(g.cell, out var list))
        {
            list = new List<Gem>();
            gemsAtCell[g.cell] = list;
        }
        if (!list.Contains(g)) list.Add(g);
    }

    public void UnregisterGem(Gem g)
    {
        if (gemsAtCell.TryGetValue(g.cell, out var list))
        {
            list.Remove(g);
            if (list.Count == 0) gemsAtCell.Remove(g.cell);
        }
    }

    public List<Gem> GetGemsAtCell(Vector2Int cell)
    {
        return gemsAtCell.TryGetValue(cell, out var list) ? new List<Gem>(list) : new List<Gem>();
    }

    public bool HasGemAtCell(Vector2Int cell)
    {
        return gemsAtCell.TryGetValue(cell, out var list) && list.Count > 0;
    }

    public bool TryReserveAgentCell(Vector2Int target, Agent agent)
    {
        if (agentAtCell.TryGetValue(target, out var who) && who != null && who != agent) return false;
        agentAtCell[target] = agent;
        return true;
    }

    public void ReleaseAgentCell(Vector2Int cell, Agent agent)
    {
        if (agentAtCell.TryGetValue(cell, out var who) && who == agent) agentAtCell.Remove(cell);
    }

    public bool IsAgentCellFree(Vector2Int cell) => !agentAtCell.ContainsKey(cell);
}