using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PatchZone.Core;
using PatchZone.Core.Mods;
using PatchZone.Core.Utils;
using Path = System.IO.Path;

namespace PatchZone.GUI
{
    /// <summary>
    /// Interaction logic for CreateModWindow.xaml
    /// </summary>
    public partial class CreateModWindow : Window
    {
        private Config Config;
        private string ModTemplatePath = "Assets\\ModTemplate";
        private Brush DefaultBorderBrush;

        public CreateModWindow(Config config)
        {
            this.Config = config;
            InitializeComponent();
            this.DefaultBorderBrush = this.ModNameBox.BorderBrush;
        }

        private void CreateMod(object sender, RoutedEventArgs e)
        {
            this.ModNameBox.BorderBrush = this.DefaultBorderBrush;
            this.ModGUIDBox.BorderBrush = this.DefaultBorderBrush;

            var modName = this.ModNameBox.Text;
            if(string.IsNullOrEmpty(modName))
            {
                this.ModNameBox.BorderBrush = Brushes.Red;
                return;
            }

            Guid guid;
            var guidString = this.ModGUIDBox.Text;
            if(string.IsNullOrEmpty(guidString))
            {
                guid = Guid.NewGuid();
            }
            else
            {
                if(Guid.TryParse(guidString, out guid) == false)
                {
                    this.ModGUIDBox.BorderBrush = Brushes.Red;
                    return;
                }
            }

            var modInfo = new ModInfo
            {
                Guid = guid,
                Active = true,
                DisplayName = modName
            };

            var modPath = ModUtils.GetModDirectory(modInfo);
            var modTemplatePath = Path.Combine(PatchZoneCore.PatchZoneInstallationPath, ModTemplatePath);
            CopyDirectory(modTemplatePath, modPath);

            bool makeNextUpper = false;
            var escapedModName = new StringBuilder();
            foreach(char x in modName)
            {
                var toUpper = makeNextUpper;
                makeNextUpper = false;

                if(char.IsNumber(x))
                {
                    escapedModName.Append(x);
                }
                else if('a' <= x && x <= 'z')
                {
                    escapedModName.Append(toUpper ? char.ToUpper(x) : x);
                }
                else if('A' <= x && x <= 'Z')
                {
                    escapedModName.Append(x);
                }
                else if (char.IsWhiteSpace(x) || toUpper)
                {
                    makeNextUpper = true;
                }
            }

            if(escapedModName.Length == 0)
            {
                escapedModName.Append("PatchZoneMod");
            }

            var gameRoot = PatchZoneCore.GameInstallationPath;
            if(gameRoot[gameRoot.Length - 1] != Path.DirectorySeparatorChar)
            {
                gameRoot += Path.DirectorySeparatorChar;
            }

            FixupNewModInfo(modPath, new Dictionary<string, string>
            {
                {"___GAME_ROOT___",     gameRoot                  },
                {"___MOD_NAME_FULL___", modName                   },
                {"___MOD_GUID___",      guid.ToString()           },
                {"___MOD_NAME___",      escapedModName.ToString() },
            });

            this.Config.KnownMods.Add(modInfo);

            if(this.StartVSBox.IsChecked ?? false)
            {
                try
                {
                    var solutionFiles = Directory.EnumerateFiles(modPath, "*.sln", SearchOption.TopDirectoryOnly);
                    Process.Start(solutionFiles.First());
                }
                catch
                { }
            }

            this.Close();
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destinationDir));
            }

            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourceDir, destinationDir), true);
            }
        }

        private static void FixupNewModInfo(string modPath, Dictionary<string, string> replacePatterns)
        {
            modPath = Path.GetFullPath(modPath);
            var modPathOffset = modPath.Length;

            //Note: Fix directories first so that subsequent file fixups
            //      doesn't fail cos of corrections in subdirectory names
            FixupDirectoryNames();
            FixupFileNames();
            FixupFileContents();

            void FixupFileNames()
            {
                foreach(string file in Directory.GetFiles(modPath, "*", SearchOption.AllDirectories))
                {
                    if(FixupSubPath(file, out var newFile))
                    {
                        File.Move(file, newFile);
                    }
                }
            }

            void FixupDirectoryNames()
            {
                FixNextDirectory:
                foreach(string directory in Directory.EnumerateDirectories(modPath, "*", SearchOption.AllDirectories))
                {
                    if(FixupSubPath(directory, out var newDirectory))
                    {
                        Directory.Move(directory, newDirectory);
                        
                        //Other directory results are now potentially invalid. Make new query
                        goto FixNextDirectory;
                    }
                }
            }

            void FixupFileContents()
            {
                foreach(string file in Directory.GetFiles(modPath, "*", SearchOption.AllDirectories))
                {
                    var fileContent = File.ReadAllText(file);
                    if(FixupString(ref fileContent))
                    {
                        File.WriteAllText(file, fileContent);
                    }
                }
            }

            bool FixupSubPath(string pathToFix, out string newPath)
            {
                var subPath = pathToFix.Substring(modPathOffset);

                if (FixupString(ref subPath))
                {
                    newPath = modPath + subPath;
                    return true;
                }

                newPath = null;
                return false;
            }

            bool FixupString(ref string str)
            {
                bool changed = false;
                foreach(var (keyword, replace) in replacePatterns)
                {
                    var newString = str.Replace(keyword, replace);

                    if (ReferenceEquals(str, newString) == false)
                    {
                        changed = true;
                        str = newString;
                    }
                }

                return changed;
            }
        }
    }
}
