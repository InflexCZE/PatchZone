using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for AddModWindow.xaml
    /// </summary>
    public partial class AddModWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Config Config { get; }

        enum ModType
        {
            Local,
            Remote
        }

        public IEnumerable<ModInfo> NewLocalMods
        {
            get
            {
                if(Directory.Exists(ModUtils.ModsStorageRoot) == false)
                    yield break;

                //TODO: Move I/O operations to async Task
                foreach(string modDir in Directory.GetDirectories(ModUtils.ModsStorageRoot))
                {
                    var manifestPath = ModUtils.GetManifestPath(modDir);

                    if(File.Exists(manifestPath) == false)
                        continue;

                    ModInfo modInfo;
                    try
                    {
                        var manifest = XML.Deserialize<ModManifest>(manifestPath);

                        if(this.Config.KnownMods.Any(x => x.Guid == manifest.Guid))
                            continue;

                        modInfo = ModUtils.BuildModInfoFromManifest(manifest);

                        var modPath = Path.GetFullPath(modDir);
                        var expectedModPath = Path.GetFullPath(ModUtils.GetModDirectory(modInfo));
                        if (modPath != expectedModPath)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }

                    yield return modInfo;
                }
            }
        }

        private ModType SelectedModType
        {
            get
            {
                return (ModType) ((ComboBoxItem)this.ModTypeCombo.SelectedValue).Tag;
            }
        }

        public AddModWindow(Config config)
        {
            this.Config = config;
            this.DataContext = this;

            InitializeComponent();

            this.ModTypeCombo.Items.Add(new ComboBoxItem() {Content = "Local mod", Tag = ModType.Local });
            this.ModTypeCombo.Items.Add(new ComboBoxItem() {Content = "Remote mod", Tag = ModType.Remote });
            this.ModTypeCombo.SelectedIndex = 0;
        }

        private void OnPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void OnModTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            var currentArea = this.SelectedModType == ModType.Local ? this.LocalModArea : this.RemoteModArea;
            var otherArea   = this.SelectedModType != ModType.Local ? this.LocalModArea : this.RemoteModArea;

            otherArea.Height = new GridLength(0);
            currentArea.Height = new GridLength(1, GridUnitType.Star);
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            switch(this.SelectedModType)
            {
                case ModType.Local:
                    OnPropertyChanged(nameof(NewLocalMods));
                    break;

                case ModType.Remote:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddMod(object sender, RoutedEventArgs e)
        {
            foreach(ModInfo selected in this.LocalModsView.SelectedItems)
            {
                this.Config.KnownMods.Add(selected);
            }

            Close();
        }
    }
}
