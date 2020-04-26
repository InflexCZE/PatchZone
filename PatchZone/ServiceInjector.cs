using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ECS;
using HarmonyLib;
using ModestTree;
using PatchZone.Hatch;
using PatchZone.Hatch.Annotations;
using PatchZone.Hatch.Utils;
using PatchZone.Patcher;
using PatchZone.Utils;
using Service.Achievement;
using Service.Building;
using Service.Localization;
using Service.Street;
using Service.UserWorldTasks;
using TMPro;
using Zenject;

namespace PatchZone
{
#if false
    public class ServiceInstaller
    {
        private static DiContainer GameCore => global::Kernel.Instance.Container;

        public static void InstallModServices()
        {
            try
            {
                var servicePatchStack = new Dictionary<Type, List<object>>();

                foreach(var mod in PatchZone.Instance.LoadedMods)
                foreach(var (service, patch) in mod.ServicePatches)
                {
                    var baseCascade = GetServicePatchStack(service);
                    var proxyService = ServicePatcher.InstantiatePatch(patch, baseCascade);
                    baseCascade.Add(proxyService);
                }

                List<object> GetServicePatchStack(Type key)
                {
                    if (servicePatchStack.TryGetValue(key, out var baseCascade) == false)
                    {
                        baseCascade = new List<object>();

                        var vanilla = GameCore.Resolve(key);
                        if(vanilla == null)
                        {
                            throw new Exception("Vanilla service not found: " + key);
                        }

                        baseCascade.Add(vanilla);
                        servicePatchStack.Add(key, baseCascade);
                    }

                    return baseCascade;
                }
            }
            catch (Exception e)
            {
                GlobalLog.Error.PrintLine("Error while installing mod services");
                GlobalLog.Error.PrintLine(e.ToString());
                throw;
            }
        }

        private static void InstallModServicesImpl()
        {
            
            try
            {
                //using (Log.Debug.OpenScope("Installing services", string.Empty))
                {
                    //Install<IEntityManager, CreativeEntityManager>();
                    //Install<IBuildingService, CreativeBuildingService>();
                    //Install<IAchievementService, CreativeAchievementService>();
                    //Install<ILocalizationService, CreativeLocalizationService>();
                    //Install<IUserWorldTasksService, CreativeUserWorldTaskService>();
                    Install<ILocalizationService, TestService>();
                    Install<ILocalizationService, TestService2>();
                }

                //Log.Default.PrintLine("Install completed");
            }
            catch (Exception e)
            {
                //Log.Error.PrintLine("Exception during service install:");
                //Log.Error.PrintLine(e.ToString());
                throw;
            }
        }

        private static Dictionary<Type, List<object>> m_servicePatchStack = new Dictionary<Type, List<object>>();

        private static void Install<TService, TImpl>()
        {
            //using (Log.Debug.OpenScope($"Installing {typeof(TImpl)} -> {typeof(TService)} ... ", "Done"))
            {
                var vanilla = GameCore.Resolve<TService>();

                var key = typeof(TService);
                if(m_servicePatchStack.TryGetValue(key, out var baseCascade) == false)
                {
                    baseCascade = new List<object>();
                    baseCascade.Add(vanilla);

                    m_servicePatchStack.Add(key, baseCascade);
                }

                File.WriteAllText("preloader.txt", typeof(TImpl).FullName);

                var patch = ServicePatcher.InstantiatePatch(typeof(TImpl), baseCascade);
                baseCascade.Insert(0, patch);

                //var installMethod = typeof(TImpl).GetMethod("Install", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                //installMethod.Invoke(null, new object[] { Harmony, vanilla, typeof(TService) });
            }
        }
    }

    public class TestService : ProxyService<TestService, ILocalizationService>
    {
        [LogicProxy]
        string GetLocalization(
            Keys locaKey,
            Dictionary<string, string> replacements = null,
            ReplacementStyle replacementStyle = null)
        {
            var str = this.Vanilla.GetLocalization(locaKey, replacements, replacementStyle);

            if(
                locaKey == Keys.Common_EarlyAccessDisclaimer ||
                locaKey == Keys.Common_ClosedBetaDisclaimer ||
                locaKey == Keys.Common_Watermarks_ClosedBeta ||
                locaKey == Keys.Common_Watermarks_EarlyAccess
                )
            {
                str += " SUCCESS";
            }

            return str;
        }

        [LogicProxy]
        void Localize(
            Keys locaKey,
            TextMeshProUGUI textOutput,
            Dictionary<string, string> replacements = null,
            ReplacementStyle replacementStyle = null)
        {
            this.Vanilla.Localize(locaKey, textOutput, replacements, replacementStyle);

            if (locaKey == Keys.Common_EarlyAccessDisclaimer ||
                locaKey == Keys.Common_ClosedBetaDisclaimer ||
                locaKey == Keys.Common_Watermarks_ClosedBeta ||
                locaKey == Keys.Common_Watermarks_EarlyAccess
            )
            {
                var text = textOutput.text;
                text += " SUCCESS";
                textOutput.text = text;
            }
        }
    }

    public class TestService2 : ProxyService<TestService2, ILocalizationService>
    {
        [LogicProxy]
        string GetLocalization(
            Keys locaKey,
            Dictionary<string, string> replacements = null,
            ReplacementStyle replacementStyle = null)
        {
            var str = this.Vanilla.GetLocalization(locaKey, replacements, replacementStyle);

            if (
                locaKey == Keys.Common_EarlyAccessDisclaimer ||
                locaKey == Keys.Common_ClosedBetaDisclaimer ||
                locaKey == Keys.Common_Watermarks_ClosedBeta ||
                locaKey == Keys.Common_Watermarks_EarlyAccess
            )
            {
                str += " SUCCESS";
            }

            return str;
        }

        [LogicProxy]
        void Localize(
            Keys locaKey,
            TextMeshProUGUI textOutput,
            Dictionary<string, string> replacements = null,
            ReplacementStyle replacementStyle = null)
        {
            this.Vanilla.Localize(locaKey, textOutput, replacements, replacementStyle);

            if (locaKey == Keys.Common_EarlyAccessDisclaimer ||
                locaKey == Keys.Common_ClosedBetaDisclaimer ||
                locaKey == Keys.Common_Watermarks_ClosedBeta ||
                locaKey == Keys.Common_Watermarks_EarlyAccess
            )
            {
                var text = textOutput.text;
                text += " SUCCESS";
                textOutput.text = text;
            }
        }
    }
#endif
}
