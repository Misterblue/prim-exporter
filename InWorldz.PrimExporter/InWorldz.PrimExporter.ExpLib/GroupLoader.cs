using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using InWorldz.Data.Inventory.Cassandra;
using System.Drawing;
using System.Linq;
using OpenMetaverse.Rendering;
using MySql.Data.MySqlClient;
using InWorldz.Region.Data.Thoosa.Engines;
using Nini.Config;
using System.Reflection;
using OpenSim.Data;

namespace InWorldz.PrimExporter.ExpLib
{
    public sealed class GroupLoader
    {
        [Flags]
        public enum LoaderChecks
        {
            PrimLimit = (1 << 0), 
            UserMustBeCreator = (1 << 1),
            TexturesMustBeFullPerm = (1 << 2),
            FlipTextureUVs = (1 << 3)
        }

        private const int DEFAULT_PART_VERT_LIMIT = 50000;

        public class LoaderParams
        {
            public int PrimLimit;
            public LoaderChecks Checks;
            public int PartVertLimit = DEFAULT_PART_VERT_LIMIT;
        }

        

        private static readonly GroupLoader instance = new GroupLoader();

        private readonly Data.Assets.Stratus.StratusAssetClient _stratus;
        private InventoryStorage _inv;
        private LegacyMysqlInventoryStorage _legacyInv;
        private readonly CassandraMigrationProviderSelector _invSelector;
        private readonly MeshmerizerR _renderer = new MeshmerizerR();
        private Dictionary<UUID, string> _usernameCache = new Dictionary<UUID,string>();

        static GroupLoader()
        {
        }

        private GroupLoader()
        {
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            IniConfigSource config = new IniConfigSource(System.IO.Path.Combine(dir, "OpenSim.ini"));
            var settings = new OpenSim.Framework.ConfigSettings();
            settings.SettingsFile = config.Configs;

            Data.Assets.Stratus.Config.Settings.Instance.DisableWritebackCache = true;
            _stratus = new Data.Assets.Stratus.StratusAssetClient();
            _stratus.Initialize(settings);
            _stratus.Start();

            _legacyInv = new LegacyMysqlInventoryStorage(Properties.Settings.Default.CoreConnStr);
            _inv = new InventoryStorage(Properties.Settings.Default.InventoryCluster);
            _invSelector = new CassandraMigrationProviderSelector(Properties.Settings.Default.MigrationActive,
                Properties.Settings.Default.CoreConnStr, _inv, _legacyInv);
        }

        public void ShutDown()
        {
            _stratus.Stop();
        }

        public static GroupLoader Instance
        {
            get
            {
                return instance;
            }
        }

        public GroupDisplayData Load(UUID userId, UUID itemId, LoaderParams parms)
        {
            OpenSim.Data.IInventoryStorage inv = _invSelector.GetProvider(userId);

            InventoryItemBase item = inv.GetItem(itemId, UUID.Zero);
            if (item == null) throw new Exceptions.PrimExporterPermissionException("The item could not be found");

            var asset = _stratus.RequestAssetSync(item.AssetID);

            if (item.Owner != userId)
            {
                throw new Exceptions.PrimExporterPermissionException("You do not own that object");
            }

            if (((parms.Checks & LoaderChecks.UserMustBeCreator) != 0) && item.CreatorIdAsUuid != userId)
            {
                throw new Exceptions.PrimExporterPermissionException("You are not the creator of the base object");
            }

            //get the user name
            string userName = LookupUserName(item.CreatorIdAsUuid);

            //try thoosa first
            SceneObjectGroup sog;

            InventoryObjectSerializer engine = new InventoryObjectSerializer();
            if (engine.CanDeserialize(asset.Data))
            {
                sog = engine.DeserializeGroupFromInventoryBytes(asset.Data);
            }
            else
            {
                sog = SceneXmlLoader.DeserializeGroupFromXml2(Utils.BytesToString(asset.Data));
            }

            return GroupDisplayDataFromSOG(userId, parms, sog, inv, userName, item);
        }

        public GroupDisplayData LoadFromXML(string xmlData, LoaderParams parms)
        {
            SceneObjectGroup sog = SceneXmlLoader.DeserializeGroupFromXml2(xmlData);
            return GroupDisplayDataFromSOG(UUID.Zero, parms, sog, null, string.Empty, null);
        }

