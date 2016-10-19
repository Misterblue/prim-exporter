using System;
using System.IO;
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
        private static string _xmlFile;
        private static bool _direct;

        private static OptionSet _options = new OptionSet()
        {
            { "u|userid=",      "Specifies the user who's inventory to load items from",    v => _userId = v != null ? UUID.Parse(v) : UUID.Zero },
            { "i|invitemid=",   "Specifies the inventory ID of the item to load",           v => _invItemId = v != null ? UUID.Parse(v) : UUID.Zero },
            { "f|formatter=",   "Specifies the object formatter to use " +
                "(ThreeJSONFormatter, BabylonJSONFormatter)",                               v => _formatter = v },
            { "l|primlimit=",   "Specifies a limit to the number of prims in a group",      v => _primLimit = v != null ? int.Parse(v) : 0 },
            { "c|checks=",      "Specifies the permissions checks to run",                  v => _checks = v != null ? int.Parse(v) : 0 },
            { "o|output=",      "Specifies the output directory",                           v => _outputDir = v },
            { "d|direct",       "Dump the generated files directly in the output dir",      v => _direct = v != null },
            { "p|packager=",    "Specifies the packager " +
                "(ThreeJSONPackager, BabylonJSONPackager)",                                 v => _packager = v },
            { "s|stream",       "Stream XML input from STDIN",                              v => _stream = v != null },
            { "x|xmlfile=",      "Open the given XML file for input",                        v => _xmlFile = v },
            { "?|help",         "Prints this help message",                                 v => _help = v != null }
        };

        static int Main(string[] args)
        {
            try
            {
                var extra = _options.Parse(args);
                if (extra.Count != 0)
                {
                    Console.Error.WriteLine($"Invalid option: {extra[0]}");
                    PrintUsage();
                    return 1;
                }
            }
            catch (OptionException)
            {
                PrintUsage();
                return 2;
            }

            int errcode;
            if (!CheckBasicOptions(args, out errcode)) return errcode;

            GroupLoader.LoaderParams parms = new GroupLoader.LoaderParams
            {
                PrimLimit = Convert.ToInt32(_primLimit),
                Checks = (GroupLoader.LoaderChecks)Convert.ToInt16(_checks)
            };

            ExpLib.Init();

            GroupDisplayData data;
            if (_stream)
            {
                string input = Console.In.ReadToEnd();
                data = GroupLoader.Instance.LoadFromXML(input, parms);
            }
            else if (_xmlFile != null)
            {
                string input = File.ReadAllText(_xmlFile);
                data = GroupLoader.Instance.LoadFromXML(input, parms);
            }
            else if (_userId != UUID.Zero && _invItemId != UUID.Zero)
            {
                data = GroupLoader.Instance.Load(_userId, _invItemId, parms);
            }
            else
            {
                Console.Error.WriteLine("Either stream or userid and invitemid are required parameters");
                PrintUsage();
                return 5;
            }

            IExportFormatter formatter = ExportFormatterFactory.Instance.Get(_formatter);
            ExportResult res = formatter.Export(data);

            PackagerParams pp = new PackagerParams {Direct = _direct};

            IPackager packager = PackagerFactory.Instance.Get(_packager);
            packager.CreatePackage(res, _outputDir, pp);

            ExpLib.ShutDown();

            return 0;
        }

        private static bool CheckBasicOptions(string[] args, out int main)
        {
            if (args.Length == 0 || _help)
            {
                PrintUsage();
                {
                    main = 0;
                    return false;
                }
            }

            if (_formatter == null)
            {
                Console.Error.WriteLine("formatter option is required");
                PrintUsage();
                {
                    main = 3;
                    return false;
                }
            }

            if (_packager == null)
            {
                Console.Error.WriteLine("packager option is required");
                PrintUsage();
                {
                    main = 4;
                    return false;
                }
            }

            if (_outputDir == null)
            {
                Console.Error.WriteLine("output option is required");
                PrintUsage();
                {
                    main = 5;
                    return false;
                }
            }

            main = 0;
            return true;
        }

        private static void PrintUsage()
        {
            Console.Out.WriteLine("Usage:");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
