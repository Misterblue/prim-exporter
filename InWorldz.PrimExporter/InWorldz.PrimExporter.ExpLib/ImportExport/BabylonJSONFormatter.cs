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

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Formatter that outputs results suitable for using with three.js
    /// </summary>
    public class BabylonJSONFormatter : IExportFormatter
    {
        private readonly ObjectHasher _objHasher = new ObjectHasher();
        
        public ExportResult Export(GroupDisplayData datas)
        {
            BabylonOutputs outputs = new BabylonOutputs();
            string tempPath = Path.GetTempPath();

            var prims = new List<object>();

            Tuple<string, object> rootPrim = SerializeCombinedFaces(null, datas.RootPrim, "png", tempPath, outputs);
            prims.Add(rootPrim.Item2);

            foreach (var data in datas.Prims.Where(p => p != datas.RootPrim))
            {
                prims.Add(SerializeCombinedFaces(rootPrim.Item1, data, "png", tempPath, outputs).Item2);
            }

            return PackageResult(datas.ObjectName, datas.CreatorName, outputs, prims);
        }

        private static ExportResult PackageResult(string objectName, string creatorName, BabylonOutputs outputs, List<object> prims)
        {
            ExportResult result = new ExportResult();
            result.ObjectName = objectName;
            result.CreatorName = creatorName;

            var babylonFile = new
            {
                materials = outputs.Materials.Values,
                multiMaterials = outputs.MultiMaterials.Values,
                meshes = prims
            };

            result.FaceBytes.Add(Encoding.UTF8.GetBytes(JsonSerializer.SerializeToString(babylonFile)));
            result.TextureFiles = outputs.TextureFiles;

            return result;
        }

        public ExportResult Export(PrimDisplayData data)
        {
            BabylonOutputs outputs = new BabylonOutputs();
            string tempPath = Path.GetTempPath();

            Tuple<string, object> result = SerializeCombinedFaces(null, data, "png", tempPath, outputs);
            

            return PackageResult("object", "creator", outputs, new List<object>{result.Item2});
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
        private Tuple<string, object> SerializeCombinedFaces(
            string parent, PrimDisplayData data, 
            string materialType, string tempPath, BabylonOutputs outputs)
        {
            BabylonJSONPrimFaceCombiner combiner = new BabylonJSONPrimFaceCombiner();
            foreach (var face in data.Mesh.Faces)
            {
                combiner.CombineFace(face);
            }           
            
            
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

                    object texture = null;
                    if (hasTexture)
                    {
                        texture = new
                        {
                            name = trackedTexture.Name,
                            level = 1,
                            hasAlpha = hasTransparent,
                            getAlphaFromRGB = false,
                            coordinatesMode = 0,
                            uOffset = 0,
                            vOffset = 0,
                            uScale = 1,
                            vScale = 1,
                            uAng = 0,
                            vAng = 0,
                            wAng = 0,
                            wrapU = true,
                            wrapV = true,
                            coordinatesIndex = 0
                        };
                    }

                    var jsMaterial = new
                    {
                        name = matHash.ToString(),
                        id = matHash.ToString(),
                        ambient = new[] { material.RGBA.R, material.RGBA.G, material.RGBA.B },
                        diffuse = new[] { material.RGBA.R, material.RGBA.G, material.RGBA.B },
                        specular = new[] { material.RGBA.R * shinyPercent, material.RGBA.G * shinyPercent, material.RGBA.B * shinyPercent },
                        specularPower = 50,
                        emissive = new[] { 0.01f, 0.01f, 0.01f },
                        alpha = material.RGBA.A,
                        backFaceCulling = true,
                        wireframe = false,
                        diffuseTexture = hasTexture ? texture : null,
                        useLightmapAsShadowmap = false,
                        checkReadOnlyOnce = true
                    };
                    
                    outputs.Materials.Add(matHash, jsMaterial);
                }

                materialsList.Add(matHash.ToString());
            }

            var multiMaterialName = data.MaterialHash + "_mm";
            if (!outputs.MultiMaterials.ContainsKey(data.MaterialHash))
            {
                //create the multimaterial
                var multiMaterial = new
                {
                    name = multiMaterialName,
                    id = multiMaterialName,
                    materials = materialsList
                };

                outputs.MultiMaterials[data.MaterialHash] = multiMaterial;
            }

            //finally serialize the mesh
            Vector3 pos = data.IsRootPrim ? Vector3.Zero : data.OffsetPosition;
            Quaternion rot = data.IsRootPrim ? Quaternion.Identity : data.OffsetRotation;

            float[] identity4x4 = 
            {
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            };

            List<object> submeshes = new List<object>();
            foreach (var subMesh in combiner.SubMeshes)
            {
                submeshes.Add(new
                {
                    materialIndex = subMesh.MaterialIndex,
                    verticesStart = subMesh.VerticesStart,
                    verticesCount = subMesh.VerticesCount,
                    indexStart = subMesh.IndexStart,
                    indexCount = subMesh.IndexCount
                });
            }

            var primId = data.ShapeHash + "_" + data.MaterialHash + "-" + Guid.NewGuid();
            var mesh = new
            {
                name = primId,
                id = primId,
                parentId = parent,
                materialId = multiMaterialName,
                position = new [] {pos.X, pos.Y, pos.Z},
                rotationQuaternion = new[] { rot.X, rot.Y, rot.Z, rot.W },
                scaling = new[] {data.Scale.X, data.Scale.Y, data.Scale.Z},
                pivotMatrix = identity4x4,
                infiniteDistance = false,
                showBoundingBox = false,
                showSubMeshesBoundingBox = false,
                isVisible = true,
                isEnabled = true,
                pickable = true,
                applyFog = false,
                checkCollisions = false,
                receiveShadows = true,
                positions = combiner.Vertices,
                normals = combiner.Normals,
                uvs = combiner.UVs,
                indices = combiner.Indices,
                subMeshes = submeshes,
                autoAnimate = false,
                billboardMode = 0
            };

            return new Tuple<string, object>(primId, mesh);
        }

        private float ShinyToPercent(OpenMetaverse.Shininess shininess)
        {
            switch (shininess)
            {
                case OpenMetaverse.Shininess.High:
                    return 1.0f;
                case OpenMetaverse.Shininess.Medium:
                    return 0.5f;
                case OpenMetaverse.Shininess.Low:
                    return 0.25f;
            }

            return 0.0f;
        }


    }
}
