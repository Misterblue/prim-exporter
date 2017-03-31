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

        public class LoaderParams
        {
            public int PrimLimit;
            public LoaderChecks Checks;
        }

        

        private static readonly GroupLoader instance = new GroupLoader();

        private readonly Data.Assets.Stratus.StratusAssetClient _stratus;
        private InventoryStorage _inv;
        private LegacyMysqlInventoryStorage _legacyInv;
        private readonly CassandraMigrationProviderSelector _invSelector;
        private readonly MeshmerizerR _renderer = new MeshmerizerR();
        private Dictionary<UUID, string> _usernameCache = new Dictionary<UUID,string>();
        private readonly ObjectHasher _objectHasher = new ObjectHasher();

        static GroupLoader()
        {
            
        }

        private GroupLoader()
        {
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            IniConfigSource config = new IniConfigSource(System.IO.Path.Combine(dir, "Halcyon.ini"));
            var settings = new ConfigSettings();
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
                return GroupLoader.instance;
            }
        }

        public GroupDisplayData Load(UUID userId, UUID itemId, GroupLoader.LoaderParams parms)
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

        public GroupDisplayData LoadFromXML(string xmlData, GroupLoader.LoaderParams parms)
        {
            SceneObjectGroup sog = SceneXmlLoader.DeserializeGroupFromXml2(xmlData);
            return GroupDisplayDataFromSOG(UUID.Zero, parms, sog, null, string.Empty, null);
        }

        public GroupDisplayData GroupDisplayDataFromSOG(UUID userId, GroupLoader.LoaderParams parms, SceneObjectGroup sog,
            IInventoryStorage inv, string userName, InventoryItemBase item)
        {
            if (((parms.Checks & LoaderChecks.PrimLimit) != 0) && sog.GetParts().Count > parms.PrimLimit)
            {
                throw new Exceptions.PrimExporterPermissionException("Object contains too many prims");
            }

            HashSet<UUID> fullPermTextures = CollectFullPermTexturesIfNecessary(ref userId, parms, inv);

            PrimDisplayData rootPrim = null;
            List<PrimDisplayData> groupData = new List<PrimDisplayData>();
            foreach (SceneObjectPart part in sog.GetParts())
            {
                if (((parms.Checks & LoaderChecks.UserMustBeCreator) != 0) && part.CreatorID != userId)
                {
                    throw new Exceptions.PrimExporterPermissionException("You are not the creator of all parts");
                }

                PrimDisplayData pdd = this.ExtractPrimMesh(part, parms, fullPermTextures);
                if (pdd == null)
                {
                    throw new NullReferenceException("Prim mesh could not be loaded");
                }

                groupData.Add(pdd);
                if (pdd.IsRootPrim) rootPrim = pdd;
            }

            //assign parent to all prims
            foreach (var part in groupData)
            {
                if (!part.IsRootPrim)
                {
                    part.Parent = rootPrim;
                }
            }

            //sort prims by link number
            var sortedPrims = groupData.OrderBy(g => g.LinkNum);

            return new GroupDisplayData {Prims = sortedPrims, RootPrim = rootPrim, CreatorName = userName,
                ObjectName = sog.Name};
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

        private HashSet<UUID> CollectFullPermTexturesIfNecessary(ref UUID userId, GroupLoader.LoaderParams parms, OpenSim.Data.IInventoryStorage inv)
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

        public PrimDisplayData ExtractPrimMesh(SceneObjectPart part, GroupLoader.LoaderParams parms, HashSet<UUID> fullPermTextures)  
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


            var pos = part.IsRootPart() ? part.RawGroupPosition : part.OffsetPosition;

            return new PrimDisplayData { Mesh = mesh, IsRootPrim = part.IsRootPart(),
                OffsetPosition = pos, OffsetRotation = part.RotationOffset,
                Scale = part.Scale,
                ShapeHash = _objectHasher.GetMeshShapeHash(part.Shape, DetailLevel.Highest),
                MaterialHash = _objectHasher.GetMeshMaterialHash(mesh, prim),
                LinkNum = part.LinkNum
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