        private GroupDisplayData GroupDisplayDataFromSOG(UUID userId, LoaderParams parms, SceneObjectGroup sog,
            IInventoryStorage inv, string userName, InventoryItemBase item)
        {
            if (((parms.Checks & LoaderChecks.PrimLimit) != 0) && sog.GetParts().Count > parms.PrimLimit)
            {
                throw new Exceptions.PrimExporterPermissionException("Object contains too many prims");
            }

            HashSet<UUID> fullPermTextures = CollectFullPermTexturesIfNecessary(ref userId, parms, inv);

            List<PrimDisplayData> groupData = new List<PrimDisplayData>();
            foreach (SceneObjectPart part in sog.GetParts())
            {
                if (((parms.Checks & LoaderChecks.UserMustBeCreator) != 0) && part.CreatorID != userId)
                {
                    throw new Exceptions.PrimExporterPermissionException("You are not the creator of all parts");
                }

                PrimDisplayData pdd = this.ExtractPrimMesh(part, parms, fullPermTextures);
                int vertCount = 0;
                foreach (var face in pdd.Mesh.Faces)
                {
                    vertCount += face.Vertices.Count;
                }

                if (vertCount <= parms.PartVertLimit)
                {
                    groupData.Add(pdd);
                }
            }

            return new GroupDisplayData {Prims = groupData, CreatorName = userName, ObjectName = item?.Name.Replace('_', ' ') ?? ""};
        }

        private string LookupUserName(UUID uuid)
        {
            string userName;
            if (! _usernameCache.TryGetValue(uuid, out userName))
            {
                userName = DbLookupUser(uuid);
            }

            return userName;
        }

        private string DbLookupUser(UUID uuid)
        {
            using (MySqlConnection conn = new MySqlConnection(Properties.Settings.Default.CoreConnStr))
            {
                conn.Open();

                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = String.Format("SELECT CONCAT(username, ' ', lastname) FROM users WHERE UUID = '{0}' LIMIT 1", uuid.ToString());
                    return (string)cmd.ExecuteScalar();
                }
            }
        }

        private HashSet<UUID> CollectFullPermTexturesIfNecessary(ref UUID userId, LoaderParams parms, OpenSim.Data.IInventoryStorage inv)
        {
            HashSet<UUID> fullPermTextures;
            if ((parms.Checks & LoaderChecks.TexturesMustBeFullPerm) != 0)
            {
                fullPermTextures = new HashSet<UUID>();

                //check the textures folder...
                InventoryFolderBase textureFolder = inv.FindFolderForType(userId, AssetType.Texture);
                InventoryFolderBase fullTextureFolder = inv.GetFolder(textureFolder.ID);

                if (textureFolder != null)
                {
                    RecursiveCollectFullPermTextureIds(inv, fullTextureFolder, fullPermTextures);
                }
                else
                {
                    throw new ApplicationException("Could not find texture folder");
                }

                InventoryFolderBase objFolder = inv.FindFolderForType(userId, AssetType.Object);
                InventoryFolderBase dsFolder = null;
                foreach (InventorySubFolderBase subFolder in objFolder.SubFolders)
                {
                    if (subFolder.Name.ToLower() == "dreamshare")
                    {
                        dsFolder = inv.GetFolder(subFolder.ID);
                    }
                }

                if (dsFolder != null)
                {
                    RecursiveCollectFullPermTextureIds(inv, dsFolder, fullPermTextures);
                }

                return fullPermTextures;
            }

            return null;
        }

