using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using WeaponsMath.Data;
using WeaponsMath.Enums;
using WeaponsMath.Jobs;

namespace WeaponsMath.Utility
{
    [BurstCompile] public static class WeaponEdgeClassifier
    {
        // Keeps adjacency per mesh instance as flattened arrays (start indices + neighbor list)
        private static readonly Dictionary<int, WeaponMeshAdjacencyDataFlattened> adjacencyCache = new();

        /// <summary>
        ///     Classifies all vertices of a mesh.
        /// </summary>
        [BurstDiscard] public static void ClassifyAllVertices(
            [NotNull] Mesh mesh,
            WeaponEdgeClassifierParams p,
            out NativeArray<WeaponEdgeType> edges,
            Allocator allocator = Allocator.TempJob)
        {
            Assert.IsNotNull(mesh, "Mesh must not be null");
            Assert.IsTrue(mesh.vertexCount > 0, "Mesh must have at least one vertex");

            // Build or get adjacency flattened arrays
            WeaponMeshAdjacencyDataFlattened data = GetOrBuildAdjacencyFlattened(mesh);

            // Get & copy vertices and normals
            Vector3[] verticesArray = mesh.vertices;
            Vector3[] normalsArray = mesh.normals;

            // Classify vertices
            ClassifyAllVertices(verticesArray, normalsArray, p, data, out edges, allocator);
        }

        /// <summary>
        ///     Classifies all vertices from provided mesh data.
        /// </summary>
        public static void ClassifyAllVertices(
            [NotNull] Vector3[] verticesArray,
            [NotNull] Vector3[] normalsArray,
            WeaponEdgeClassifierParams p,
            WeaponMeshAdjacencyDataFlattened data,
            out NativeArray<WeaponEdgeType> edges,
            Allocator allocator = Allocator.TempJob)
        {
            Assert.IsNotNull(verticesArray, "Vertices array must not be null");
            Assert.IsNotNull(normalsArray, "Normals array must not be null");
            Assert.IsTrue(verticesArray.Length == normalsArray.Length,
                "Vertices and normals arrays must have the same length");
            Assert.IsTrue(verticesArray.Length > 0, "Vertices array must not be empty");
            Assert.IsTrue(data.neighbors.IsCreated && data.neighborStarts.IsCreated,
                "Adjacency data must be created");

            // Create native arrays for vertices and normals
            NativeArray<Vector3> vertices = new(verticesArray, Allocator.TempJob);
            NativeArray<Vector3> normals = new(normalsArray, Allocator.TempJob);

            // Base classification entry point
            ClassifyAllVertices(vertices, normals, p, data, out edges, out JobHandle handle, allocator);
            handle.Complete();

            // Dispose native arrays
            vertices.Dispose();
            normals.Dispose();
        }

        /// <summary>
        ///     Classifies all vertices from provided mesh data.
        /// </summary>
        [BurstCompile] public static void ClassifyAllVertices(
            in NativeArray<Vector3> vertices,
            in NativeArray<Vector3> normals,
            in WeaponEdgeClassifierParams p,
            in WeaponMeshAdjacencyDataFlattened weaponAdjacencyData,
            out NativeArray<WeaponEdgeType> edges,
            out JobHandle handle,
            Allocator allocator = Allocator.TempJob)
        {
            Assert.IsTrue(vertices.IsCreated && normals.IsCreated, "Vertices and normals must be created");
            Assert.IsTrue(weaponAdjacencyData.neighbors.IsCreated && weaponAdjacencyData.neighborStarts.IsCreated,
                "Adjacency data must be created");
            Assert.IsTrue(vertices.Length == normals.Length, "Vertices and normals must have the same length");
            Assert.IsTrue(vertices.Length > 0, "Vertices must not be empty");
            
            edges = new NativeArray<WeaponEdgeType>(vertices.Length, allocator);

            // Schedule job to classify vertices
            ClassifyWeaponVerticesJob job = new()
            {
                vertices = vertices,
                normals = normals,
                neighborStarts = weaponAdjacencyData.neighborStarts,
                neighbors = weaponAdjacencyData.neighbors,
                // copy params
                depth = p.depth,
                maxNeighbors = p.maxNeighbors,
                splitLow = p.splitLow,
                splitHigh = p.splitHigh,
                depthDecay = p.depthDecay,
                distanceWeightPower = p.distanceWeightPower,
                minSqrDistance = p.minSqrDistance,
                maxCollected = p.maxCollected,

                outEdges = edges,
            };

            handle = job.Schedule(vertices.Length, 64); // batch size 64
        }

#region Adjacency tables with flattening

        [BurstDiscard]
        public static WeaponMeshAdjacencyDataFlattened GetOrBuildAdjacencyFlattened([NotNull] Mesh mesh)
        {
            int id = mesh.GetInstanceID();
            if (adjacencyCache.TryGetValue(id, out WeaponMeshAdjacencyDataFlattened cached)) return cached;

            // Build adjacency as lists first
            List<int>[] lists = BuildAdjacencyLists(mesh);

            int n = lists.Length;
            int[] starts = new int[n + 1];
            int total = 0;
            for (int i = 0; i < n; ++i)
            {
                starts[i] = total;
                total += lists[i].Count;
            }

            starts[n] = total;

            int[] neighbors = new int[total];
            for (int i = 0; i < n; ++i)
            {
                int offset = starts[i];
                List<int> list = lists[i];
                for (int j = 0; j < list.Count; ++j) neighbors[offset + j] = list[j];
            }

            // Convert to native arrays
            NativeArray<int> startsNative = new(starts, Allocator.Persistent);
            NativeArray<int> neighborsNative = new(neighbors, Allocator.Persistent);

            cached = new WeaponMeshAdjacencyDataFlattened(startsNative, neighborsNative);
            adjacencyCache[id] = cached;
            return cached;
        }

        /// <summary>
        ///     Build adjacency as lists to store in cache
        /// </summary>
        /// <param name="mesh">Mesh to build adjacency for</param>
        /// <returns>Adjacency lists in an array form</returns>
        [BurstDiscard] [NotNull] private static List<int>[] BuildAdjacencyLists([NotNull] Mesh mesh)
        {
            int n = mesh.vertexCount;
            List<int>[] adj = new List<int>[n];
            for (int i = 0; i < n; ++i) adj[i] = new List<int>();

            int[] tris = mesh.triangles;
            for (int t = 0; t < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                AddEdge(adj, a, b);
                AddEdge(adj, b, a);
                AddEdge(adj, b, c);
                AddEdge(adj, c, b);
                AddEdge(adj, c, a);
                AddEdge(adj, a, c);
            }

            return adj;
        }

        /// <summary>
        ///     Add an edge to the adjacency list
        /// </summary>
        /// <param name="adj">Adjacency lists</param>
        /// <param name="from">Index of the first vertex</param>
        /// <param name="to">Index of the second vertex</param>
        [BurstDiscard] private static void AddEdge([NotNull] List<int>[] adj, int from, int to)
        {
            List<int> list = adj[from];

            // keep duplicates out
            if (!list.Contains(to)) list.Add(to);
        }

#endregion
    }
}