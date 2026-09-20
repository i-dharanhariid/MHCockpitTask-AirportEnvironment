using System.Collections.Generic;
using UnityEngine;

/// <summary>Marks a place pedestrians like to stand (queue, cafe, baggage claim, gate, lounge...).</summary>
public class NpcPoi : MonoBehaviour
{
    public string poiType = "Generic";
    public float radius = 4f;

    public static readonly List<NpcPoi> All = new List<NpcPoi>();

    /// <summary>0 = ground floor, 1 = upper floor.</summary>
    public int Floor { get { return transform.position.y > 4f ? 1 : 0; } }

    private void OnEnable() { All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
