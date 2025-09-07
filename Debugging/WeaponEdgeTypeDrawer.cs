// Example MonoBehaviour to compute per-vertex continuous score and visualize in Editor (Gizmos)

using Unity.Mathematics;
using UnityEngine;
using WeaponsMath.Data;
using WeaponsMath.Enums;

namespace WeaponsMath.Debugging
{
    public sealed class WeaponEdgeTypeDrawer : MonoBehaviour
    {
        [Header("Classification")] public MeshFilter meshFilter;
        public WeaponEdgeClassifierParams parameters = WeaponEdgeClassifierParams.Default;

        [Header("Visualization")] [Tooltip("Multiplier for normal length, used to make drawing easier to read")]
        public float normalLength = 0.01f;

        [Tooltip("Multiplier for sphere radius, used to make drawing easier to read")]
        public float baseSphereRadius = 0.002f;

        [Header("Movement affection")] [Tooltip("Should drawn values be affected by movement vector?")]
        public bool enableMovementAffection;

        [Tooltip("Movement vector, used to affect drawn values")] public Vector3 movementVector = Vector3.forward;
        [Tooltip("Movement velocity, used to affect drawn values")] public float velocity = 1f;

        private float[] scores;

        [ContextMenu("Classify")] private void Classify()
        {
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;

            // Perform classification and store results
            WeaponMeshClassificationResult result = WeaponEdgeClassifier.ClassifyAllVertices(mesh, parameters);
            scores = result.scores;
        }

        private void OnValidate()
        {
            // clamp param safety
            parameters.depth = Mathf.Clamp(parameters.depth, 1, 32);
            parameters.splitLow = Mathf.Clamp01(parameters.splitLow);
            parameters.splitHigh = Mathf.Clamp01(parameters.splitHigh);
            if (parameters.splitLow >= parameters.splitHigh)
                parameters.splitHigh = Mathf.Min(1f, parameters.splitLow + 0.01f);

            // normalize visualization vector
            movementVector.Normalize();
        }

        private void OnDrawGizmosSelected()
        {
            if (scores == null) Classify();
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;

            Transform meshTransform = meshFilter.transform;
            Vector3 position = meshTransform.position;

            // Remap visualization vector to mesh
            Vector3 remappedVisualizationVector = meshTransform.TransformDirection(movementVector);

            // Draw remapped visualization vector from mesh center
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(position, position + remappedVisualizationVector * 0.5f);

            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < verts.Length; ++i)
            {
                Vector3 vertexWorldPosition = meshTransform.TransformPoint(verts[i]);
                if (i >= scores!.Length) return;

                float score = scores![i]; // 0..2
                // visualize: blue=blunt, white=blade, red=spike
                WeaponEdgeType type = WeaponEdgeClassifier.ClassifyVertexScore(score, parameters);
                switch (type)
                {
                    case WeaponEdgeType.Blunt: Gizmos.color = Color.blue; break;
                    case WeaponEdgeType.Blade: Gizmos.color = Color.green; break;
                    case WeaponEdgeType.Spike: Gizmos.color = Color.red; break;
                }

                Gizmos.DrawSphere(vertexWorldPosition, baseSphereRadius * (1f + score));
                Vector3 normal = meshTransform.TransformDirection(mesh.normals[i]);

                // Draw normal based on movement affection
                // This considers movement vector and velocity to affect normal length
                // based on E = 1/2 * m * v^2 formula while keeping normal angle affection
                // quite high to ensure that quadratic effect won't be too strong on non-related
                // normals what could yield terrible results.
                if (enableMovementAffection)
                {
                    // Compute dot product of normal and visualization vector
                    float dotProduct = math.dot(normal, remappedVisualizationVector);
                    dotProduct = math.max(dotProduct, 0); // Prevent angles greater than 90 degrees

                    // Increase strength
                    dotProduct = math.pow(dotProduct, 4);

                    // Compute new normal and draw it
                    Vector3 fixedNormal = normal * (normalLength * dotProduct * math.pow(velocity, 2));
                    Gizmos.DrawLine(vertexWorldPosition, vertexWorldPosition + fixedNormal);
                }
                else
                {
                    Gizmos.DrawLine(vertexWorldPosition, vertexWorldPosition + normal * normalLength);
                }
            }
        }
    }
}