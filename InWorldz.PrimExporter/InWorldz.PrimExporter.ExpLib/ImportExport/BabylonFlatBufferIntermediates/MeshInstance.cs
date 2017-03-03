using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMetaverse;

namespace InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates
{
    /// <summary>
    /// Intermediate pre-flatbuffer mesh instance object
    /// </summary>
    internal class MeshInstance
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scaling { get; set; }

        public void ToFlatbuffer()
        {
            /*var name = builder.CreateString(groupHash + "_inst_" + instances.Count);
            var position = BabylonFlatBuffers.Vector3.CreateVector3(builder, pos.X, pos.Y, pos.Z);
            var rotation = BabylonFlatBuffers.Quaternion.CreateQuaternion(builder, rot.X, rot.Y, rot.Z, rot.W);
            var scale = BabylonFlatBuffers.Vector3.CreateVector3(builder, group.RootPrim.Scale.X,
                group.RootPrim.Scale.Y,
                group.RootPrim.Scale.Z);

            MeshInstance.StartMeshInstance(builder);
            MeshInstance.AddName(builder, name);
            MeshInstance.AddPosition(builder, position);
            MeshInstance.AddRotationQuaternion(builder, rotation);
            MeshInstance.AddScaling(builder, scale);
            MeshInstance.EndMeshInstance(builder);*/
        }
    }
}
