using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InWorldz.PrimExporter.ExpLib;
using InWorldz.PrimExporter.ExpLib.ImportExport;
using OpenMetaverse;
using System.IO;

namespace PrimExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            //Args must be [userid, inv item id, formatter, primlimit, checks, outputdir]

            ExpLib.Init();

            GroupLoader.LoaderParams parms = new GroupLoader.LoaderParams
            {
                PrimLimit = Convert.ToInt32(args[3]),
                Checks = (GroupLoader.LoaderChecks)Convert.ToInt16(args[4])
            };

            GroupDisplayData data = GroupLoader.Instance.Load(UUID.Parse(args[0]), UUID.Parse(args[1]), parms);
            IExportFormatter formatter = ExportFormatterFactory.Instance.Get(args[2]);
            ExportResult res = formatter.Export(data);

            //for now we're just using the json packager directly. This should be settable in the future 
            //so that different packagers can be selected
            ThreeJSONPackager packager = new ThreeJSONPackager();
            packager.CreatePackage(res, args[5]);

            ExpLib.ShutDown();
        }
    }
}
