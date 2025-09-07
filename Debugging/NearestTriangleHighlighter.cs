using System;
using UnityEngine;
using WeaponsMath.Utility;

namespace WeaponsMath.Debugging
{
    [RequireComponent(typeof(WeaponSystemModel))]
    public sealed class NearestTriangleHighlighter : MonoBehaviour
    {
        [SerializeField] private Transform referencePoint;
        private WeaponSystemModel _model;
        
        private void OnValidate()
        {
            _model = GetComponent<WeaponSystemModel>();
        }

        private void OnDrawGizmos()
        {
            if (_model == null) return;
            if (referencePoint == null) return;
         
            // Get transform
            Transform objTransform = transform;
            
            Gizmos.color = Color.red;
            
            // Inverse transform point
            Vector3 localPoint = objTransform.InverseTransformPoint(referencePoint.position);
            
            // Get nearest triangle
            int nearestTriangleIndex = TriangleFinder.GetNearestTriangle(_model.Vertices, _model.Triangles, localPoint);
            
            // Check if triangle was found
            if (nearestTriangleIndex < 0) return;
            
            // Find vertices
            int vertexAIndex = _model.Triangles[nearestTriangleIndex * 3];
            int vertexBIndex = _model.Triangles[nearestTriangleIndex * 3 + 1];
            int vertexCIndex = _model.Triangles[nearestTriangleIndex * 3 + 2];
            
            // Compute vertex world positions
            Vector3 vertexA = objTransform.TransformPoint(_model.Vertices[vertexAIndex]);
            Vector3 vertexB = objTransform.TransformPoint(_model.Vertices[vertexBIndex]);
            Vector3 vertexC = objTransform.TransformPoint(_model.Vertices[vertexCIndex]);
            
            // Draw triangle
            Gizmos.DrawLine(vertexA, vertexB);
            Gizmos.DrawLine(vertexB, vertexC);
            Gizmos.DrawLine(vertexC, vertexA);
        }
    }
}