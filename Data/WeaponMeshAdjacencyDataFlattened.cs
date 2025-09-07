namespace WeaponsMath.Data
{
    public struct WeaponMeshAdjacencyDataFlattened
    {
        public readonly int[] neighborStarts;
        public readonly int[] neighbors;
        
        public WeaponMeshAdjacencyDataFlattened(int[] neighborStarts, int[] neighbors)
        {
            this.neighborStarts = neighborStarts;
            this.neighbors = neighbors;
        }
    }
}