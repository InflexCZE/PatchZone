using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PatchZone.Core.Utils
{
    public static class XML
    {
        public static T Deserialize<T>(string path)
        {
            using(var file = new FileStream(path, FileMode.Open))
            {
                return Deserialize<T>(file);
            }
        }

        public static T Deserialize<T>(Stream stream)
        {
            var xml = new XmlSerializer(typeof(T));
            return (T) xml.Deserialize(stream);
        }

        public static T Deserialize<T>(TextReader reader)
        {
            var xml = new XmlSerializer(typeof(T));
            return (T)xml.Deserialize(reader);
        }

        public static void Serialize<T>(string path, T data)
        {
            using (var file = new FileStream(path, FileMode.Create))
            {
                Serialize(file, data);
            }
        }

        public static void Serialize<T>(Stream stream, T data)
        {
            var xml = new XmlSerializer(typeof(T));
            xml.Serialize(stream, data);
        }
    }
}
