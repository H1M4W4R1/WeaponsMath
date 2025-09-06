using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using WeaponsMath.Data;
using WeaponsMath.Enums;
using WeaponsMath.Jobs;

namespace WeaponsMath
{
    public static class VertexEdgeClassifier
    {
        // Keeps adjacency per mesh instance as flattened arrays (start indices + neighbor list)
        private static readonly Dictionary<int, AdjacencyDataFlattened> adjacencyCache = new();

        /// <summary>
        ///     Public entry - returns managed arrays for scores and types.
        ///     Blocks until job completes.
        /// </summary>
        public static VerticesClassificationResult ClassifyAllVertices(
            [NotNull] Mesh mesh,
            WeaponEdgeClassifierParams p)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            int vc = mesh.vertexCount;
            if (vc == 0) return new VerticesClassificationResult(Array.Empty<float>(), Array.Empty<EdgeType>());

            // Validate normals exist
            Vector3[] normals = mesh.normals;
            if (normals == null || normals.Length != vc)
                throw new InvalidOperationException("Mesh must have normals (mesh.normals).");

            // Build or get adjacency flattened arrays
            AdjacencyDataFlattened data = GetOrBuildAdjacencyFlattened(mesh);

            // Convert managed arrays to NativeArray read-only views for job
            NativeArray<Vector3> verticesNative = new(mesh.vertices, Allocator.TempJob);
            NativeArray<Vector3> normalsNative = new(normals, Allocator.TempJob);
            NativeArray<int> neighborStartsNative = new(data.neighborStarts, Allocator.TempJob);
            NativeArray<int> neighborsNative = new(data.neighbors, Allocator.TempJob);

            NativeArray<float> scoresNative = new(vc, Allocator.TempJob);
            NativeArray<byte> typesNative = new(vc, Allocator.TempJob);

            // Schedule job to classify vertices
            ClassifyVerticesJob job = new()
            {
                vertices = verticesNative,
                normals = normalsNative,
                neighborStarts = neighborStartsNative,
                neighbors = neighborsNative,
                // copy params
                depth = p.depth,
                maxNeighbors = p.maxNeighbors,
                splitLow = p.splitLow,
                splitHigh = p.splitHigh,
                depthDecay = p.depthDecay,
                distanceWeightPower = p.distanceWeightPower,
                minSqrDistance = p.minSqrDistance,
                maxCollected = p.maxCollected,

                outScores = scoresNative,
                outTypes = typesNative
            };

            JobHandle handle = job.Schedule(vc, 64); // batch size 64
            handle.Complete();

            // Copy back to managed arrays
            float[] scores = new float[vc];
            EdgeType[] types = new EdgeType[vc];
            scoresNative.CopyTo(scores);
            for (int i = 0; i < vc; ++i) types[i] = (EdgeType) typesNative[i];

            // Dispose
            verticesNative.Dispose();
            normalsNative.Dispose();
            neighborStartsNative.Dispose();
            neighborsNative.Dispose();
            scoresNative.Dispose();
            typesNative.Dispose();

            // Return result
            return new VerticesClassificationResult(scores, types);
        }

#region Adjacency tables with flattening

        private static AdjacencyDataFlattened GetOrBuildAdjacencyFlattened([NotNull] Mesh mesh)
        {
            int id = mesh.GetInstanceID();
            if (adjacencyCache.TryGetValue(id, out AdjacencyDataFlattened cached)) return cached;

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

            cached = new AdjacencyDataFlattened(starts, neighbors);
            adjacencyCache[id] = cached;
            return cached;
        }

        /// <summary>
        ///     Build adjacency as lists to store in cache
        /// </summary>
        /// <param name="mesh">Mesh to build adjacency for</param>
        /// <returns>Adjacency lists in an array form</returns>
        [NotNull] private static List<int>[] BuildAdjacencyLists([NotNull] Mesh mesh)
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
        private static void AddEdge([NotNull] List<int>[] adj, int from, int to)
        {
            List<int> list = adj[from];

            // keep duplicates out
            if (!list.Contains(to)) list.Add(to);
        }

#endregion
    }
}