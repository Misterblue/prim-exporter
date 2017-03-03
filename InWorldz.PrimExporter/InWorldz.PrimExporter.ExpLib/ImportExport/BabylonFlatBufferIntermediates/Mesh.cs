using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates
{
    /// <summary>
    /// Intermediate pre-flatbuffer mesh
    /// </summary>
    internal class Mesh
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public string MaterialId { get; set; }
        public float[] Position { get; set; }
        public float[] RotationQuaternion { get; set; }
        public float[] Scaling { get; set; }
        public List<float> Positions { get; set; }
        public List<float> Normals { get; set; }
        public List<float> UVs { get; set; }
        public List<ushort> Indices { get; set; }
        public List<SubMesh> Submeshes { get; set; }
        public List<MeshInstance> Instances { get; set; }
    }
}
