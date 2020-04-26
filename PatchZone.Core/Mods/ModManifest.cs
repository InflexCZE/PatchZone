using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using PatchZone.Hatch;

namespace PatchZone.Core.Mods
{
    [XmlRoot("PatchZoneManifest")]
    public class ModManifest
    {
        public Guid GUID;
        public string Icon;
        public string DisplayName;
        public string Description;

        //TODO:Fix
        /*
        [XmlElement("Source")]
        [XmlArrayItem("ManagedDllSource", typeof(ManagedDllSource))]
        [XmlIgnore]
        public List<IDataSource> DataSources = new List<IDataSource>();
        */
    }
}
