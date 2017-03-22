using System.Collections.Generic;
using FlatBuffers;
using InWorldz.PrimExporter.ExpLib;
using InWorldz.PrimExporter.ExpLib.ImportExport;
using InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBuffers;
using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace InWorldz.PrimExporter.UnitTests
{
    [TestFixture]
    public class BabylonFlatbufferSerialization
    {
        private readonly List<string> PrimCompareIgnoreList = new List<string> { "ParentGroup", "FullUpdateCounter", "TerseUpdateCounter",
                "TimeStamp", "SerializedVelocity", "InventorySerial", "Rezzed" };

        [Test]
        public void TestSerializeDeserialize()
        {
            var sops = new[] 
            {
                Util.RandomSOP("sop1", 1),
                Util.RandomSOP("sop2", 2),
                Util.RandomSOP("sop3", 3)
            };

            var group = new SceneObjectGroup(sops[0]);
            group.AddPart(sops[1]);
            group.AddPart(sops[2]);

            var loaderParams = new GroupLoader.LoaderParams();
            GroupDisplayData gdd = GroupLoader.Instance.GroupDisplayDataFromSOG(UUID.Zero, loaderParams, group, null, null, null);
            var formatter = new BabylonFlatbufferFormatter();

            var rawResult = formatter.Export(gdd);
            var result = rawResult.FaceBlob;

            CompareLogic comp = new CompareLogic
            {
                Config =
                {
                    CompareStaticFields = false,
                    CompareStaticProperties = false,
                    MembersToIgnore = PrimCompareIgnoreList
                }
            };

            var fbGroup =
                BabylonFileFlatbuffer.GetRootAsBabylonFileFlatbuffer(new ByteBuffer(result.Item1, result.Item2));
            Assert.NotNull(fbGroup);
        }
    }
}
