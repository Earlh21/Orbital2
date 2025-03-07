using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2;

public class Gradient
{
    private readonly IReadOnlyList<Tuple<float, Color>> colorStops;

    public Gradient(IReadOnlyList<Tuple<float, Color>> colorStops)
    {
        if (colorStops == null || colorStops.Count < 2)
        {
            throw new ArgumentException("At least two color stops are required.");
        }

        var sortedStops = colorStops.OrderBy(x => x.Item1).ToList();

        if (sortedStops.Select(x => x.Item1).Distinct().Count() != sortedStops.Count)
        {
            throw new ArgumentException("Duplicate float values are not allowed in color stops.");
        }

        this.colorStops = sortedStops;
    }

    public Color GetColor(float t)
    {
        if (t <= colorStops[0].Item1)
            return colorStops[0].Item2;

        if (t >= colorStops[colorStops.Count - 1].Item1)
            return colorStops[colorStops.Count - 1].Item2;

        for (int i = 0; i < colorStops.Count - 1; i++)
        {
            if (t >= colorStops[i].Item1 && t < colorStops[i + 1].Item1)
            {
                return InterpolateColor(colorStops[i].Item1, colorStops[i].Item2,
                    colorStops[i + 1].Item1, colorStops[i + 1].Item2,
                    t);
            }
        }

        // This line should never be reached due to the checks at the beginning of the method,
        // but we return the last color to ensure the method always returns a value.
        return colorStops[colorStops.Count - 1].Item2;
    }

    private Color InterpolateColor(float t1, Color color1, float t2, Color color2, float t)
    {
        float factor = (t - t1) / (t2 - t1);
        return new Color(
            (int)Lerp(color1.R, color2.R, factor),
            (int)Lerp(color1.G, color2.G, factor),
            (int)Lerp(color1.B, color2.B, factor),
            (int)Lerp(color1.A, color2.A, factor)
        );
    }

    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}