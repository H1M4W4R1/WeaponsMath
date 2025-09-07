using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;

namespace WeaponsMath.Debugging
{
    public sealed class WeaponMassDistributionDrawer : MonoBehaviour
    {
        [Header("Configuration")] public MeshFilter meshFilter;

        [Header("Visualization")] [Tooltip("Multiplier for sphere radius, used to make drawing easier to read")]
        public float baseSphereRadius = 0.002f;

        private void OnDrawGizmosSelected()
        {
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;

            WeaponMassDistributionCalculator.ComputeVertexMasses(mesh, out NativeArray<float> mass);
            
            Transform meshTransform = meshFilter.transform;

            Vector3[] verts = mesh.vertices;

            for (int i = 0; i < verts.Length; ++i)
            {
                Vector3 vertexWorldPosition = meshTransform.TransformPoint(verts[i]);
                float normalizedFactor = mass[i];

                Gizmos.color = Color.Lerp(Color.black, Color.white, normalizedFactor);
                Gizmos.DrawSphere(vertexWorldPosition, baseSphereRadius * normalizedFactor);
            }
            
            mass.Dispose();
        }
    }
}