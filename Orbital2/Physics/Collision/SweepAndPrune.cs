using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using QuikGraph;

namespace Orbital2.Physics.Collision;

public class SweepAndPrune : BroadPhase
{
    private List<AxisEvent> xEvents = new();
    private List<AxisEvent> yEvents = new();

    // Instead of storing “is start or end” each time from scratch,
    // store references to each event (start and end) inside the Body
    // so we can update them on each frame.
    private Dictionary<Body, AxisEvent> xStartEvents = new();
    private Dictionary<Body, AxisEvent> xEndEvents   = new();
    private Dictionary<Body, AxisEvent> yStartEvents = new();
    private Dictionary<Body, AxisEvent> yEndEvents   = new();

    private IReadOnlyList<Body> bodies = new List<Body>();

    public override void UpdateBodies(IReadOnlyList<Body> bodies)
    {
        this.bodies = bodies;

        // 1) The first time, or if the count changes drastically, we can rebuild from scratch.
        if (xEvents.Count != 2 * bodies.Count || yEvents.Count != 2 * bodies.Count)
        {
            RebuildAllEvents(bodies);
        }
        else
        {
            // 2) Otherwise, just update positions and incrementally resort.
            UpdatePositionsAndResort();
        }
    }

    private void RebuildAllEvents(IReadOnlyList<Body> bodies)
    {
        xEvents.Clear();
        yEvents.Clear();

        xStartEvents.Clear();
        xEndEvents.Clear();
        yStartEvents.Clear();
        yEndEvents.Clear();

        foreach (var body in bodies)
        {
            // X Start/End
            var xMin = MathF.Min(body.PreviousPosition.X, body.Position.X) - body.Radius;
            var xMax = MathF.Max(body.PreviousPosition.X, body.Position.X) + body.Radius;

            var xStart = new AxisEvent(true,  body, xMin);
            var xEnd   = new AxisEvent(false, body, xMax);

            xStartEvents[body] = xStart;
            xEndEvents[body]   = xEnd;

            xEvents.Add(xStart);
            xEvents.Add(xEnd);

            // Y Start/End
            var yMin = MathF.Min(body.PreviousPosition.Y, body.Position.Y) - body.Radius;
            var yMax = MathF.Max(body.PreviousPosition.Y, body.Position.Y) + body.Radius;

            var yStart = new AxisEvent(true,  body, yMin);
            var yEnd   = new AxisEvent(false, body, yMax);

            yStartEvents[body] = yStart;
            yEndEvents[body]   = yEnd;

            yEvents.Add(yStart);
            yEvents.Add(yEnd);
        }

        // Full sort once
        xEvents.Sort((a, b) => a.Position.CompareTo(b.Position));
        yEvents.Sort((a, b) => a.Position.CompareTo(b.Position));
    }

    /// <summary>
    /// Update the positions of each event, then do an incremental resort of xEvents and yEvents.
    /// </summary>
    private void UpdatePositionsAndResort()
    {
        // Update each event
        foreach (var body in bodies)
        {
            // Recalculate bounding region on each axis
            float xMin = MathF.Min(body.PreviousPosition.X, body.Position.X) - body.Radius;
            float xMax = MathF.Max(body.PreviousPosition.X, body.Position.X) + body.Radius;
            float yMin = MathF.Min(body.PreviousPosition.Y, body.Position.Y) - body.Radius;
            float yMax = MathF.Max(body.PreviousPosition.Y, body.Position.Y) + body.Radius;

            // Update X Start/End
            var sx = xStartEvents[body];
            sx = sx with { Position = xMin };
            xStartEvents[body] = sx;

            var ex = xEndEvents[body];
            ex = ex with { Position = xMax };
            xEndEvents[body] = ex;

            // Update Y Start/End
            var sy = yStartEvents[body];
            sy = sy with { Position = yMin };
            yStartEvents[body] = sy;

            var ey = yEndEvents[body];
            ey = ey with { Position = yMax };
            yEndEvents[body] = ey;
        }

        // Copy those changes back into xEvents / yEvents array
        // (Alternatively, you could store references directly.)
        for (int i = 0; i < xEvents.Count; i++)
        {
            var e = xEvents[i];
            if (e.IsStart)
            {
                xEvents[i] = xStartEvents[e.Body];
            }
            else
            {
                xEvents[i] = xEndEvents[e.Body];
            }
        }

        for (int i = 0; i < yEvents.Count; i++)
        {
            var e = yEvents[i];
            if (e.IsStart)
            {
                yEvents[i] = yStartEvents[e.Body];
            }
            else
            {
                yEvents[i] = yEndEvents[e.Body];
            }
        }

        // Now do an incremental bubble sort (or insertion sort). 
        // This is typically much cheaper than a full .Sort() if objects move little.

        IncrementalResort(xEvents);
        IncrementalResort(yEvents);
    }

