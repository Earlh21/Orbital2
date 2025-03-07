using Orbital2.Physics;
using Orbital2.Physics.Collision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Engine;

public class GameWorld
{
    public World PhysicsWorld { get; } = new(new SpatialHashing(40));

    public BroadPhase BroadPhase
    {
        get => PhysicsWorld.BroadPhase;
        set => PhysicsWorld.BroadPhase = value;
    }

    public IReadOnlyList<GameObject> GameObjects => gameObjects.AsReadOnly();
    public IReadOnlyList<PhysicalGameObject> PhysicalObjects => physicalObjects.AsReadOnly();

    private readonly List<GameObject> gameObjects = [];
    private readonly List<GameObject> nonPhysicalObjects = [];
    private readonly List<PhysicalGameObject> physicalObjects = [];

    private readonly Dictionary<Body, PhysicalGameObject> bodyLookup = [];

    private readonly Dictionary<string, List<GameObject>> nameLookup = [];
    private readonly Dictionary<GameObject, string> objectNames = [];

    private readonly Queue<GameObjectChangeEvent> changeEvents = [];

    public void AddObject(GameObject gameObject, string? name = null)
    {
        changeEvents.Enqueue(new(gameObject, false, name));
    }
    
    public void AddObjects(IEnumerable<GameObject> gameObjects)
    {
        foreach(var gameObject in gameObjects)
        {
            AddObject(gameObject);
        }
    }

    public void RemoveObject(GameObject gameObject)
    {
        changeEvents.Enqueue(new(gameObject, true, null));
    }

    public void ClearObjects()
    {
        gameObjects.Clear();
        physicalObjects.Clear();
        PhysicsWorld.Clear();
        nameLookup.Clear();
        objectNames.Clear();
        changeEvents.Clear();
    }

    internal List<GameObjectChangeEvent> ProcessGameObjectQueue()
    {
        List<GameObjectChangeEvent> processedEvents = [];

        while (changeEvents.Count > 0)
        {
            processedEvents.Add(changeEvents.Dequeue());
        }

        foreach(var ev in processedEvents)
        {
            if(ev.Remove)
            {
                RemoveObjectHelper(ev.GameObject);
            }
            else
            {
                AddObjectHelper(ev.GameObject, ev.Name);
            }
        }

        return processedEvents;
    }

    public void SetName(GameObject gameObject, string? name)
    {
        RemoveName(gameObject);

        if (name != null)
        {
            AddName(gameObject, name);
        }
    }

    public IReadOnlyList<GameObject> FindObjectsByName(string name)
    {
        return nameLookup.GetValueOrDefault(name, []).AsReadOnly();
    }

    public IEnumerable<T> FindObjectsByType<T>() where T : GameObject
    {
        if(typeof(T).IsSubclassOf(typeof(PhysicalGameObject)))
        {
            return physicalObjects.Where(x => x is T).Select(x => (T)(GameObject)x);
        }
        else
        {
            return nonPhysicalObjects.Where(x => x is T).Select(x => (T)x);
        }
    }

    public GameObject? FindFirstObjectByName(string name)
    {
        return FindObjectsByName(name).FirstOrDefault();
    }

    public T? FindFirstObjectByType<T>() where T : GameObject
    {
        return FindObjectsByType<T>().FirstOrDefault();
    }

    private void AddObjectHelper(GameObject gameObject, string? name)
    {
        gameObjects.Add(gameObject);

        if (gameObject is PhysicalGameObject physicalObject)
        {
            physicalObjects.Add(physicalObject);
            PhysicsWorld.AddBody(physicalObject.Body);
            bodyLookup[physicalObject.Body] = physicalObject;
        }
        else
        {
            nonPhysicalObjects.Add(gameObject);
        }

        if (name != null)
        {
            AddName(gameObject, name);
        }
    }

    private void RemoveObjectHelper(GameObject gameObject)
    {
        gameObjects.Remove(gameObject);

        if (gameObject is PhysicalGameObject physicalObject)
        {
            physicalObjects.Remove(physicalObject);
            PhysicsWorld.RemoveBody(physicalObject.Body);
            bodyLookup.Remove(physicalObject.Body);
        }
        else
        {
            nonPhysicalObjects.Remove(gameObject);
        }

        RemoveName(gameObject);
    }

    private void AddName(GameObject gameObject, string name)
    {
        if (!nameLookup.ContainsKey(name))
        {
            nameLookup[name] = [];
        }

        nameLookup[name].Add(gameObject);
        objectNames[gameObject] = name;
    }

    private void RemoveName(GameObject gameObject)
    {
        string? name = objectNames.GetValueOrDefault(gameObject);
        if (name != null)
        {
            nameLookup[name].Remove(gameObject);

            if (nameLookup[name].Count == 0)
            {
                nameLookup.Remove(name);
            }
        }

        objectNames.Remove(gameObject);
    }

    public PhysicalGameObject? GetGameObjectByBody(Body body)
    {
        return bodyLookup.GetValueOrDefault(body);
    }

    internal readonly record struct GameObjectChangeEvent(GameObject GameObject, bool Remove, string? Name);
}