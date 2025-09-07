using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace WeaponsMath.Jobs
{
    [BurstCompile]
    public struct ComputeWeaponVertexMassJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<int> triangles;

        [NativeDisableParallelForRestriction] public NativeArray<float> vertexMasses;

        public void Execute(int index)
        {
            int triangleIndex = index * 3;
            
            float3 a = vertices[triangles[triangleIndex]];
            float3 b = vertices[triangles[triangleIndex + 1]];
            float3 c = vertices[triangles[triangleIndex + 2]];

            float area = math.length(math.cross(b - a, c - a)) * 0.5f;
            vertexMasses[triangles[triangleIndex]] += area;
            vertexMasses[triangles[triangleIndex + 1]] += area;
            vertexMasses[triangles[triangleIndex + 2]] += area;
        }
    }
}