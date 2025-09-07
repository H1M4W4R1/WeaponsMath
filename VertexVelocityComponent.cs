using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using WeaponsMath.Utility;

namespace WeaponsMath
{
    /// <summary>
    ///     Vertex velocity component is class that should be used on both hitbox and
    ///     weapon objects to store per-vertex velocity data that is further used to compute
    ///     relative velocity between objects at hit position.
    /// </summary>
    [RequireComponent(typeof(WeaponSystemModel))] public sealed class VertexVelocityComponent : MonoBehaviour
    {
        /// <summary>
        ///     Stash of per-vertex velocity data
        /// </summary>
        private NativeArray<Vector3> _vertexVelocities;

        private float _accumulatedTime;

        private WeaponSystemModel _model;
        private Transform _modelTransform;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

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

            // Compute sum velocity affected by distance inversion
            float firstVertexDistance = 1f / Vector3.Distance(localPoint, _model.Vertices[firstVertex]);
            float secondVertexDistance = 1f / Vector3.Distance(localPoint, _model.Vertices[secondVertex]);
            float thirdVertexDistance = 1f / Vector3.Distance(localPoint, _model.Vertices[thirdVertex]);

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
        }

        private void Update()
        {
            _accumulatedTime += Time.deltaTime;
        }

        private void FixedUpdate()
        {
            // Perform calculation of each vertex velocity
            Vector3 currentPosition = _modelTransform.position;
            Quaternion currentRotation = _modelTransform.rotation;

            // Update all vertices velocities
            for (int n = 0; n < _model.Vertices.Length; n++)
            {
                // Compute vertex position in world space for previous and current frames
                Vector3 previousVertexPosition = _lastRotation * _model.Vertices[n] + _lastPosition;
                Vector3 currentVertexPosition = currentRotation * _model.Vertices[n] + currentPosition;

                // Compute vertex velocity
                Vector3 vertexVelocity = (currentVertexPosition - previousVertexPosition) / _accumulatedTime;

                // Store vertex velocity
                _vertexVelocities[n] = vertexVelocity;
            }

            // Store last position and rotation
            _lastPosition = currentPosition;
            _lastRotation = currentRotation;

            // Reset accumulated time
            _accumulatedTime = 0;
        }

        private void OnDestroy()
        {
            if (_vertexVelocities.IsCreated) _vertexVelocities.Dispose();
        }
    }
}