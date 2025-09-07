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
        
        [Range(0, MAX_DEPTH)] public int depth; // n-depth BFS (best: 1..32)
        [Range(0, MAX_NEIGHBORS)] public int maxNeighbors; // cap on neighbors to consider (0 = no cap)
        [Range(0, 1f)] public float splitLow; // <= => Blunt (0..1)
        [Range(0, 1f)] public float splitHigh; // >= => Spike (0..1)
        [Range(0, 1f)] public float depthDecay; // weight multiplier per depth level (e.g. 0.7)

        [Range(0, 2f)]
        public float distanceWeightPower; // 0 = disabled, >0 to scale inverse-distance weight (e.g. 1.0)

        public float minSqrDistance; // avoid divis by zero
        [Range(1, MAX_COLLECTED)] public int maxCollected; // safety cap

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