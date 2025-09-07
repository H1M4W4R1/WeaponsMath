using WeaponsMath.Enums;

namespace WeaponsMath.Data
{
    public readonly ref struct WeaponMeshClassificationResult
    {
        public readonly float[] scores;

        public WeaponMeshClassificationResult(float[] scores)
        {
            this.scores = scores;
        }
    }
}