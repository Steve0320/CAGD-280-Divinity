using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// Helper functions for drawing shapes with Unity's Debug lines. These are mainly used to visualize grids and pathing effects
/// during debugging.
/// </summary>
public class DebugShapes
{

    public static void DrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color c, float t)
    {
        Debug.DrawLine(p1, p2, c, t);
        Debug.DrawLine(p2, p3, c, t);
        Debug.DrawLine(p3, p1, c, t);
    }
    
    /// <summary>
    /// Draw shaded triangle
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="c"></param>
    public static void DrawShadedTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color c, float t, int divisions = 20)
    {

        
        // First, draw the triangle bounds
        DrawTriangle(p1, p2, p3, c, t);

        for (int i = 0; i < divisions; i++)
        {

            float frac = (float) i / divisions;
            Vector3 p12 = p1 + (p2 - p1) * frac;
            Vector3 p13 = p1 + (p3 - p1) * frac;

            Debug.DrawLine(p12, p13, c, t);

        }


    }

    public static void DrawWall(Vector3 start, Vector3 end, float height, Color c, float t, int divisions = 20)
    {

        Debug.DrawRay(start, Vector3.up * height, c, t);
        Debug.DrawRay(end, Vector3.up * height, c, t);

        for (int i = 0; i < divisions; i++)
        {

            float frac = (float) i / divisions;
            Vector3 a = start + frac * height * Vector3.up;
            Vector3 b = end + frac * height * Vector3.up;
            Debug.DrawLine(a, b, c, t);

        }

    }

    /// <summary>
    /// Draw an n-gon with the given radius (center-to-corner).
    /// </summary>
    /// <param name="center"></param>
    /// <param name="width"></param>
    public static void DrawRegularPoly(Vector3 center, int sides, float radius, Color c, float t)
    {

        float degrees = 360.0f / sides;

        // We'll rotate this vector to get the offset for each point
        Vector3 masterRadius = new(0, 0, radius);

        Vector3 lastPoint = center + masterRadius;

        for (int i = 1; i <= sides; i++)
        {
            Vector3 curPoint = center + Quaternion.AngleAxis(i * degrees, Vector3.up) * masterRadius;
            Debug.DrawLine(lastPoint, curPoint, c, t);
            lastPoint = curPoint;
        }

    }

}
