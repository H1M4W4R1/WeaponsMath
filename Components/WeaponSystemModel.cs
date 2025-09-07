using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace WeaponsMath.Components
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
        
        [field: SerializeField, HideInInspector]
        public Mesh Mesh { get; private set; }
        
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
            Mesh = _meshFilter.sharedMesh;
            Assert.IsNotNull(Mesh, "Mesh is null");
            
            vertices = new NativeArray<Vector3>(Mesh.vertices, Allocator.Persistent);
            triangles = new NativeArray<int>(Mesh.triangles, Allocator.Persistent);
            normals = new NativeArray<Vector3>(Mesh.normals, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
            if (normals.IsCreated) normals.Dispose();
        }
    }
}