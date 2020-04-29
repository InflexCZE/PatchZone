using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PatchZone.Core;
using PatchZone.Hatch.Utils;
using PatchZone.Utils;

namespace PatchZone
{
    public static class Preloader
    {
        /// <summary>
        /// Invoked by Doorstop
        /// </summary>
        public static void Main()
        {
            try
            {
                GlobalLog.Default.PrintLine(DateTime.Now.ToString());
                GlobalLog.Default.PrintLine("Loading Patch Zone");

                PatchZone.Start();

                GlobalLog.Default.PrintLine("Startup inject");
                LoadGameAssembly();
                PatchGameStartup();

                GlobalLog.Default.PrintLine("Loading mods");
                PatchZone.Instance.LoadMods();

                GlobalLog.Default.PrintLine("PatchZone loading finished");
            }
            catch (Exception e)
            {
                GlobalLog.Error.PrintLine("Exception during PatchZone init:");
                GlobalLog.Error.PrintLine(e.ToString());
                Environment.Exit(1);
            }
        }

        private static void LoadGameAssembly()
        {
            var t = typeof(AfterTheEndKernel);
        }

        private static void PatchGameStartup()
        {
            var harmony = PatchZone.Instance.Harmony;
            var awakeMethod = typeof(Zenject.SceneContext).GetMethod("Awake");
            var onBeforeGameStart = AccessTools.Method(typeof(Preloader), nameof(OnBeforeGameStart));
            harmony.Patch(awakeMethod, postfix: new HarmonyMethod(onBeforeGameStart));
        }

        private static void OnBeforeGameStart()
        {
            PatchZone.Instance.OnBeforeGameStart();
        }
    }
}
