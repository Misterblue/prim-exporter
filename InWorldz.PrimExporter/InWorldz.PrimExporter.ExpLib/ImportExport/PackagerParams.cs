using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Parameters that affect how the packager creates an export package
    /// </summary>
    public class PackagerParams
    {
        /// <summary>
        /// Don't create a random UUID based folder, instead dump the files directly
        /// into the output folder
        /// </summary>
        public bool Direct { get; set; }
    }
}
