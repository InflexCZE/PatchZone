using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ModestTree;
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

            //TODO: Hack, remove when IDataSource is part of mod manifest
            var currentlyLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(x =>
            {
                var assemblyLocation = x.Location;
                try
                {
                    return Path.GetFileName(assemblyLocation);
                }
                catch
                {
                    return string.Empty;
                }
            }).ToHashSet();

            foreach (string assemblyPath in Directory.EnumerateFiles(modContext.SourcePath, "*.dll", SearchOption.AllDirectories))
            {
                var assemblyFile = Path.GetFileName(assemblyPath);
                if(currentlyLoadedAssemblies.Contains(assemblyFile))
                {
                    //Assume all assemblies currently loaded into app domain
                    //are part of PatchZone so don't reload them from mod folder
                    continue;
                }

                var assembly = Assembly.LoadFile(assemblyPath);
                this.Assemblies.Add(assembly);
            }
        }
    }
}
