using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace WeaponsMath
{
    [BurstCompile] public sealed class WeaponMassDistributionCalculator
    {
        /// <summary>
        ///     Computes the vertex masses for a given mesh.
        /// </summary>
        /// <param name="mesh">Mesh to compute vertex masses for.</param>
        /// <param name="vertexMasses">Resulting vertex masses.</param>
        /// <param name="allocator">Allocator to use for allocation of vertexMasses.</param>
        [BurstDiscard]
        public static void ComputeVertexMasses([NotNull] Mesh mesh, out NativeArray<float> vertexMasses, Allocator allocator = Allocator.TempJob)
        {
            // Create arrays
            NativeArray<Vector3> vertices = new(mesh.vertices, Allocator.TempJob);
            NativeArray<int> triangles = new(mesh.triangles, Allocator.TempJob);
            ComputeVertexMasses(vertices, triangles, out vertexMasses, allocator);

            // Dispose arrays
            vertices.Dispose();
            triangles.Dispose();
        }
        
        /// <summary>
        ///     Computes the vertex masses for a given mesh data.
        /// </summary>
        /// <param name="vertices">Vertices of the mesh.</param>
        /// <param name="triangles">Triangles of the mesh.</param>
        /// <param name="vertexMasses">Resulting vertex masses.</param>
        /// <param name="allocator">Allocator to use for allocation of vertexMasses.</param>
        [BurstCompile]
        public static void ComputeVertexMasses(
            in NativeArray<Vector3> vertices,
            in NativeArray<int> triangles,
            out NativeArray<float> vertexMasses,
            Allocator allocator = Allocator.TempJob)
        {
            // Allocate vertex masses
            vertexMasses = new NativeArray<float>(vertices.Length, allocator);

            // Compute vertex masses
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float3 a = vertices[triangles[i]];
                float3 b = vertices[triangles[i + 1]];
                float3 c = vertices[triangles[i + 2]];

                float area = math.length(math.cross(b - a, c - a)) * 0.5f;
                vertexMasses[triangles[i]] += area;
                vertexMasses[triangles[i + 1]] += area;
                vertexMasses[triangles[i + 2]] += area;
            }

            // Normalize vertex masses
            float sum = 0;
            foreach (float weight in vertexMasses) sum += weight;
            for (int i = 0; i < vertexMasses.Length; i++) vertexMasses[i] /= sum;
        }
    }
}