using System;
using UnityEngine;

namespace WeaponsMath.Data
{
    /// <summary>
    ///     Parameters used to classify weapon edges.
    /// </summary>
    [Serializable] public struct WeaponEdgeClassifierParams
    {
        public const int MAX_COLLECTED = 1024;
        public const int MAX_DEPTH = 128;
        public const int MAX_NEIGHBORS = 128;

        [Range(0, MAX_DEPTH)]
        [Tooltip("Depth of scanning, recommended values: 1..32, but keep it low to avoid performance issues")]
        public int depth; // n-depth BFS (best: 1..32)

        [Range(0, MAX_NEIGHBORS)]
        [Tooltip(
            "Safety cap for neighbors to scan, recommended values: 8..128. Zero is disabled (not recommended)")]
        public int maxNeighbors; // cap on neighbors to consider (0 = no cap)

        [Range(0, 1f)] [Tooltip("Values under this one will be considered blunt, above are blades or spikes")]
        public float splitLow; // <= => Blunt (0..1)

        [Range(0, 1f)]
        [Tooltip(
            "Values above this one will be considered spikes, when lower then it will be considered blades (only if higher than " +
            nameof(splitLow) + ")")]
        public float splitHigh; // >= => Spike (0..1)

        [Range(0, 1f)]
        [Tooltip("Weight multiplier per depth level - reduces weight of vertices that are not directly connected")]
        public float depthDecay; // weight multiplier per depth level (e.g. 0.7)

        [Range(0, 1f)] [Tooltip("0 = disabled, when above it causes weight to decrease with distance")]
        public float distanceWeightPower; // 0 = disabled, >0 to scale inverse-distance weight (e.g. 1.0)

        [Tooltip(
            "Minimum distance between vertices to avoid division by zero, recommended values 1e-6..1e-12, use lower values for more precision in some meshes")]
        public float minSqrDistance; // avoid divis by zero

        [Range(1, MAX_COLLECTED)]
        [Tooltip(
            "Limit neighbours collection mechanism, recommended values: 16..1024, keep it low to avoid performance issues")]
        public int maxCollected; // safety cap

        public static WeaponEdgeClassifierParams Default => new()
        {
            depth = 3,
            maxNeighbors = 16,
            splitLow = 0.2f,
            splitHigh = 0.7f,
            depthDecay = 0.0f,
            distanceWeightPower = 0.0f,
            minSqrDistance = 1e-6f,
            maxCollected = 128
        };
    }
}