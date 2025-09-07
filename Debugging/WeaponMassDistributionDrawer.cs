using JetBrains.Annotations;
using UnityEngine;

namespace WeaponsMath.Debugging
{
    public sealed class WeaponMassDistributionDrawer : MonoBehaviour
    {
        [Header("Configuration")] public MeshFilter meshFilter;
        public Transform attachmentPoint;

        [Header("Visualization")] [Tooltip("Multiplier for sphere radius, used to make drawing easier to read")]
        public float baseSphereRadius = 0.002f;

        private void OnDrawGizmosSelected()
        {
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;

            float[] mass = ComputeVertexMasses(mesh, 1f);
            Vector3 attachmentPointWorldPosition = attachmentPoint.position;
            
            Transform meshTransform = meshFilter.transform;

            Vector3[] verts = mesh.vertices;

            float maxDistance = -1;
            for (int i = 0; i < verts.Length; ++i)
            {
                Vector3 vertexWorldPosition = meshTransform.TransformPoint(verts[i]);
                float distance = Vector3.Distance(attachmentPointWorldPosition, vertexWorldPosition);
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            for (int i = 0; i < verts.Length; ++i)
            {
                Vector3 vertexWorldPosition = meshTransform.TransformPoint(verts[i]);
                float distance = Vector3.Distance(attachmentPointWorldPosition, vertexWorldPosition);
                float normalizedDistance = 1.2f - distance / maxDistance;
                float normalizedFactor = mass[i] * normalizedDistance;

                Gizmos.color = Color.Lerp(Color.black, Color.white, normalizedFactor);
                Gizmos.DrawSphere(vertexWorldPosition, baseSphereRadius * normalizedFactor);
            }
        }

        [NotNull] private float[] ComputeVertexMasses([NotNull] Mesh mesh, float totalMass)
        {
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;
            float[] weights = new float[verts.Length];

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = verts[tris[i]];
                Vector3 b = verts[tris[i + 1]];
                Vector3 c = verts[tris[i + 2]];

                float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                weights[tris[i]] += area;
                weights[tris[i + 1]] += area;
                weights[tris[i + 2]] += area;
            }

            float sum = 0;
            foreach (float weight in weights) sum += weight;

            for (int i = 0; i < weights.Length; i++) weights[i] = (weights[i] / sum) * totalMass;

            return weights;
        }
    }
}