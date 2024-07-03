using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Collision
{
    public class SweepAndPrune : BroadPhase
    {
        public override List<Tuple<Body, Body>> FindPotentialCollisions(IReadOnlyList<Body> bodies)
        {
            var x_events = new List<Event>();
            var y_events = new List<Event>();

            foreach (var body in bodies)
            {
                float start = body.PreviousPosition.X - body.Radius;
                float end = body.Position.X + body.Radius;

                x_events.Add(new(true, body, MathF.Min(start, end)));
                x_events.Add(new(false, body, MathF.Max(start, end)));

                start = body.PreviousPosition.Y - body.Radius;
                end = body.Position.Y + body.Radius;

                y_events.Add(new(true, body, MathF.Min(start, end)));
                y_events.Add(new(false, body, MathF.Max(start, end)));
            }

            x_events.Sort((a, b) => MathF.Sign(a.Position - b.Position));
            y_events.Sort((a, b) => MathF.Sign(a.Position - b.Position));

            var x_paired = FindPairs(bodies, x_events);
            var y_paired = FindPairs(bodies, y_events);

            var potential_pairs = new List<Tuple<Body, Body>>();

            foreach (var x in x_paired.Keys)
            {
                foreach (var pair in x_paired[x])
                {
                    if(y_paired.ContainsKey(x))
                    {
                        if (y_paired[x].Contains(pair))
                        {
                            potential_pairs.Add(new(x, pair));
                            potential_pairs.Add(new(pair, x));
                        }
                    }

                    if (y_paired.ContainsKey(pair))
                    {
                        if (y_paired[pair].Contains(x))
                        {
                            potential_pairs.Add(new(x, pair));
                            potential_pairs.Add(new(pair, x));
                        }
                    }
                }
            }

            return potential_pairs;
        }

        private Dictionary<Body, HashSet<Body>> FindPairs(IReadOnlyList<Body> bodies, IEnumerable<Event> events)
        {
            var active = new HashSet<Body>();

            var paired = new Dictionary<Body, HashSet<Body>>();

            active.Clear();

            foreach (var ev in events)
            {
                if (ev.IsStart)
                {
                    foreach (var body in active)
                    {
                        if (body == ev.Body) continue;

                        if (!paired.ContainsKey(ev.Body)) paired[ev.Body] = [];

                        paired[ev.Body].Add(body);
                    }

                    active.Add(ev.Body);
                }
                else
                {
                    active.Remove(ev.Body);
                }
            }

            return paired;
        }

        private record struct Event(bool IsStart, Body Body, float Position);
    }
}
