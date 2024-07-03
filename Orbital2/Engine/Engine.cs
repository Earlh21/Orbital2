using Orbital2.Physics;
using Orbital2.Physics.Collision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Engine
{
    public class Engine
    {
        public float PhysicsTimestep
        {
            get => physics_timestep;
            set
            {
                if (physics_timestep <= 0)
                {
                    throw new ArgumentException("Physics timestep must be greater than zero.");
                }

                accumulator = accumulator / physics_timestep * value;
                physics_timestep = value;
            }
        }

        public IReadOnlyList<GameObject> GameObjects => game_objects.AsReadOnly();
        public IReadOnlyList<PhysicalGameObject> PhysicalObjects => physical_objects.AsReadOnly();

        private readonly List<GameObject> game_objects = [];
        private readonly List<PhysicalGameObject> physical_objects = [];

        private readonly Dictionary<Body, PhysicalGameObject> body_lookup = [];

        private readonly Dictionary<string, List<GameObject>> name_lookup = [];
        private readonly Dictionary<GameObject, string> object_names = [];

        private readonly HashSet<GameObject> marked_for_removal = [];

        private List<Tuple<float, PhysicalGameObject, PhysicalGameObject>> collisions = [];

        private readonly World world = new();

        private float accumulator = 0;
        private float physics_timestep = 0.2f;

        public Engine(float physics_timestep = 0.2f)
        {
            PhysicsTimestep = physics_timestep;
            accumulator = PhysicsTimestep;
        }

        public void Update(float timestep)
        {
            UpdatePhysics(timestep);

            float t = accumulator / physics_timestep;
            world.InterpolateLinear(t);

            foreach (var game_object in game_objects)
            {
                game_object.FrameUpdate(timestep, this);
            }

            foreach(var obj in marked_for_removal)
            {
                RemoveObjectHelper(obj);
            }
            marked_for_removal.Clear();

            TriggerWaitingCollisions(t);
        }

        private void UpdatePhysics(float timestep)
        {
            accumulator += timestep;

            while (accumulator > physics_timestep)
            {
                foreach (var game_object in game_objects)
                {
                    game_object.PrePhysicsUpdate(timestep, this);
                }

                accumulator -= physics_timestep;
                world.Step(physics_timestep);

                foreach (var game_object in game_objects)
                {
                    game_object.PostPhysicsUpdate(timestep, this);
                }
            }

            FindAndTriggerCollisions();
        }

        private void TriggerWaitingCollisions(float t)
        {
            while (collisions.Count > 0)
            {
                if (t < collisions.First().Item1) break;

                collisions.First().Item2.OnCollisionPassed(collisions.First().Item2, t, this);

                collisions.RemoveAt(0);
            }
        }

        private void FindAndTriggerCollisions()
        {
            collisions.Clear();

            var broad_phase = new SweepAndPrune();
            var potential_collisions = broad_phase.FindPotentialCollisions(world.Bodies);

            foreach(var collision in potential_collisions)
            {
                var goa = body_lookup[collision.Item1];
                var gob = body_lookup[collision.Item2];

                float? collision_t = collision.Item1.GetCollisionT(collision.Item2);

                if (collision_t == null) continue;

                goa.OnCollisionFound(gob, collision_t.Value, this);

                collisions.Add(new(collision_t.Value, goa, gob));
            }

            collisions.Sort((col_a, col_b) => MathF.Sign(col_a.Item1 - col_b.Item1));
        }

        public void AddObject(GameObject game_object, string? name = null)
        {
            game_objects.Add(game_object);

            if (game_object is PhysicalGameObject physical_object)
            {
                physical_objects.Add(physical_object);
                world.AddBody(physical_object.Body);
                body_lookup[physical_object.Body] = physical_object;
            }

            if (name != null)
            {
                AddName(game_object, name);
            }
        }

        public void RemoveObject(GameObject game_object)
        {
            marked_for_removal.Add(game_object);
        }

        private void RemoveObjectHelper(GameObject game_object)
        {
            game_objects.Remove(game_object);

            if (game_object is PhysicalGameObject physical_object)
            {
                physical_objects.Remove(physical_object);
                world.RemoveBody(physical_object.Body);
                body_lookup.Remove(physical_object.Body);
            }

            RemoveName(game_object);
        }
        
        private void AddName(GameObject game_object, string name)
        {
            if (!name_lookup.ContainsKey(name))
            {
                name_lookup[name] = [];
            }

            name_lookup[name].Add(game_object);
            object_names[game_object] = name;
        }

        private void RemoveName(GameObject game_object)
        {
            string? name = object_names.GetValueOrDefault(game_object);
            if (name != null)
            {
                name_lookup[name].Remove(game_object);

                if (name_lookup[name].Count == 0)
                {
                    name_lookup.Remove(name);
                }
            }

            object_names.Remove(game_object);
        }

        public void SetName(GameObject game_object, string? name)
        {
            RemoveName(game_object);

            if (name != null)
            {
                AddName(game_object, name);
            }
        }

        public IReadOnlyList<GameObject> FindObjectsByName(string name)
        {
            return name_lookup.GetValueOrDefault(name, []).AsReadOnly();
        }

        public GameObject? FindFirstObjectByName(string name)
        {
            return FindObjectsByName(name).FirstOrDefault();
        }

        public void ClearObjects()
        {
            game_objects.Clear();
            physical_objects.Clear();
            world.Clear();
            name_lookup.Clear();
            object_names.Clear();
        }
    }
}
