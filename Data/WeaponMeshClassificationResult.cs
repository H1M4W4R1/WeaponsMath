using WeaponsMath.Enums;

namespace WeaponsMath.Data
{
    public readonly ref struct WeaponMeshClassificationResult
    {
        public readonly float[] scores;
        public readonly WeaponEdgeType[] types;

        public WeaponMeshClassificationResult(float[] scores, WeaponEdgeType[] types)
        {
            this.scores = scores;
            this.types = types;
        }
    }
}