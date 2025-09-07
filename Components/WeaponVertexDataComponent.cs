using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using WeaponsMath.Data;
using WeaponsMath.Enums;
using WeaponsMath.Utility;

namespace WeaponsMath.Components
{
    [RequireComponent(typeof(WeaponSystemModel))] public sealed class WeaponVertexDataComponent : MonoBehaviour
    {
        [SerializeField] [HideInInspector] private WeaponEdgeType[] weaponEdgeTypes;
        [SerializeField] [HideInInspector] private float[] vertexMass;

        [Header("Configuration")] [SerializeField]
        private WeaponEdgeClassifierParams edgeClassifierParams = WeaponEdgeClassifierParams.Default;

        [Header("Debug")] [SerializeField] private bool enableDebug;
        [SerializeField] private float sphereSize = 0.005f;
        [SerializeField] private float massSphereSize = 1f;
        [SerializeField] private float normalLength = 0.1f;
        [SerializeField] private bool velocityAffectsNormal = true;

        private WeaponSystemModel _model;
        private VertexVelocityComponent _vertexVelocityComponent;

        private void OnValidate()
        {
            // Update data if null
            _model = GetComponent<WeaponSystemModel>();
            _vertexVelocityComponent = GetComponent<VertexVelocityComponent>();
            if (weaponEdgeTypes == null || vertexMass == null) ComputeData();
        }

        [ContextMenu("Compute Data")] private void ComputeData()
        {
            Assert.IsNotNull(_model, "WeaponSystemModel is null");

            weaponEdgeTypes = new WeaponEdgeType[_model.Mesh.vertexCount];
            vertexMass = new float[_model.Mesh.vertexCount];

            WeaponEdgeClassifier.ClassifyAllVertices(_model.Mesh, edgeClassifierParams,
                out NativeArray<WeaponEdgeType> scores);
            WeaponMassDistributionCalculator.ComputeVertexMasses(_model.Mesh, out NativeArray<float> masses);

            // Copy data
            scores.CopyTo(weaponEdgeTypes);
            masses.CopyTo(vertexMass);

            // Get rid of data
            scores.Dispose();
            masses.Dispose();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!enableDebug) return;
            if (!_model) return;
            Transform meshTransform = transform;

            bool hasVertexVelocityComponent = _vertexVelocityComponent != null;

            for (int i = 0; i < _model.Vertices.Length; ++i)
            {
                Vector3 vertexWorldPosition = meshTransform.TransformPoint(_model.Vertices[i]);
                if (i >= weaponEdgeTypes.Length) return;

                WeaponEdgeType type = weaponEdgeTypes[i];
                switch (type)
                {
                    case WeaponEdgeType.Blunt: Gizmos.color = Color.blue; break;
                    case WeaponEdgeType.Blade: Gizmos.color = Color.green; break;
                    case WeaponEdgeType.Spike: Gizmos.color = Color.red; break;
                }

                // Check, if object has VertexVelocityComponent and compute dot product
                // we also pre-compute world-space normal of vertex
                Vector3 normal = meshTransform.TransformDirection(_model.Normals[i]);
                
                float dotProduct = 1f;
                float velocity = 1f;
                if (velocityAffectsNormal && hasVertexVelocityComponent &&  
                    _vertexVelocityComponent.Velocities.Length > i)
                {
                    dotProduct = math.dot(math.normalize(_vertexVelocityComponent.Velocities[i]), normal);
                    velocity = math.lengthsq(_vertexVelocityComponent.Velocities[i]);
                }

                dotProduct = math.pow(dotProduct, 4);
               

                // Draw edge type debug
                Gizmos.DrawSphere(vertexWorldPosition, sphereSize);
                Gizmos.DrawLine(vertexWorldPosition, vertexWorldPosition + normal * (normalLength * dotProduct * velocity));

                // Draw mass debug
                Gizmos.color = Color.black;
                Gizmos.DrawWireSphere(vertexWorldPosition, massSphereSize * vertexMass[i]);
            }
        }
#endif
    }
}