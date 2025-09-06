using System;
using UnityEngine;

namespace WeaponsMath.Data
{
    /// <summary>
    ///     Parameters used to classify weapon edges.
    /// </summary>
    [Serializable] public struct WeaponEdgeClassifierParams
    {
        [Range(0, 128)] public int depth; // n-depth BFS (best: 1..32)
        [Range(0, 128)] public int maxNeighbors; // cap on neighbors to consider (0 = no cap)
        [Range(0, 1f)] public float splitLow; // <= => Blunt (0..1)
        [Range(0, 1f)] public float splitHigh; // >= => Spike (0..1)
        [Range(0, 1f)] public float depthDecay; // weight multiplier per depth level (e.g. 0.7)

        [Range(0, 2f)]
        public float distanceWeightPower; // 0 = disabled, >0 to scale inverse-distance weight (e.g. 1.0)

        public float minSqrDistance; // avoid divis by zero
        [Range(1, 4096)] public int maxCollected; // safety cap

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