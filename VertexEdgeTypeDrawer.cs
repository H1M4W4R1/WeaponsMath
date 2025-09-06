// Example MonoBehaviour to compute per-vertex continuous score and visualize in Editor (Gizmos)

using UnityEngine;
using WeaponsMath.Data;
using WeaponsMath.Enums;

namespace WeaponsMath
{
    public sealed class EdgeTypeDebugger : MonoBehaviour
    {
        public MeshFilter meshFilter;
        public WeaponEdgeClassifierParams parameters = WeaponEdgeClassifierParams.Default;

        private float[] scores;
        private EdgeType[] types;

        [ContextMenu("Classify")] private void Classify()
        {
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;
            
            // Perform classification and store results
            VerticesClassificationResult result = VertexEdgeClassifier.ClassifyAllVertices(mesh, parameters);
            scores = result.scores;
            types = result.types;
        }

        private void OnValidate()
        {
            // clamp param safety
            parameters.depth = Mathf.Clamp(parameters.depth, 1, 32);
            parameters.splitLow = Mathf.Clamp01(parameters.splitLow);
            parameters.splitHigh = Mathf.Clamp01(parameters.splitHigh);
            if (parameters.splitLow >= parameters.splitHigh) parameters.splitHigh = Mathf.Min(1f, parameters.splitLow + 0.01f);
        }

        private void OnDrawGizmosSelected()
        {
            if (scores == null || types == null) Classify();
            if (meshFilter == null) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;
            
            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < verts.Length; ++i)
            {
                Vector3 world = meshFilter.transform.TransformPoint(verts[i]);
                if (i >= scores!.Length) return;
                
                float score = scores![i]; // 0..2
                // visualize: blue=blunt, white=blade, red=spike
                EdgeType type = types![i];
                switch (type)
                {
                    case EdgeType.Blunt: Gizmos.color = Color.blue; break;
                    case EdgeType.Blade: Gizmos.color = Color.green; break;
                    case EdgeType.Spike: Gizmos.color = Color.red; break;
                }
                
                Gizmos.DrawSphere(world, 0.002f * (1f + score));
                
                Color c = Color.Lerp(Color.black, Color.white, score/2f);
                Gizmos.color = c;
                Gizmos.DrawWireSphere(world, 0.003f * (1f + score));
                
               
       
            }
        }
    }
}