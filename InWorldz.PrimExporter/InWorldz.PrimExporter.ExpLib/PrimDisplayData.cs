using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrimMesher;
using OpenMetaverse;

namespace InWorldz.PrimExporter.ExpLib
{
    /// <summary>
    /// Data required to draw a primitive including the mesh and the texture/face data
    /// </summary>
    public class PrimDisplayData
    {
        public OpenMetaverse.Rendering.FacetedMesh Mesh;
        public bool IsRootPrim;
        public Vector3 OffsetPosition;
        public Quaternion OffsetRotation;
        public Vector3 Scale;
    }
}
