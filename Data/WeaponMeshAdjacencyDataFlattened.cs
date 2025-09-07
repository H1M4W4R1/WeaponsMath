using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;

namespace WeaponsMath.Data
{
    public struct WeaponMeshAdjacencyDataFlattened : INativeDisposable
    {
        public NativeArray<int> neighborStarts;
        public NativeArray<int> neighbors;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeaponMeshAdjacencyDataFlattened(in NativeArray<int> neighborStarts, in NativeArray<int> neighbors)
        {
            this.neighborStarts = neighborStarts;
            this.neighbors = neighbors;
        }

        public void Dispose()
        {
            Dispose(default);
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            neighborStarts.Dispose(inputDeps);
            neighbors.Dispose(inputDeps);
            return inputDeps;
        }
    }
}