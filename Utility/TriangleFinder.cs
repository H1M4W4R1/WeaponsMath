using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace WeaponsMath.Utility
{
    [BurstCompile]
    public static class TriangleFinder
    {
        /// <summary>
        ///     Gets the nearest triangle to the given point.
        /// </summary>
        /// <returns>Triangle index (mult by 3 when handling triangles with vertices)</returns>
        [BurstCompile]
        public static int GetNearestTriangle(
            in NativeArray<Vector3> vertices,
            in NativeArray<int> triangles,
            in Vector3 point)
        {
            int nearestTri = -1;
            float minDist = float.MaxValue;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];

                float dist = PointTriangleDistance(point, a, b, c);
                if (dist > minDist) continue;
                minDist = dist;
                nearestTri = i / 3;
            }

            return nearestTri;
        }

        
        [BurstCompile]
        private static float PointTriangleDistance(in float3 p, in float3 a, in float3 b, in float3 c)
        {
            // Edge vectors
            float3 ab = b - a;
            float3 ac = c - a;
            float3 ap = p - a;

            float d1 = math.dot(ab, ap);
            float d2 = math.dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return math.distancesq(p, a);

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return math.distancesq(p, b);

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                float v = d1 / (d1 - d3);
                return math.distancesq(p , a + v * ab);
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return math.distancesq(p, c);

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float w = d2 / (d2 - d6);
                return math.distancesq(p, a + w * ac);
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return math.distancesq(p, b + w * (c - b));
            }

            // Inside face
            float3 n = math.normalize(math.cross(ab, ac));
            return math.pow(math.dot(ap, n), 2);
        }
    }
}