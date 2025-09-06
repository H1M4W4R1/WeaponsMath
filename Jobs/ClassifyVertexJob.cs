using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using WeaponsMath.Enums;

namespace WeaponsMath.Jobs
{
    /// <summary>
    ///     Job used to classify vertices based on their neighbors
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Default, OptimizeFor = OptimizeFor.Performance)]
    internal struct ClassifyVerticesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<Vector3> normals;
        [ReadOnly] public NativeArray<int> neighborStarts; // length = vertexCount + 1
        [ReadOnly] public NativeArray<int> neighbors;

        // Params (primitive fields)
        public int depth;
        public int maxNeighbors;
        public float splitLow;
        public float splitHigh;
        public float depthDecay;
        public float distanceWeightPower;
        public float minSqrDistance;
        public int maxCollected;

        // Outputs
        [WriteOnly] public NativeArray<float> outScores; // [0..2]
        [WriteOnly] public NativeArray<byte> outTypes; // cast to enum later

        public unsafe void Execute(int vi)
        {
            // Validate small maxCollected; must be >0
            if (maxCollected <= 0)
            {
                outScores[vi] = 0f;
                outTypes[vi] = (byte) EdgeType.Blunt;
                return;
            }

            Vector3 origin = vertices[vi];
            Vector3 originNormal = normals[vi];
            float normSq = originNormal.x * originNormal.x + originNormal.y * originNormal.y +
                           originNormal.z * originNormal.z;
            if (normSq < 1e-6f)
            {
                originNormal = new Vector3(0f, 1f, 0f);
            }
            else
            {
                float inv = math.rsqrt(normSq);
                originNormal.x *= inv;
                originNormal.y *= inv;
                originNormal.z *= inv;
            }


            // Clamp maxCollected to a safe compile-time constant
            const int CONST_MAX_COLLECTED = 1024;
            if (maxCollected > CONST_MAX_COLLECTED)
                maxCollected = CONST_MAX_COLLECTED;

            float sumWeightedAbsDot = 0f;
            float sumWeights = 0f;
            int collected = 0;

            // If depth == 1, compute direct neighbor statistics exactly; else, do a simplified iterative expansion
            // (one-step expansion repeated depth times but without exact path-level visited deduplicate across steps).
            if (depth == 1)
            {
                // Iterate direct neighbors of vi
                int start = neighborStarts[vi];
                int end = neighborStarts[vi + 1];
                int neighborSeen = 0;
                
                // Iterate over neighbors
                for (int nIdx = start; nIdx < end; ++nIdx)
                {
                    // Skip self
                    int nb = neighbors[nIdx];
                    if (nb == vi) continue;

                    // Distance check
                    Vector3 v = vertices[nb] - origin;
                    float sqr = v.x * v.x + v.y * v.y + v.z * v.z;
                    if (sqr <= minSqrDistance) continue;

                    Vector3 dir = v * (1.0f / math.sqrt(sqr));
                    float absDot = math.abs(originNormal.x * dir.x + originNormal.y * dir.y +
                                            originNormal.z * dir.z);

                    float weight = math.pow(depthDecay, 0f); // depth=1 -> decay^0
                    if (distanceWeightPower > 0f)
                    {
                        float invDist = 1.0f / math.pow(sqr, 0.5f * distanceWeightPower);
                        weight *= invDist;
                    }

                    sumWeightedAbsDot += absDot * weight;
                    sumWeights += weight;
                    neighborSeen++;
                    if (maxNeighbors > 0 && neighborSeen >= maxNeighbors) break;
                    if (maxCollected > 0 && neighborSeen >= maxCollected) break;
                }
            }
            else
            {
                // Simplified iterative expansion: sample neighbors, then neighbors-of-neighbors up to depth
                // This will count duplicates across layers, but produces a useful approximation and is Burst-safe.
                // (If you need exact BFS with visited deduplicate across all depth, you will have a bad day).
                int* layer = stackalloc int[32]; // small stack buffer for current layer (32 neighbors typical)
                int* nextLayer = stackalloc int[128]; // slightly larger next layer buffer

                // Initialize layer with direct neighbors
                int start = neighborStarts[vi];
                int end = neighborStarts[vi + 1];
                int count = 0;
                
                // Copy neighbors to layer
                for (int nIdx = start; nIdx < end; ++nIdx)
                {
                    int nb = neighbors[nIdx];
                    if (nb == vi) continue;
                    if (count < 32) layer[count] = nb;
                    count++;
                    if (maxNeighbors > 0 && count >= maxNeighbors) break;
                }

                // Clamp to safe compile-time constant
                int layerCount = math.min(count, 32);

                // Expand layers up to depth
                for (int d = 1; d <= depth; ++d)
                {
                    // Process current layer
                    for (int li = 0; li < layerCount; ++li)
                    {
                        int idx = layer[li];
                        
                        // Compute contribution
                        Vector3 v = vertices[idx] - origin;
                        float sqr = v.x * v.x + v.y * v.y + v.z * v.z;
                        if (sqr <= minSqrDistance) continue;

                        // Compute angle
                        Vector3 dir = v * (1.0f / math.sqrt(sqr));
                        float absDot = math.abs(originNormal.x * dir.x + originNormal.y * dir.y +
                                                originNormal.z * dir.z);
                        float weight = math.pow(depthDecay, d - 1);

                        // Compute weight based on distance if needed
                        if (distanceWeightPower > 0f)
                        {
                            float invDist = 1.0f / math.pow(sqr, 0.5f * distanceWeightPower);
                            weight *= invDist;
                        }

                        // Update statistics
                        sumWeightedAbsDot += absDot * weight;
                        sumWeights += weight;
                        collected++;
                        if (maxCollected > 0 && collected >= maxCollected) break;
                    }

                    // Stop if we have collected enough neighbors
                    if (collected >= maxCollected) break;

                    // Build next layer from current layer (neighbors-of-neighbors), limited
                    int nextCount = 0;
                    for (int li = 0; li < layerCount; ++li)
                    {
                        int baseIdx = layer[li];
                        int s = neighborStarts[baseIdx];
                        int e = neighborStarts[baseIdx + 1];
                        int nAdded = 0;
                        for (int nIdx = s; nIdx < e; ++nIdx)
                        {
                            int nb = neighbors[nIdx];
                            if (nb == vi) continue;
                            if (nextCount < 128) nextLayer[nextCount] = nb;
                            nextCount++;
                            nAdded++;
                            if (maxNeighbors > 0 && nAdded >= maxNeighbors) break;
                        }

                        if (maxCollected > 0 && collected + nextCount >= maxCollected) break;
                    }

                    // Rotate buffers: layer = nextLayer (we overwrite layer's small buffer with first N of nextLayer)
                    int newLayerCount = math.min(nextCount, 32);
                    for (int i = 0; i < newLayerCount; ++i) layer[i] = nextLayer[i];
                    layerCount = newLayerCount;
                    if (layerCount == 0) break;
                }
            }

            // Compute final score
            float avgAbsDot = (sumWeights <= 0f) ? 0f : (sumWeightedAbsDot / sumWeights);
            float score = math.clamp(avgAbsDot * 2f, 0f, 2f);

            // Classify
            byte type;
            if (avgAbsDot <= splitLow)
                type = (byte) EdgeType.Blunt;
            else if (avgAbsDot >= splitHigh)
                type = (byte) EdgeType.Spike;
            else
                type = (byte) EdgeType.Blade;

            outScores[vi] = score;
            outTypes[vi] = type;
        }
    }
}