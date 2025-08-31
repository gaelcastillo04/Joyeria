using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public string agentId = "A1";
    public GemColor Color = GemColor.Red;
    public Board board;
    public float moveSpeed = 2f;
    public float pickRadius = 0.35f;

    public int zoneMinX = 0, zoneMaxX = 1;
    public int zoneMinZ = 0, zoneMaxZ = 4;

    public AgentPhase phase = AgentPhase.Exploring;
    public Vector2Int cell;
    public List<Vector2Int> route = new();

    public Vector2Int homeCell;
    public bool goHomeFirst = true;

    private readonly Queue<Target> inbox = new();
    private Gem carried;
    private int deliveredCount = 0;

    public struct Target
    {
        public Vector2Int cell;
        public string reportedBy;
        public Target(Vector2Int c, string by) { cell = c; reportedBy = by; }
    }

    void Start()
    {
        if (board == null) { enabled = false; return; }
        GameManager.I.RegisterOwner(Color, this);
        cell = ClosestCellFromWorld(transform.position);
        transform.position = board.IndexToWorld(cell);
        StartCoroutine(MainLoop());
    }

    public void EnqueueTarget(Vector2Int c, Agent reportedBy)
    {
        inbox.Enqueue(new Target(c, reportedBy.agentId));
    }

    List<Vector2Int> DrainInboxDistinct()
    {
        var set = new HashSet<Vector2Int>();
        while (inbox.Count > 0) set.Add(inbox.Dequeue().cell);
        return set.ToList();
    }

    IEnumerator MainLoop()
    {
        if (goHomeFirst && cell != homeCell && board.InBounds(homeCell))
            yield return MoveToCell(homeCell);

        phase = AgentPhase.Exploring;
        foreach (var c in SerpentineCells())
        {
            yield return MoveToCell(c);
            TryPickGemsHere();
            if (carried != null) yield return DeliverCarriedGem();
        }

        while (true)
        {
            phase = AgentPhase.HarvestPlanning;
            var targets = DrainInboxDistinct();
            if (targets.Count == 0) break;

            route = GreedyRoute(cell, targets);

            phase = AgentPhase.Harvesting;
            foreach (var tcell in route)
            {
                yield return MoveToCell(tcell);
                TryPickGemsHere();
                if (carried != null) yield return DeliverCarriedGem();
            }

            yield return null;
        }

        phase = AgentPhase.Done;
    }

    IEnumerable<Vector2Int> SerpentineCells()
    {
        for (int z = zoneMinZ; z <= zoneMaxZ; z++)
        {
            if ((z - zoneMinZ) % 2 == 0)
                for (int x = zoneMinX; x <= zoneMaxX; x++)
                    yield return new Vector2Int(x, z);
            else
                for (int x = zoneMaxX; x >= zoneMinX; x--)
                    yield return new Vector2Int(x, z);
        }
    }

    List<Vector2Int> GreedyRoute(Vector2Int start, List<Vector2Int> goals)
    {
        var remaining = new HashSet<Vector2Int>(goals);
        var current = start;
        var plan = new List<Vector2Int>();
        while (remaining.Count > 0)
        {
            var next = remaining.OrderBy(g => Manhattan(current, g)).First();
            plan.Add(next);
            remaining.Remove(next);
            current = next;
        }
        return plan;
    }

    int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    IEnumerator MoveToCell(Vector2Int target)
    {
        if (!board.InBounds(target)) yield break;
        var start = board.IndexToWorld(cell);
        var end = board.IndexToWorld(target);
        Face(end);
        float dist = Vector3.Distance(start, end);
        float t = 0f;
        while (t < 1f)
        {
            float step = (moveSpeed * Time.deltaTime) / Mathf.Max(0.0001f, dist);
            t += step;
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }
        cell = target;
    }

    void Face(Vector3 worldTarget)
    {
        var dir = worldTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 1e-6f)
        {
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 0.5f);
        }
    }

    void TryPickGemsHere()
    {
        var gemsHere = GameManager.I.GetGemsAtCell(cell);
        if (gemsHere == null || gemsHere.Count == 0) return;
        foreach (var gem in gemsHere.ToArray())
        {
            if (gem == null) continue;
            if (carried != null) { gem.TryPickBy(this); continue; }
            gem.TryPickBy(this);
        }
    }

    public bool CanCarryAnother()
    {
        return carried == null;
    }

    public void PickUp(Gem g)
    {
        carried = g;
        g.transform.SetParent(transform);
        g.transform.localPosition = new Vector3(0f, 0.6f, 0f);
    }

    IEnumerator DeliverCarriedGem()
    {
        var dropCell = new Vector2Int(homeCell.x, deliveredCount);
        deliveredCount++;
        yield return MoveToCell(dropCell);
        var dropPos = board.IndexToWorld(dropCell);
        carried.transform.SetParent(null);
        carried.transform.position = dropPos;
        carried.SetDelivered();
        carried = null;
    }

    Vector2Int ClosestCellFromWorld(Vector3 pos)
    {
        float minD = float.MaxValue;
        Vector2Int best = new(0, 0);
        for (int j = 0; j < board.sizeZ; j++)
            for (int i = 0; i < board.sizeX; i++)
            {
                var p = board.IndexToWorld(new Vector2Int(i, j));
                float d = (p - pos).sqrMagnitude;
                if (d < minD) { minD = d; best = new Vector2Int(i, j); }
            }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, pickRadius);
    }
}
