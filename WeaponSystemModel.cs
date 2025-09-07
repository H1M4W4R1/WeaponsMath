using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace WeaponsMath
{
    /// <summary>
    ///     Class used to store weapon system model data (triangles, vertices, etc.)
    /// </summary>
    public sealed class WeaponSystemModel : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private NativeArray<Vector3> vertices;
        private NativeArray<int> triangles;
        private NativeArray<Vector3> normals;

        public NativeArray<Vector3> Vertices => vertices;
        public NativeArray<int> Triangles => triangles;
        public NativeArray<Vector3> Normals => normals;
        
        public Mesh Mesh => _meshFilter.sharedMesh;
        
        private void Awake()
        {
            UpdateMesh();
        }

        private void OnValidate()
        {
            UpdateMesh();
        }

        internal void UpdateMesh()
        {
            // Pre-compute data
            _meshFilter = GetComponent<MeshFilter>();
            Mesh mesh = _meshFilter.sharedMesh;
            Assert.IsNotNull(mesh, "Mesh is null");
            
            vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent);
            triangles = new NativeArray<int>(mesh.triangles, Allocator.Persistent);
            normals = new NativeArray<Vector3>(mesh.normals, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
            if (normals.IsCreated) normals.Dispose();
        }
    }
}