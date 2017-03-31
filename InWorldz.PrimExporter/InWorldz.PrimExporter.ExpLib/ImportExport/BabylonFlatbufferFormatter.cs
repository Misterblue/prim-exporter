using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Text;
using System.Drawing;
using System.Drawing.Imaging;
using OpenMetaverse;
using System.IO;
using System.Drawing.Drawing2D;
using System.Linq;
using FlatBuffers;
using InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates;
using Murmurhash264A;
using Material = InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBuffers.Material;
using Quaternion = OpenMetaverse.Quaternion;
using Vector3 = OpenMetaverse.Vector3;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Formatter that outputs results suitable for using with three.js
    /// </summary>
    public class BabylonFlatbufferFormatter : IExportFormatter
    {
        private readonly ObjectHasher _objHasher = new ObjectHasher();
        readonly Vector3 _centerAdj = new Vector3(-128f, -128f, 0f);

        private void FixCoordinateSystem(ref Vector3 position, ref Quaternion rotation)
        {
            //center the object
            var centerPos = position + _centerAdj;
            //change coordinate system
            centerPos.X = -centerPos.X;
            //re-translate the object
            position = centerPos - _centerAdj;

            //compensate Y/Z flip
            var fixRot = Quaternion.CreateFromAxisAngle(1.0f, 0.0f, 0.0f, -(float)Math.PI / 2f);
            position = position * fixRot;
            rotation = fixRot * rotation;
        }

        public ExportResult Export(IEnumerable<GroupDisplayData> groups)
        {
            ExportStats stats = new ExportStats();
            BabylonFlatBufferOutputs outputs = new BabylonFlatBufferOutputs();
            string tempPath = Path.GetTempPath();

            var prims = new List<Mesh>();
            var groupInstances = new Dictionary<ulong, List<MeshInstance>>();

            foreach (var group in groups)
            {
                //see if we already have this group
                ulong groupHash = GetGroupHash(group);

                List <MeshInstance> instances;
                if (groupInstances.TryGetValue(groupHash, out instances))
                {
                    var pos = group.RootPrim.OffsetPosition;
                    var rot = group.RootPrim.OffsetRotation;

                    FixCoordinateSystem(ref pos, ref rot);

                    //yes, add this as an instance of the group
                    outputs.Instances.Add(new MeshInstance()
                    {
                        Name = groupHash + "_inst_" + instances.Count,
                        Position = pos,
                        Rotation = rot,
                        Scaling = group.RootPrim.Scale
                    });

                    stats.InstanceCount++;
                }
                else
                {
                    int startingPrimCount = stats.PrimCount;
                    int startingSubmeshCount = stats.SubmeshCount;

                    Tuple<string, Mesh, List<MeshInstance>> rootPrim = SerializeCombinedFaces(null, group.RootPrim, "png", tempPath, outputs, stats);
                    prims.Add(rootPrim.Item2);

                    foreach (var data in group.Prims.Where(p => p != group.RootPrim))
                    {
                        prims.Add(SerializeCombinedFaces(rootPrim.Item1, data, "png", tempPath, outputs, stats).Item2);
                    }

                    groupInstances.Add(groupHash, rootPrim.Item3);

                    stats.GroupsByPrimCount.Add(new Tuple<string, int>(group.ObjectName + "-" + groupHash, stats.PrimCount - startingPrimCount));
                    stats.GroupsBySubmeshCount.Add(new Tuple<string, int>(group.ObjectName + "-" + groupHash, stats.SubmeshCount - startingSubmeshCount));

                    stats.ConcreteCount++;
                }
            }
            

            var res = PackageResult(string.Empty, string.Empty, outputs, prims);
            stats.TextureCount = res.TextureFiles.Count;
            res.Stats = stats;
            return res;
        }

        private ulong GetGroupHash(GroupDisplayData group)
        {
            ulong groupHash = 5381;
            foreach (var prim in group.Prims)
            {
                groupHash = Murmur2.Hash(prim.ShapeHash, groupHash);
                groupHash = Murmur2.Hash(prim.MaterialHash, groupHash);
            }

            return groupHash;
        }

        public ExportResult Export(GroupDisplayData datas)
        {
            ExportStats stats = new ExportStats();
            BabylonFlatBufferOutputs outputs = new BabylonFlatBufferOutputs();
            string tempPath = Path.GetTempPath();

            var prims = new List<Mesh>();

            Tuple<string, Mesh, List<MeshInstance>> rootPrim = SerializeCombinedFaces(null, datas.RootPrim, "png", tempPath, outputs, stats);
            prims.Add(rootPrim.Item2);

            foreach (var data in datas.Prims.Where(p => p != datas.RootPrim))
            {
                prims.Add(SerializeCombinedFaces(rootPrim.Item1, data, "png", tempPath, outputs, stats).Item2);
            }

            var res = PackageResult(datas.ObjectName, datas.CreatorName, outputs, prims);
            stats.ConcreteCount = 1;
            stats.TextureCount = res.TextureFiles.Count;
            res.Stats = stats;
            res.Hash = GetGroupHash(datas);
            
            return res;
        }

        private static ExportResult PackageResult(string objectName, string creatorName, BabylonFlatBufferOutputs outputs, List<Mesh> prims)
        {
            ExportResult result = new ExportResult();
            result.ObjectName = objectName;
            result.CreatorName = creatorName;
            
            FlatBufferBuilder builder = new FlatBufferBuilder(2048);

            List<Offset<BabylonFlatBuffers.Material>> materialOffsets = new List<Offset<Material>>();
            foreach (var mat in outputs.Materials.Values)
            {
                var id = builder.CreateString(mat.Id);
                var name = builder.CreateString(mat.Name);
                var color = BabylonFlatBuffers.Material.CreateColorVector(builder, mat.Color);
                var textureName = builder.CreateString(mat.DiffuseTexture.Name);
                var texture = BabylonFlatBuffers.Texture.CreateTexture(builder, textureName, mat.DiffuseTexture.HasAlpha);

                var outMat = BabylonFlatBuffers.Material.CreateMaterial(builder, id, name, color, mat.ShinyPercent,
                    mat.Alpha, texture);
                materialOffsets.Add(outMat);
            }


            List<Offset<BabylonFlatBuffers.MultiMaterial>> multiMaterialOffsets = new List<Offset<BabylonFlatBuffers.MultiMaterial>>();
            foreach (var mat in outputs.MultiMaterials.Values)
            {
                var id = builder.CreateString(mat.Id);
                var name = builder.CreateString(mat.Name);

                List<StringOffset> materialIds = new List<StringOffset>();
                foreach (var matId in mat.MaterialsList)
                {
                    materialIds.Add(builder.CreateString(matId));
                }

                var matList = BabylonFlatBuffers.MultiMaterial.CreateMaterialsListVector(builder, materialIds.ToArray());
                var outMat = BabylonFlatBuffers.MultiMaterial.CreateMultiMaterial(builder, id, name, matList);
                multiMaterialOffsets.Add(outMat);
            }


            List<Offset<BabylonFlatBuffers.Mesh>> meshOffsets = new List<Offset<BabylonFlatBuffers.Mesh>>();
            foreach (var mesh in prims)
            {
                var id = builder.CreateString(mesh.Id);
                var name = builder.CreateString(mesh.Name);
                var parentId = builder.CreateString(mesh.ParentId);
                var materialId = builder.CreateString(mesh.MaterialId);
                var position = BabylonFlatBuffers.Mesh.CreatePositionVector(builder, mesh.Position);
                var rotationQuaternion = BabylonFlatBuffers.Mesh.CreateRotationQuaternionVector(builder,
                    mesh.RotationQuaternion);
                var scaling = BabylonFlatBuffers.Mesh.CreateScalingVector(builder, mesh.Scaling);
                var positions = BabylonFlatBuffers.Mesh.CreatePositionsVector(builder, mesh.Positions.ToArray());
                var normals = BabylonFlatBuffers.Mesh.CreateNormalsVector(builder, mesh.Normals.ToArray());
                var uvs = BabylonFlatBuffers.Mesh.CreateUvsVector(builder, mesh.UVs.ToArray());
                var indices = BabylonFlatBuffers.Mesh.CreateIndicesVector(builder, mesh.Indices.ToArray());

                var submeshList = new List<Offset<BabylonFlatBuffers.SubMesh>>();
                foreach (var submesh in mesh.Submeshes)
                {
                    var submeshOff = BabylonFlatBuffers.SubMesh.CreateSubMesh(builder, submesh.MaterialIndex,
                        submesh.VerticesStart, submesh.VerticesCount, submesh.IndexStart, submesh.IndexCount);
                    submeshList.Add(submeshOff);
                }

                var submeshes = BabylonFlatBuffers.Mesh.CreateSubmeshesVector(builder, submeshList.ToArray());

                var outMesh = BabylonFlatBuffers.Mesh.CreateMesh(builder, id, name, parentId, materialId, 
                    position, rotationQuaternion, scaling, positions, normals, uvs, indices, submeshes);

                meshOffsets.Add(outMesh);
            }

            var materialsVector = BabylonFlatBuffers.BabylonFileFlatbuffer.CreateMaterialsVector(builder, materialOffsets.ToArray());
            var multiMaterialsVector = BabylonFlatBuffers.BabylonFileFlatbuffer.CreateMultiMaterialsVector(builder, multiMaterialOffsets.ToArray());
            var meshesVector = BabylonFlatBuffers.BabylonFileFlatbuffer.CreateMeshesVector(builder,
                meshOffsets.ToArray());

            var offset = BabylonFlatBuffers.BabylonFileFlatbuffer.CreateBabylonFileFlatbuffer(builder, materialsVector,
                multiMaterialsVector, meshesVector);


            BabylonFlatBuffers.BabylonFileFlatbuffer.FinishBabylonFileFlatbufferBuffer(builder, offset);
            result.FaceBlob = new Tuple<byte[], int, int>(builder.DataBuffer.Data, builder.DataBuffer.Position, builder.DataBuffer.Length);

            return result;
        }

        public ExportResult Export(PrimDisplayData data)
        {
            ExportStats stats = new ExportStats();
            BabylonFlatBufferOutputs outputs = new BabylonFlatBufferOutputs();
            string tempPath = Path.GetTempPath();

            Tuple<string, Mesh, List<MeshInstance>> result = SerializeCombinedFaces(null, data, "png", tempPath, outputs, stats);
            
            var res = PackageResult("object", "creator", outputs, new List<Mesh>{result.Item2});
            res.Stats = stats;
            stats.ConcreteCount = 1;

            return res;
        }

        /// <summary>
        /// Writes the given material texture to a file and writes back to the KVP whether it contains alpha
        /// </summary>
        /// <param name="textureAssetId"></param>
        /// /// <param name="textureName"></param>
        /// <param name="fileRecord"></param>
        /// <param name="tempPath"></param>
        /// <returns></returns>
        private KeyValuePair<UUID, TrackedTexture> WriteMaterialTexture(UUID textureAssetId, string textureName, 
            string tempPath, List<string> fileRecord)
        {
            const int MAX_IMAGE_SIZE = 1024;

            Image img = null;
            bool hasAlpha = false;
            if (GroupLoader.Instance.LoadTexture(textureAssetId, ref img, false))
            {
                img = ConstrainTextureSize((Bitmap)img, MAX_IMAGE_SIZE);
                hasAlpha = DetectAlpha((Bitmap)img);
                string fileName = Path.Combine(tempPath, textureName);

                using (img)
                {
                    img.Save(fileName, ImageFormat.Png);
                }

                fileRecord.Add(fileName);
            }

            KeyValuePair<UUID, TrackedTexture> retMaterial = new KeyValuePair<UUID, TrackedTexture>(textureAssetId,
                new TrackedTexture { HasAlpha = hasAlpha, Name = textureName });

            return retMaterial;
        }

        private Image ConstrainTextureSize(Bitmap img, int size)
        {
            if (img.Width > size)
            {
                Image thumbNail = new Bitmap(size, size, img.PixelFormat);
                using (Graphics g = Graphics.FromImage(thumbNail))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    Rectangle rect = new Rectangle(0, 0, size, size);
                    g.DrawImage(img, rect);
                }

                img.Dispose();
                return thumbNail;
            }

            return img;
        }

        private bool DetectAlpha(Bitmap img)
        {
            for (int x = 0; x < img.Width; x++)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    Color c = img.GetPixel(x, y);
                    if (c.A < 255) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Serializes the combined faces and returns a mesh
        /// </summary>
        private Tuple<string, Mesh, List<MeshInstance>> SerializeCombinedFaces(
            string parent, PrimDisplayData data, 
            string materialType, string tempPath, BabylonFlatBufferOutputs outputs,
            ExportStats stats)
        {
            stats.PrimCount++;

            BabylonPrimFaceCombiner combiner = new BabylonPrimFaceCombiner();
            foreach (var face in data.Mesh.Faces)
            {
                combiner.CombineFace(face);
            }           
            
            combiner.Complete();
            
            List<string> materialsList = new List<string>();
            for (int i = 0; i < combiner.Materials.Count; i++)
            {
                var material = combiner.Materials[i];
                float shinyPercent = ShinyToPercent(material.Shiny);

                bool hasTexture = material.TextureID != OpenMetaverse.UUID.Zero;

                //check the material tracker, if we already have this texture, don't export it again
                TrackedTexture trackedTexture = null;

                if (hasTexture)
                {
                    if (outputs.Textures.ContainsKey(material.TextureID))
                    {
                        trackedTexture = outputs.Textures[material.TextureID];
                    }
                    else
                    {
                        string materialMapName = $"tex_mat_{material.TextureID}.{materialType}";
                        var kvp = this.WriteMaterialTexture(material.TextureID, materialMapName, tempPath, outputs.TextureFiles);

                        outputs.Textures.Add(kvp.Key, kvp.Value);

                        trackedTexture = kvp.Value;
                    }
                }

                var matHash = _objHasher.GetMaterialFaceHash(material);
                if (! outputs.Materials.ContainsKey(matHash))
                {
                    bool hasTransparent = material.RGBA.A < 1.0f || (trackedTexture != null && trackedTexture.HasAlpha);

                    Texture texture = null;
                    if (hasTexture)
                    {
                        texture = new Texture()
                        {
                            HasAlpha = hasTransparent,
                            Name = trackedTexture.Name
                        };
                    }

                    BabylonFlatBufferIntermediates.Material jsMaterial = new BabylonFlatBufferIntermediates.Material()
                    {
                        Alpha = material.RGBA.A,
                        Color = new[] { material.RGBA.R, material.RGBA.G, material.RGBA.B },
                        DiffuseTexture = hasTexture ? texture : null,
                        Id = matHash.ToString(),
                        Name = matHash.ToString(),
                        ShinyPercent = shinyPercent
                    }; 
                    
                    outputs.Materials.Add(matHash, jsMaterial);
                }

                materialsList.Add(matHash.ToString());
            }

            var multiMaterialName = data.MaterialHash + "_mm";
            if (!outputs.MultiMaterials.ContainsKey(data.MaterialHash))
            {
                //create the multimaterial
                var multiMaterial = new MultiMaterial()
                {
                    Id = multiMaterialName,
                    MaterialsList = materialsList,
                    Name = multiMaterialName
                };

                outputs.MultiMaterials[data.MaterialHash] = multiMaterial;
            }

            List<SubMesh> submeshes = new List<SubMesh>();
            foreach (var subMesh in combiner.SubMeshes)
            {
                submeshes.Add(new SubMesh()
                {
                    MaterialIndex = subMesh.MaterialIndex,
                    VerticesStart = subMesh.VerticesStart,
                    VerticesCount = subMesh.VerticesCount,
                    IndexStart = subMesh.IndexStart,
                    IndexCount = subMesh.IndexCount
                });

                stats.SubmeshCount++;
            }

            List<MeshInstance> instanceList = null;
            if (parent == null)
            {
                instanceList = new List<MeshInstance>();
            }

            //if this is a child prim, divide out the scale of the parent
            var scale = data.Scale;
            if (parent != null)
            {
                scale /= data.Parent.Scale;
            }

            Vector3 pos = data.OffsetPosition;
            Quaternion rot = data.OffsetRotation;

            if (parent == null)
            {
                FixCoordinateSystem(ref pos, ref rot);
            }
            

            var primId = data.ShapeHash + "_" + data.MaterialHash + (parent == null ? "_P" : "");

            Mesh mesh = new Mesh()
            {
                Name = primId,
                Id = primId,
                ParentId = parent,
                MaterialId = multiMaterialName,
                Position = new [] {pos.X, pos.Y, pos.Z},
                RotationQuaternion = new[] { rot.X, rot.Y, rot.Z, rot.W },
                Scaling = new[] {scale.X, scale.Y, scale.Z},
                Positions = combiner.Vertices,
                Normals = combiner.Normals,
                UVs = combiner.UVs,
                Indices = combiner.Indices,
                Submeshes = submeshes,
                Instances = instanceList
            };

            return new Tuple<string, Mesh, List<MeshInstance>>(primId, mesh, instanceList);
        }

        private float ShinyToPercent(Shininess shininess)
        {
            switch (shininess)
            {
                case Shininess.High:
                    return 1.0f;
                case Shininess.Medium:
                    return 0.5f;
                case Shininess.Low:
                    return 0.25f;
            }

            return 0.0f;
        }
        
    }
}
