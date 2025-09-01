using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Gem : MonoBehaviour
{
    public GemColor color;
    public Vector2Int cell;

    private bool reported;
    private bool delivered;

    void Start() { GameManager.I.RegisterGem(this); }

    void OnDisable() { if (GameManager.I != null) GameManager.I.UnregisterGem(this); }
    void OnDestroy() { if (GameManager.I != null) GameManager.I.UnregisterGem(this); }

    public bool TryPickBy(Agent agent)
    {
        if (delivered) return false;

        if (agent.Color == color)
        {
            if (!agent.CanCarryAnother()) return false;
            GameManager.I.UnregisterGem(this);
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;
            agent.PickUp(this);
            return true;
        }
        else
        {
            if (!reported)
            {
                GameManager.I.ReportGemToOwner(color, cell, agent);
                reported = true;
            }
            return false;
        }
    }

    public void SetDelivered()
    {
        delivered = true;
        var col = GetComponent<Collider>();
        if (col) { col.enabled = true; col.isTrigger = true; }
        gameObject.layer = LayerMask.NameToLayer("Gem");
        gameObject.SetActive(true);
    }
}