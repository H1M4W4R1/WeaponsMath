using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using WeaponsMath.Utility;

namespace WeaponsMath.Components
{
    /// <summary>
    ///     Vertex velocity component is class that should be used on both hitbox and
    ///     weapon objects to store per-vertex velocity data that is further used to compute
    ///     relative velocity between objects at hit position.
    /// </summary>
    [RequireComponent(typeof(WeaponSystemModel))] public sealed class VertexVelocityComponent : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Alternatively: how long should swing last to grant full energy")]
        private float expectedAttackTime = 1f;

        [Header("Debug")] [SerializeField] private bool enableDebug;
        [SerializeField] private float debugLineLength = 1f;

        /// <summary>
        ///     Stash of per-vertex velocity data
        /// </summary>
        private NativeArray<Vector3> _vertexVelocities;

        private NativeArray<Vector3> _lastVertexPositions;

        private float _accumulatedTime;

        private WeaponSystemModel _model;
        private Transform _modelTransform;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

        public NativeArray<Vector3> Velocities => _vertexVelocities;

        /// <summary>
        ///     Gets the velocity for the given point.
        /// </summary>
        public Vector3 GetVelocityForPoint(in Vector3 worldPoint)
        {
            // Update model transform if not found
            if (!_modelTransform) _modelTransform = transform;

            // Convert point to local space and find nearest triangle
            Vector3 localPoint = _modelTransform.InverseTransformPoint(worldPoint);
            int nTriangle = TriangleFinder.GetNearestTriangle(_model.Vertices, _model.Triangles, localPoint);

            // If no triangle found, return zero velocity
            if (nTriangle == -1) return Vector3.zero;

            Vector3 velocity = Vector3.zero;

            // Get vertices of the triangle
            int startTriangleVertex = nTriangle * 3;
            int firstVertex = _model.Triangles[startTriangleVertex];
            int secondVertex = _model.Triangles[startTriangleVertex + 1];
            int thirdVertex = _model.Triangles[startTriangleVertex + 2];

            // Transform vertex positions
            Vector3 firstVertexWorldPosition = _modelTransform.TransformPoint(_model.Vertices[firstVertex]);
            Vector3 secondVertexWorldPosition = _modelTransform.TransformPoint(_model.Vertices[secondVertex]);
            Vector3 thirdVertexWorldPosition = _modelTransform.TransformPoint(_model.Vertices[thirdVertex]);

            // Compute sum velocity affected by distance inversion
            float firstVertexDistance = 1f / Vector3.Distance(worldPoint, firstVertexWorldPosition);
            float secondVertexDistance = 1f / Vector3.Distance(worldPoint, secondVertexWorldPosition);
            float thirdVertexDistance = 1f / Vector3.Distance(worldPoint, thirdVertexWorldPosition);

            velocity += _vertexVelocities[firstVertex] * firstVertexDistance;
            velocity += _vertexVelocities[secondVertex] * secondVertexDistance;
            velocity += _vertexVelocities[thirdVertex] * thirdVertexDistance;

            // Average velocity
            float totalDistance = firstVertexDistance + secondVertexDistance + thirdVertexDistance;
            velocity /= totalDistance;

            return velocity;
        }

        private void Awake()
        {
            _model = GetComponent<WeaponSystemModel>();
            Assert.IsNotNull(_model, "WeaponSystemModel is null");
            Assert.IsNotNull(_model.Mesh, "Mesh is null");

            // Store model transform for later use
            _modelTransform = _model.transform;

            // We use mesh to ensure proper allocation
            _vertexVelocities = new NativeArray<Vector3>(_model.Mesh.vertexCount, Allocator.Persistent);
            _lastVertexPositions = new NativeArray<Vector3>(_model.Mesh.vertexCount, Allocator.Persistent);

            // Initialize last vertex positions
            for (int n = 0; n < _model.Mesh.vertexCount; n++)
            {
                _lastVertexPositions[n] = _modelTransform.TransformPoint(_model.Vertices[n]);
                _vertexVelocities[n] = Vector3.zero;
            }
        }

        private void Update()
        {
            _accumulatedTime += Time.deltaTime;
        }

        private void FixedUpdate()
        {
            // Update all vertices velocities
            for (int n = 0; n < _model.Vertices.Length; n++)
            {
                // Transform vertex position
                Vector3 currentVertexPosition = _modelTransform.TransformPoint(_model.Vertices[n]);
                Vector3 previousVertexPosition = _lastVertexPositions[n];

                // Compute vertex velocity
                Vector3 vertexVelocity;
                if (_accumulatedTime > 0f)
                    vertexVelocity = (currentVertexPosition - previousVertexPosition) / _accumulatedTime;
                else
                    vertexVelocity = Vector3.zero;

                // Compute time weight, we shall clamp this to ensure proper calculation as Unity sometimes tends
                // to do weird stuff, especially when frame takes longer than expectedAttackTime
                float timeWeight = expectedAttackTime > 0
                    ? math.clamp(_accumulatedTime / expectedAttackTime, 0f, 1f)
                    : 1f;
                
                // Interpolate vertex velocity and store it, same as new position to ensure proper calculation
                _vertexVelocities[n] = math.lerp(_vertexVelocities[n], vertexVelocity, timeWeight);
                _lastVertexPositions[n] = currentVertexPosition;
            }

            // Reset accumulated time
            _accumulatedTime = 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!enableDebug) return;
            if (!_vertexVelocities.IsCreated) return;

            for (int n = 0; n < _model.Vertices.Length; n++)
            {
                Gizmos.color = Color.yellow;

                // Compute vertex world position
                Vector3 vertexWorldPosition = _modelTransform.TransformPoint(_model.Vertices[n]);

                // Draw velocity direction
                Gizmos.DrawLine(vertexWorldPosition, vertexWorldPosition + _vertexVelocities[n] * debugLineLength);
            }
        }
#endif

        private void OnDestroy()
        {
            if (_vertexVelocities.IsCreated) _vertexVelocities.Dispose();
            if (_lastVertexPositions.IsCreated) _lastVertexPositions.Dispose();
        }
    }
}