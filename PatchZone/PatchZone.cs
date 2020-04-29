using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using PatchZone.Core;
using PatchZone.Core.Mods;
using PatchZone.Hatch;
using PatchZone.Hatch.Utils;
using PatchZone.Mods;
using PatchZone.Patcher;
using PatchZone.Utils;

namespace PatchZone
{
    class PatchZone : Singleton<PatchZone>
    {
        public Harmony Harmony { get; }
        public Config Config { get; private set; }
        public List<ModContext> LoadedMods { get; } = new List<ModContext>();

        public void LoadMods()
        {
            var knownMods = this.Config.KnownMods;
            GlobalLog.Default.PrintLine("Total known mods: " + knownMods.Count);

            foreach (var modInfo in knownMods)
            {
                var modIdentifier = ModUtils.GetReadableIdentifier(modInfo);

                if(modInfo.Active == false)
                {
                    GlobalLog.Debug.PrintLine("Skipping (not active): " + modIdentifier);
                    continue;
                }

                GlobalLog.Default.PrintLine("Loading: " + modIdentifier);

                var modContext = new ModContext(modInfo);
                this.LoadedMods.Add(modContext);
            }
        }
        
        private bool GameStarted;
        public void OnBeforeGameStart()
        {
            if(this.GameStarted)
                return;

            this.GameStarted = true;

            using(GlobalLog.Default.OpenScope($"Invoking {nameof(OnBeforeGameStart)} for {this.LoadedMods.Count} mods", "Invoke done"))
            {
                try
                {
                    foreach (var modContext in this.LoadedMods)
                    {
                        modContext.Log.Log(nameof(OnBeforeGameStart));
                        modContext.Hatch.OnBeforeGameStart();
                    }
                }
                catch (Exception e)
                {
                    GlobalLog.Error.PrintLine("Error while invoking " + nameof(OnBeforeGameStart));
                    GlobalLog.Error.PrintLine(e.ToString());
                    throw;
                }
            }

            GlobalLog.Default.PrintNewLine();

            using (GlobalLog.Default.OpenScope($"Installing registered service patches for {this.LoadedMods.Count} mods", "Installation completed"))
            {
                try
                {
                    InstallServices();
                }
                catch (Exception e)
                {
                    GlobalLog.Error.PrintLine("Error while installing mod services");
                    GlobalLog.Error.PrintLine(e.ToString());
                    throw;
                }
            }
        }

        private void InstallServices()
        {
            var vanillaServices = global::Kernel.Instance.Container;
            var servicePatchStack = new Dictionary<Type, List<object>>();

            foreach(var mod in this.LoadedMods)
            {
                var servicePatches = mod.ServicePatches;

                if(servicePatches.Count > 0)
                {
                    using(mod.Log.Normal.OpenScope($"Installing {servicePatches.Count} service patches", "Done"))
                    {
                        foreach (var (service, patch) in servicePatches)
                        {
                            mod.Log.Log($"{service} -> {patch}");

                            var baseCascade = GetServicePatchStack(service);
                            var proxyService = ServicePatcher.InstantiatePatch(patch, baseCascade);
                            baseCascade.Insert(0, proxyService);
                        }
                    }
                }
            }

            List<object> GetServicePatchStack(Type key)
            {
                if (servicePatchStack.TryGetValue(key, out var baseCascade) == false)
                {
                    baseCascade = new List<object>();

                    var vanilla = vanillaServices.Resolve(key);
                    if (vanilla == null)
                    {
                        throw new Exception("Vanilla service not found: " + key);
                    }

                    baseCascade.Add(vanilla);
                    servicePatchStack.Add(key, baseCascade);
                }

                return baseCascade;
            }
           
        }


        public static void Start()
        {
            bool WaitForDebugger = false;

            while (WaitForDebugger && Debugger.IsAttached == false)
            {
                GlobalLog.Error.PrintLine("Awaiting debugger...");
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            //Note: Saves instance to Singleton
            var instance = new PatchZone();
        }

        public PatchZone()
        {
            TryConfigPath(Config.CONFIG_PATH);
            TryConfigPath(Path.Combine("PatchZone", Config.CONFIG_PATH));

            if(ReferenceEquals(this.Config, null))
            {
                this.Config = Config.Load();
            }

            this.Harmony = new Harmony("PatchZone");

            void TryConfigPath(string path)
            {
                if(ReferenceEquals(this.Config, null) == false)
                    return;

                if(File.Exists(path))
                {
                    this.Config = Config.Load(path);
                }
            }
        }
    }
}
