using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PatchZone.Core.Mods;
using PatchZone.Hatch;

namespace PatchZone.Mods
{
    public interface IDataSource
    {
        void Initialize(ModContext modContext);

        IEnumerable<Assembly> ManagedAssemblies { get; }
    }

    public class ManagedDllSource : IDataSource
    {
        private List<Assembly> Assemblies;

        public IEnumerable<Assembly> ManagedAssemblies => this.Assemblies;

        public void Initialize(ModContext modContext)
        {
            this.Assemblies = new List<Assembly>();

            foreach (string assemblyPath in Directory.EnumerateFiles(modContext.SourcePath, "*.dll", SearchOption.AllDirectories))
            {
                var assembly = Assembly.LoadFile(assemblyPath);
                this.Assemblies.Add(assembly);
            }
        }
    }
}
