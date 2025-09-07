// Example MonoBehaviour to compute per-vertex continuous score and visualize in Editor (Gizmos)

using UnityEngine;
using WeaponsMath.Data;
using WeaponsMath.Enums;

namespace WeaponsMath
{
    public sealed class WeaponEdgeTypeDrawer : MonoBehaviour
    {
        public MeshFilter meshFilter;
        public WeaponEdgeClassifierParams parameters = WeaponEdgeClassifierParams.Default;

        public float normalLength = 0.01f;
        public float baseSphereRadius = 0.002f;

        private float[] scores;
        private WeaponEdgeType[] types;

        [ContextMenu("Classify")] private void Classify()
        {
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;

            // Perform classification and store results
            WeaponMeshClassificationResult result = WeaponEdgeClassifier.ClassifyAllVertices(mesh, parameters);
            scores = result.scores;
            types = result.types;
        }

        private void OnValidate()
        {
            // clamp param safety
            parameters.depth = Mathf.Clamp(parameters.depth, 1, 32);
            parameters.splitLow = Mathf.Clamp01(parameters.splitLow);
            parameters.splitHigh = Mathf.Clamp01(parameters.splitHigh);
            if (parameters.splitLow >= parameters.splitHigh)
                parameters.splitHigh = Mathf.Min(1f, parameters.splitLow + 0.01f);
        }

        private void OnDrawGizmosSelected()
        {
            if (scores == null || types == null) Classify();
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;

            Transform meshTransform = meshFilter.transform;

            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < verts.Length; ++i)
            {
                Vector3 vertexWorldPosition = meshTransform.TransformPoint(verts[i]);
                if (i >= scores!.Length) return;

                float score = scores![i]; // 0..2
                // visualize: blue=blunt, white=blade, red=spike
                WeaponEdgeType type = types![i];
                switch (type)
                {
                    case WeaponEdgeType.Blunt: Gizmos.color = Color.blue; break;
                    case WeaponEdgeType.Blade: Gizmos.color = Color.green; break;
                    case WeaponEdgeType.Spike: Gizmos.color = Color.red; break;
                }

                Gizmos.DrawSphere(vertexWorldPosition, baseSphereRadius * (1f + score));

                // Draw normal
                Vector3 normal = meshTransform.TransformDirection(mesh.normals[i]);
                Gizmos.DrawLine(vertexWorldPosition, vertexWorldPosition + normal * normalLength);
            }
        }
    }
}