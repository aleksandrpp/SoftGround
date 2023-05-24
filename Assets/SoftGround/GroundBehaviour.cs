using UnityEngine;
using UnityEngine.Rendering;
using AK.CG.Common;
using AK.MassiveShooting;
using Unity.Mathematics;

namespace AK.SoftGround
{
    public class GroundBehaviour : ShootBehaviour
    {
        [SerializeField] private GroundConfig _groundConfig;
        [SerializeField] private MeshFilter _groundFilter;
        [SerializeField] private MeshCollider _groundCollider;

        private Mesh _mesh;
        private CompShader _cs;
        private CompBuffer<Vertex> _vertices;

        public override void Start()
        {
            base.Start();

            _mesh = _groundFilter.mesh;

            _mesh.MarkDynamic();
            _mesh.SetVertexBufferParams(_mesh.vertexCount, Vertex.VertexBufferMemoryLayout);

            VertexBuffer();

            _cs = new CompShader(_groundConfig.Compute, _mesh.vertexCount, _groundConfig.InputCount);
            _cs.SetBuffer("_Vertices", _vertices.Buffer);

            AsyncGPUReadback.Request(_vertices.Buffer, OnReadback);
        }

        private void VertexBuffer()
        {
            var count = _mesh.vertexCount;

            _vertices = new CompBuffer<Vertex>(count);

            var vertices = _mesh.vertices;
            var normals = _mesh.normals;
            var uv = _mesh.uv;

            for (int i = 0; i < count; ++i)
            {
                var vertex = new Vertex
                {
                    Position = vertices[i],
                    Normal = normals[i],
                    UV = uv[i]
                };

                _vertices.SetData(i, vertex);
            }

            _vertices.ApplyData();
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            if (_mesh == null) return;

            if (request.done && !request.hasError)
            {
                using var vertices = request.GetData<Vertex>();

                _mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
                //_mesh.RecalculateNormals();

                // still bottleneck
                _groundCollider.sharedMesh = _mesh;
            }

            AsyncGPUReadback.Request(_vertices.Buffer, OnReadback);
        }

        public override void Update()
        {
            base.Update();

            Interact();
        }

        private void Interact()
        {
            var count = _impacts.Length;
            if (count == 0) return;

            using var inputs = new CompBuffer<Input>(count);
            for (int i = 0; i < count; i++)
            {
                inputs.SetData(i, new Input()
                {
                    Position = _impacts[i].Position,
                    Direction = math.down() * _groundConfig.InputPower
                });
            }
            inputs.ApplyData();

            _cs.SetBuffer("_Inputs", inputs.Buffer);
            _cs.Dispatch();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            _vertices.Dispose();
        }

        #region GUI

        public override void OnGUI()
        {
            base.OnGUI();

            var stl = new GUIStyle(GUI.skin.label)
            {
                padding = new RectOffset(150, 125, 195, 100),
                fontSize = 24
            };

            var text =
                $"Mesh/Collider vertices: {_vertices.Buffer.count}";

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), text, stl);
        }

        #endregion
    }
}