        private void RecursiveCollectFullPermTextureIds(OpenSim.Data.IInventoryStorage inv, InventoryFolderBase parentFolder, HashSet<UUID> fullPermTextures)
        {
            //depth first
            foreach (var childFolder in parentFolder.SubFolders)
            {
                InventoryFolderBase fullChild = inv.GetFolder(childFolder.ID);
                RecursiveCollectFullPermTextureIds(inv, fullChild, fullPermTextures);
            }

            foreach (var item in parentFolder.Items)
            {
                if (item.AssetType == (int)AssetType.Texture)
                {
                    if (((item.CurrentPermissions & (uint)PermissionMask.Copy) != 0) &&
                        ((item.CurrentPermissions & (uint)PermissionMask.Modify) != 0) &&
                        ((item.CurrentPermissions & (uint)PermissionMask.Transfer) != 0))
                    {
                        fullPermTextures.Add(item.AssetID);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate a hash value over fields that can affect the underlying physics shape.
        /// Things like RenderMaterials and TextureEntry data are not included.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="lod"></param>
        /// <returns>ulong - a calculated hash value</returns>
        public ulong GetMeshShapeHash(PrimitiveBaseShape shape, DetailLevel lod)
        {
            ulong hash = 5381;

            hash = djb2(hash, shape.PathCurve);
            hash = djb2(hash, (byte)((byte)shape.HollowShape | (byte)shape.ProfileShape));
            hash = djb2(hash, shape.PathBegin);
            hash = djb2(hash, shape.PathEnd);
            hash = djb2(hash, shape.PathScaleX);
            hash = djb2(hash, shape.PathScaleY);
            hash = djb2(hash, shape.PathShearX);
            hash = djb2(hash, shape.PathShearY);
            hash = djb2(hash, (byte)shape.PathTwist);
            hash = djb2(hash, (byte)shape.PathTwistBegin);
            hash = djb2(hash, (byte)shape.PathRadiusOffset);
            hash = djb2(hash, (byte)shape.PathTaperX);
            hash = djb2(hash, (byte)shape.PathTaperY);
            hash = djb2(hash, shape.PathRevolutions);
            hash = djb2(hash, (byte)shape.PathSkew);
            hash = djb2(hash, shape.ProfileBegin);
            hash = djb2(hash, shape.ProfileEnd);
            hash = djb2(hash, shape.ProfileHollow);

            // Include LOD in hash, accounting for endianness
            byte[] lodBytes = new byte[4];
            Buffer.BlockCopy(BitConverter.GetBytes((int)lod), 0, lodBytes, 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lodBytes, 0, 4);
            }

            foreach (byte t in lodBytes)
                hash = djb2(hash, t);

            // include sculpt UUID
            if (shape.SculptEntry)
            {
                var sculptUUIDBytes = shape.SculptTexture.GetBytes();
                foreach (byte t in sculptUUIDBytes)
                    hash = djb2(hash, t);

                hash = djb2(hash, shape.SculptType);
            }

            return hash;
        }

        /// <summary>
        /// Returns a hash value calculated from face parameters that would affect
        /// the appearance of the mesh faces but not their shape
        /// </summary>
        /// <param name="faces"></param>
        /// <returns></returns>
        public ulong GetMeshMaterialHash(FacetedMesh mesh, Primitive prim)
        {
            ulong hash = 5381;

            var numFaces = mesh.Faces.Count;
            for (int i = 0; i < numFaces; i++)
            {
                Primitive.TextureEntryFace teFace = prim.Textures.GetFace((uint)i);
                hash = djb2(hash, (ushort) teFace.Bump);
                hash = djb2(hash, (byte) (teFace.Fullbright ? 1 : 0));
                hash = djb2(hash, BitConverter.GetBytes(teFace.Glow));
                hash = djb2(hash, (byte) (teFace.MediaFlags ? 1 : 0));
                hash = djb2(hash, BitConverter.GetBytes(teFace.OffsetU));
                hash = djb2(hash, BitConverter.GetBytes(teFace.OffsetV));
                hash = djb2(hash, BitConverter.GetBytes(teFace.RepeatU));
                hash = djb2(hash, BitConverter.GetBytes(teFace.RepeatV));
                hash = djb2(hash, BitConverter.GetBytes(teFace.Rotation));
                hash = djb2(hash, teFace.RGBA.GetBytes());
                hash = djb2(hash, (byte)teFace.Shiny);
                hash = djb2(hash, (byte)teFace.TexMapType);
                hash = djb2(hash, teFace.TextureID.GetBytes());
            }

            return hash;
        }

        private ulong djb2(ulong hash, byte c)
        {
            return ((hash << 5) + hash) + (ulong)c;
        }

        private ulong djb2(ulong hash, ushort c)
        {
            hash = ((hash << 5) + hash) + (ulong)((byte)c);
            return ((hash << 5) + hash) + (ulong)(c >> 8);
        }

        private ulong djb2(ulong hash, byte[] bytes)
        {
            return bytes.Aggregate(hash, (current, b) => djb2(current, b));
        }

        private PrimDisplayData ExtractPrimMesh(SceneObjectPart part, LoaderParams parms, HashSet<UUID> fullPermTextures)  
        {
            Primitive prim = part.Shape.ToOmvPrimitive(part.OffsetPosition, part.RotationOffset);
            //always generate at scale 1.0 and export the true scale for each part
            prim.Scale = new Vector3(1, 1, 1);

            FacetedMesh mesh;
            try
            {
                if (prim.Sculpt != null && prim.Sculpt.SculptTexture != UUID.Zero)
                {
                    if (prim.Sculpt.Type != SculptType.Mesh)
                    { // Regular sculptie
                        Image img = null;
                        if (!LoadTexture(prim.Sculpt.SculptTexture, ref img, true))
                            return null;

                        mesh = _renderer.GenerateFacetedSculptMesh(prim, (Bitmap)img, DetailLevel.Highest);
                        img.Dispose();
                    }
                    else
                    { // Mesh
                        var meshAsset = _stratus.RequestAssetSync(prim.Sculpt.SculptTexture);
                        if (! FacetedMesh.TryDecodeFromAsset(prim, new OpenMetaverse.Assets.AssetMesh(prim.Sculpt.SculptTexture, meshAsset.Data), DetailLevel.Highest, out mesh))
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    mesh = _renderer.GenerateFacetedMesh(prim, DetailLevel.Highest);
                }
            }
            catch
            {
                return null;
            }

            // Create a FaceData struct for each face that stores the 3D data
            // in a OpenGL friendly format
            for (int j = 0; j < mesh.Faces.Count; j++)
            {
                Face face = mesh.Faces[j];
                PrimFace.FaceData data = new PrimFace.FaceData();

                // Vertices for this face
                data.Vertices = new float[face.Vertices.Count * 3];
                data.Normals = new float[face.Vertices.Count * 3];
                for (int k = 0; k < face.Vertices.Count; k++)
                {
                    data.Vertices[k * 3 + 0] = face.Vertices[k].Position.X;
                    data.Vertices[k * 3 + 1] = face.Vertices[k].Position.Y;
                    data.Vertices[k * 3 + 2] = face.Vertices[k].Position.Z;

                    data.Normals[k * 3 + 0] = face.Vertices[k].Normal.X;
                    data.Normals[k * 3 + 1] = face.Vertices[k].Normal.Y;
                    data.Normals[k * 3 + 2] = face.Vertices[k].Normal.Z;
                }

                // Indices for this face
                data.Indices = face.Indices.ToArray();

                // Texture transform for this face
                Primitive.TextureEntryFace teFace = prim.Textures.GetFace((uint)j);

                //not sure where this bug is coming from, but in order for sculpt textures
                //to line up, we need to flip V here
                if (prim.Sculpt != null && prim.Sculpt.Type != SculptType.None && prim.Sculpt.Type != SculptType.Mesh)
                {
                    teFace.RepeatV *= -1.0f;
                }

                _renderer.TransformTexCoords(face.Vertices, face.Center, teFace, prim.Scale);

                // Texcoords for this face
                data.TexCoords = new float[face.Vertices.Count * 2];
                for (int k = 0; k < face.Vertices.Count; k++)
                {
                    data.TexCoords[k * 2 + 0] = face.Vertices[k].TexCoord.X;
                    data.TexCoords[k * 2 + 1] = face.Vertices[k].TexCoord.Y;
                }

                if (((parms.Checks & LoaderChecks.TexturesMustBeFullPerm) != 0))
                {
                    if (teFace.TextureID != UUID.Zero && !fullPermTextures.Contains(teFace.TextureID))
                    {
                        teFace.TextureID = UUID.Zero;
                    }
                }

                //store the actual texture
                data.TextureInfo = new PrimFace.TextureInfo { TextureID = teFace.TextureID };

                // Set the UserData for this face to our FaceData struct
                face.UserData = data;
                mesh.Faces[j] = face;
            }

            return new PrimDisplayData { Mesh = mesh, IsRootPrim = part.IsRootPart(),
                OffsetPosition = part.OffsetPosition, OffsetRotation = part.RotationOffset,
                Scale = part.Scale,
                ShapeHash = GetMeshShapeHash(part.Shape, DetailLevel.Highest),
                MaterialHash = GetMeshMaterialHash(mesh, prim)
            };
        }

        public bool LoadTexture(UUID textureID, ref Image texture, bool removeAlpha)
        {
            if (textureID == UUID.Zero) return false;

            try
            {
                var textureAsset = _stratus.RequestAssetSync(textureID);

                texture = CSJ2K.J2kImage.FromBytes(textureAsset.Data);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
