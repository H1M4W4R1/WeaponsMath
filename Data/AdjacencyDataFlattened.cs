namespace WeaponsMath.Data
{
    public struct AdjacencyDataFlattened
    {
        public readonly int[] neighborStarts;
        public readonly int[] neighbors;
        
        public AdjacencyDataFlattened(int[] neighborStarts, int[] neighbors)
        {
            this.neighborStarts = neighborStarts;
            this.neighbors = neighbors;
        }
    }
}