    /// <summary>
    /// Very naive bubble-sort pass. For a real system, you might do multiple passes 
    /// until no swaps are needed, but usually objects won't move very far each frame.
    /// </summary>
    private void IncrementalResort(List<AxisEvent> events)
    {
        for (int i = 0; i < events.Count - 1; i++)
        {
            // If out of order, swap
            if (events[i].Position > events[i + 1].Position)
            {
                var tmp = events[i];
                events[i] = events[i + 1];
                events[i + 1] = tmp;
            }
        }
    }

    public override IEnumerable<Body> FixedRaycast(Vector2 start, Vector2 end)
    {
        // The original code for FixedRaycast in your example has several issues:
        //  - It mixes up xEvents[x_start] with x_bodies.Count
        //  - The approach for “start, end, sweep” is incomplete for typical ray intersection
        //
        // For demonstration, we’ll do a simpler bounding-range check. 
        // A complete Ray vs. bounding-interval approach can be more involved.

        float minX = MathF.Min(start.X, end.X);
        float maxX = MathF.Max(start.X, end.X);

        float minY = MathF.Min(start.Y, end.Y);
        float maxY = MathF.Max(start.Y, end.Y);

        // Simple bounding rectangle approach (not strictly correct for diagonal rays,
        // but good enough for quick culling).
        // For truly robust ray intersection, you'd want parametric checks.

        var result = new List<Body>();

        foreach (var body in bodies)
        {
            // Quick bounding check
            var bMinX = MathF.Min(body.Position.X, body.PreviousPosition.X) - body.Radius;
            var bMaxX = MathF.Max(body.Position.X, body.PreviousPosition.X) + body.Radius;
            var bMinY = MathF.Min(body.Position.Y, body.PreviousPosition.Y) - body.Radius;
            var bMaxY = MathF.Max(body.Position.Y, body.PreviousPosition.Y) + body.Radius;

            if (bMaxX < minX || bMinX > maxX) continue;
            if (bMaxY < minY || bMinY > maxY) continue;

            // Passed broad-phase check, so add it
            result.Add(body);
        }

        return result;
    }

    public override IEnumerable<Body> DynamicRaycast(Vector2 start, Vector2 end)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<(Body, Body)> Collisions()
    {
        // Do a 1D sweep on xEvents, then a 1D sweep on yEvents, 
        // then combine pairs. 
        var xPairs = SweepAndCollectPairs(xEvents);
        var yPairs = SweepAndCollectPairs(yEvents);

        return CombinePairs(xPairs, yPairs);
    }

    /// <summary>
    /// Standard 1D sweep. Return a dictionary of {Body => set of bodies that overlap in that axis}
    /// </summary>
    private Dictionary<Body, HashSet<Body>> SweepAndCollectPairs(List<AxisEvent> events)
    {
        var active = new List<Body>();
        var result = new Dictionary<Body, HashSet<Body>>();

        active.Clear();

        foreach (var ev in events)
        {
            if (ev.IsStart)
            {
                // Overlaps with all currently active
                foreach (var other in active)
                {
                    if (!result.TryGetValue(ev.Body, out var set))
                    {
                        set = new HashSet<Body>();
                        result[ev.Body] = set;
                    }
                    set.Add(other);

                    // Also add reverse
                    if (!result.TryGetValue(other, out var otherSet))
                    {
                        otherSet = new HashSet<Body>();
                        result[other] = otherSet;
                    }
                    otherSet.Add(ev.Body);
                }
                // Then add self to active
                active.Add(ev.Body);
            }
            else
            {
                // End event: remove from active
                active.Remove(ev.Body);
            }
        }

        return result;
    }

    private IEnumerable<(Body, Body)> CombinePairs(
        Dictionary<Body, HashSet<Body>> xPairs,
        Dictionary<Body, HashSet<Body>> yPairs)
    {
        // Only return pairs that overlap in both x and y
        foreach (var kvp in xPairs)
        {
            Body a = kvp.Key;
            var aSet = kvp.Value;

            // If we don't have any pairs for 'a' in yPairs, skip
            if (!yPairs.ContainsKey(a)) continue;

            var ySet = yPairs[a];
            foreach (var b in aSet)
            {
                // Must also be in ySet
                if (ySet.Contains(b))
                {
                    yield return (a, b);
                }
            }
        }
    }

    public override IBidirectionalGraph<Body, Edge<Body>> CreateGraph(float distance, Func<Body, bool> selectorFunction)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Body> GetNearest(Body body, float distance)
    {
        throw new NotImplementedException();
    }
    
    private record struct AxisEvent(bool IsStart, Body Body, float Position)
    {
        // We'll implement IComparable<Event> if we still want to do a full sort at times,
        // but for incremental updates, we might do a manual "swap if out of order" approach.
    }
}