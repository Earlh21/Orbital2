﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Gravity;

public abstract class GravitySolver
{
    public float GravitationalConstant { get; set; } = 1;

    public abstract Vector2[] ComputeAccelerations(IReadOnlyList<Body> bodies);

    public Vector2 ComputeAccelerationByMass(Vector2 bodyPosition, Vector2 attractorPosition, float mass)
    {
        Vector2 disp = attractorPosition - bodyPosition;
        return GravitationalConstant * mass * disp / disp.LengthSquared();
    }
}