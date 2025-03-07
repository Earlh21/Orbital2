using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Engine;

public class Input
{
    private HashSet<Keys> previouslyDown = [];
    private HashSet<Keys> justPressed = [];
    private HashSet<Keys> justReleased = [];

    public bool IsKeyPressed(Keys key) => previouslyDown.Contains(key);
    public bool IsKeyJustPressed(Keys key) => justPressed.Contains(key);
    public bool IsKeyJustReleased(Keys key) => justReleased.Contains(key);

    public void Update()
    {
        justPressed.Clear();
        justReleased.Clear();

        foreach(var key in Enum.GetValues<Keys>())
        {
            if (Keyboard.GetState().IsKeyDown(key))
            {
                if (!previouslyDown.Contains(key))
                {
                    justPressed.Add(key);
                }

                previouslyDown.Add(key);
            }
            else
            {
                if (previouslyDown.Contains(key))
                {
                    justReleased.Add(key);
                }

                previouslyDown.Remove(key);
            }
        }
    }
}