using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using WeaponsMath.Jobs;

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
        [BurstDiscard] public static void ComputeVertexMasses(
            [NotNull] Mesh mesh,
            out NativeArray<float> vertexMasses,
            Allocator allocator = Allocator.TempJob)
        {
            Assert.IsNotNull(mesh, "Mesh must not be null");         
            ComputeVertexMasses(mesh.vertices, mesh.triangles, out vertexMasses, allocator);
        }

        [BurstDiscard] public static void ComputeVertexMasses(
            [NotNull] Vector3[] vertices,
            [NotNull] int[] triangles,
            out NativeArray<float> vertexMasses,
            Allocator allocator = Allocator.TempJob)
        {
            Assert.IsNotNull(vertices, "Vertices must not be null");
            Assert.IsNotNull(triangles, "Triangles must not be null");
             
            // Create arrays
            NativeArray<Vector3> nativeVertices = new(vertices, Allocator.TempJob);
            NativeArray<int> nativeTriangles = new(triangles, Allocator.TempJob);
            
            // Perform computation
            ComputeVertexMasses(nativeVertices, nativeTriangles, out vertexMasses, allocator);
            
            // Dispose arrays
            nativeVertices.Dispose();
            nativeTriangles.Dispose();
        }

        /// <summary>
        ///     Computes the vertex masses for a given mesh data.
        /// </summary>
        /// <param name="vertices">Vertices of the mesh.</param>
        /// <param name="triangles">Triangles of the mesh.</param>
        /// <param name="vertexMasses">Resulting vertex masses.</param>
        /// <param name="allocator">Allocator to use for allocation of vertexMasses.</param>
        [BurstCompile] public static void ComputeVertexMasses(
            in NativeArray<Vector3> vertices,
            in NativeArray<int> triangles,
            out NativeArray<float> vertexMasses,
            Allocator allocator = Allocator.TempJob)
        {
            Assert.IsTrue(vertices.IsCreated, "Vertices must be created");
            Assert.IsTrue(triangles.IsCreated, "Triangles must be created");
            
            // Allocate vertex masses
            vertexMasses = new NativeArray<float>(vertices.Length, allocator);

            // Perform computations
            ComputeWeaponVertexMassJob job = new()
            {
                vertices = vertices,
                triangles = triangles,
                vertexMasses = vertexMasses
            };
            JobHandle handle = job.Schedule(triangles.Length / 3, 64);
            handle.Complete();

            // Compute total mass
            float sum = 0;
            for (int i = 0; i < vertexMasses.Length; i++)
            {
                sum += vertexMasses[i];
            }

            // Normalize vertex masses
            for (int i = 0; i < vertexMasses.Length; i++) vertexMasses[i] /= sum;
        }
    }
}