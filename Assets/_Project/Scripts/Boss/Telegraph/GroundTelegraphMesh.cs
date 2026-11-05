using UnityEngine;

namespace AntColony.Boss.Telegraph
{
    internal static class GroundTelegraphMesh
    {
        internal static int[] GridTriangles(int rows, int columns)
        {
            var triangles = new int[rows * columns * 6];
            var stride = columns + 1;
            var t = 0;
            for (var row = 0; row < rows; row++)
                for (var column = 0; column < columns; column++)
                {
                    var a = row * stride + column;
                    var b = a + stride;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                }
            return triangles;
        }

        internal static Vector3 SampleGround(Vector3 position, float castHeight, LayerMask groundMask, float surfaceOffset)
        {
            if (Physics.Raycast(position + Vector3.up * castHeight, Vector3.down, out var hit,
                castHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point + hit.normal * surfaceOffset;
            return position + Vector3.up * surfaceOffset;
        }
    }
}
