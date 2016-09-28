using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InWorldz.PrimExporter.ExpLib
{
    /// <summary>
    /// Data required to export a group
    /// </summary>
    public class GroupDisplayData
    {
        public IEnumerable<PrimDisplayData> Prims;
        public PrimDisplayData RootPrim;
        public string ObjectName;
        public string CreatorName;
    }
}
