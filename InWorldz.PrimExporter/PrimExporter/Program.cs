using System;
using InWorldz.PrimExporter.ExpLib;
using InWorldz.PrimExporter.ExpLib.ImportExport;
using OpenMetaverse;
using NDesk.Options;

namespace PrimExporter
{
    class Program
    {
        private static UUID _userId;
        private static UUID _invItemId;
        private static string _formatter;
        private static int _primLimit;
        private static int _checks;
        private static string _outputDir;
        private static string _packager;
        private static bool _help;
        private static bool _stream;

        private static OptionSet _options = new OptionSet()
        {
            { "u|userid=",      "Specifies the user who's inventory to load items from",    v => _userId = v != null ? UUID.Parse(v) : UUID.Zero },
            { "i|invitemid=",   "Specifies the inventory ID of the item to load",           v => _invItemId = v != null ? UUID.Parse(v) : UUID.Zero },
            { "f|formatter=",   "Specifies the object formatter to use " +
                "(ThreeJSONFormatter, BabylonJSONFormatter)",                               v => _formatter = v },
            { "l|primlimit=",   "Specifies a limit to the number of prims in a group",      v => _primLimit = v != null ? int.Parse(v) : 0 },
            { "c|checks=",      "Specifies the permissions checks to run",                  v => _checks = v != null ? int.Parse(v) : 0 },
            { "o|output=",      "Specifies the output directory",                           v => _outputDir = v },
            { "p|packager=",    "Specifies the packager " +
                "(ThreeJSONPackager, BabylonJSONPackager)",                                 v => _packager = v },
            { "s|stream",       "Stream XML input from STDIN",                              v => _stream = v != null },
            { "?|help",         "Prints this help message",                                 v => _help = v != null }
        };

        static void Main(string[] args)
        {
            //Args must be [userid, inv item id, formatter, primlimit, checks, outputdir]
            ExpLib.Init();

            GroupLoader.LoaderParams parms = new GroupLoader.LoaderParams
            {
                PrimLimit = Convert.ToInt32(_primLimit),
                Checks = (GroupLoader.LoaderChecks)Convert.ToInt16(_checks)
            };

            GroupDisplayData data;
            if (_stream)
            {
                string input = Console.In.ReadToEnd();
                data = GroupLoader.Instance.LoadFromXML(input, parms);
            }
            else
            {
                data = GroupLoader.Instance.Load(_userId, _invItemId, parms);
            }

            IExportFormatter formatter = ExportFormatterFactory.Instance.Get(_formatter);
            ExportResult res = formatter.Export(data);

            IPackager packager = PackagerFactory.Instance.Get(_packager);
            packager.CreatePackage(res, _outputDir);

            ExpLib.ShutDown();
        }
    }
}
