using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using PatchZone.Core.Utils;

namespace PatchZone.Core
{
    public class Config
    {
        public static string CONFIG_PATH => "PatchZone.config";

        public List<ModInfo> KnownMods = new List<ModInfo>();

        public static Config Load(string path = null)
        {
            if(path == null)
            {
                path = CONFIG_PATH;
            }

            if(File.Exists(path) == false)
            {
                return new Config();
            }

            return XML.Deserialize<Config>(path);
        }

        public void Save(string path = null)
        {
            XML.Serialize(path ?? CONFIG_PATH, this);
        }
    }

    public class ModInfo
    {
        public Guid Guid { get; set; }
        public bool Active { get; set; }
        //public string Version { get; set; }
        public string DisplayName { get; set; }
    }
}
