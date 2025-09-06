using WeaponsMath.Enums;

namespace WeaponsMath.Data
{
    public readonly ref struct VerticesClassificationResult
    {
        public readonly float[] scores;
        public readonly EdgeType[] types;

        public VerticesClassificationResult(float[] scores, EdgeType[] types)
        {
            this.scores = scores;
            this.types = types;
        }
    }
}