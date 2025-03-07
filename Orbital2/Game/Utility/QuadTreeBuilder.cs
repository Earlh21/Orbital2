using System;
using System.Linq;
using Orbital2.Engine;
using Orbital2.Physics.Gravity;

namespace Orbital2.Game.Utility;

internal class QuadTreeBuilder<T> : GameObject where T : PhysicalGameObject
{
    public QuadTree Tree
    {
        get
        {
            if (tree == null)
            {
                tree = BuildQuadTree();
            }

            return tree;
        }
    }

    private QuadTree? tree;
    private GameWorld? world;

    private QuadTree BuildQuadTree()
    {
        if (world == null)
        {
            throw new InvalidOperationException("Can't create quad tree before first physics timestep");
        }

        return new QuadTree(world.FindObjectsByType<T>().Select(x => x.Body));
    }

    public override void OnStart(EventContext context)
    {
        world = context.World;
    }

    public override void PrePhysicsUpdate(float physicsTimestep, EventContext context)
    {
        tree = null;
    }
}