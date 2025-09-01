
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

    public int zoneMinX = 0, zoneMaxX = 1;
    public int zoneMinZ = 0, zoneMaxZ = 4;

    public AgentPhase phase = AgentPhase.Exploring;
    public Vector2Int cell;
    public List<Vector2Int> route = new();

    public Vector2Int homeCell;
    public bool goHomeFirst = true;

    private readonly Queue<Target> inbox = new();
    private Gem carried;
    private Rigidbody rb;

    public struct Target
    {
        public Vector2Int cell;
        public string reportedBy;
        public Target(Vector2Int c, string by) { cell = c; reportedBy = by; }
    }

    void Start()
    {
        if (board == null) { enabled = false; return; }
        rb = GetComponent<Rigidbody>();
        GameManager.I.RegisterOwner(Color, this);
        cell = ClosestCellFromWorld(transform.position);
        transform.position = board.IndexToWorld(cell);
        GameManager.I.TryReserveAgentCell(cell, this);
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

        yield return PostRunTidy();

        phase = AgentPhase.Done;
    }

    IEnumerator PostRunTidy()
    {
        yield return new WaitForSeconds(10f);

        int safety = 4;
        while (!IsHomeColumnStable() && safety-- > 0)
        {
            CompactHomeLine();
            yield return null;
        }
    }

    bool IsHomeColumnStable()
    {
        int x = homeCell.x;
        int maxZ = board.sizeZ;

        bool foundEmpty = false;
        for (int z = 0; z < maxZ; z++)
        {
            var here = new Vector2Int(x, z);
            var list = GameManager.I.GetGemsAtCell(here);

            if (list.Count == 0)
            {
                foundEmpty = true;
            }
            else
            {
                if (foundEmpty && list.Any(g => g != null && g.color == Color))
                    return false;
            }
        }
        return true; 
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

    int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    IEnumerator MoveToCell(Vector2Int target)
    {
        if (!board.InBounds(target)) yield break;

        while (!GameManager.I.TryReserveAgentCell(target, this))
            yield return null;

        var end = board.IndexToWorld(target);
        Face(end);

        while ((transform.position - end).sqrMagnitude > 0.0004f)
        {
            var newPos = Vector3.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);
            if (rb != null) rb.MovePosition(newPos);
            else transform.position = newPos;
            yield return null;
        }

        var prev = cell;
        cell = target;
        GameManager.I.ReleaseAgentCell(prev, this);
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

    public bool CanCarryAnother() => carried == null;

    public void PickUp(Gem g)
    {
        carried = g;
        g.transform.SetParent(transform);
        g.transform.localPosition = new Vector3(0f, 0.6f, 0f);
    }

    IEnumerator DeliverCarriedGem()
    {
        Vector2Int dropCell; int z = 0;
        while (true)
        {
            dropCell = new Vector2Int(homeCell.x, z);
            var world = board.IndexToWorld(dropCell);

            if (!GameManager.I.HasGemAtCell(dropCell))
            {
                int gemMask = LayerMask.GetMask("Gem");
                if (!Physics.CheckSphere(world, 0.2f, gemMask, QueryTriggerInteraction.Collide)) break;
            }

            z++; yield return null;
        }

        yield return MoveToCell(dropCell);

        var dropPos = board.IndexToWorld(dropCell);
        carried.transform.SetParent(null);
        carried.cell = dropCell;
        carried.transform.position = dropPos;
        carried.SetDelivered();
        GameManager.I.RegisterGem(carried);

        CompactHomeLine();
        carried = null;
    }

    void CompactHomeLine()
    {
        int x = homeCell.x;
        int maxZ = board.sizeZ;

        var ourGems = new List<(Gem gem, int z)>();
        var emptySlots = new List<int>();

        for (int z = 0; z < maxZ; z++)
        {
            var cellPos = new Vector2Int(x, z);
            var list = GameManager.I.GetGemsAtCell(cellPos);

            if (list.Count == 0) { emptySlots.Add(z); continue; }

            foreach (var g in list)
                if (g != null && g.color == Color)
                    ourGems.Add((g, z));
        }

        ourGems.Sort((a, b) => a.z.CompareTo(b.z));
        emptySlots.Sort();

        foreach (var slotZ in emptySlots)
        {
            int idx = ourGems.FindIndex(t => t.z > slotZ);
            if (idx < 0) continue;

            var g = ourGems[idx].gem;
            GameManager.I.UnregisterGem(g);
            g.cell = new Vector2Int(x, slotZ);
            g.transform.position = board.IndexToWorld(g.cell);
            g.SetDelivered();
            GameManager.I.RegisterGem(g);

            ourGems[idx] = (g, slotZ);
        }
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
}