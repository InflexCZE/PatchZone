using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using PatchZone.Core;

namespace PatchZone.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<ModInfo> KnownModsData
        {
            get { return this.Config.KnownMods.Where(x => x.Active == false); }
        }

        public IEnumerable<ModInfo> ActiveModsData
        {
            get { return this.Config.KnownMods.Where(x => x.Active); }
        }

        private Config Config;

        public MainWindow()
        {
            this.Config = Config.Load();

            this.DataContext = this;
            InitializeComponent();

            bool blockNextChange = false;
            this.KnownModsView.SelectionChanged += (a, b) =>
            {
                if(blockNextChange)
                    return;

                blockNextChange = true;
                this.ActiveModsView.UnselectAll();
                blockNextChange = false;
            };

            this.ActiveModsView.SelectionChanged += (a, b) =>
            {
                if (blockNextChange)
                    return;

                blockNextChange = true;
                this.KnownModsView.UnselectAll();
                blockNextChange = false;
            };

            this.Closing += (_, __) =>
            {
                this.Config.Save();
            };
        }

        private Guid[] SelectedMods
        {
            get
            {
                var selectedItems = this.KnownModsView.SelectedItems;
                if(selectedItems.Count == 0)
                {
                    selectedItems = this.ActiveModsView.SelectedItems;
                }

                if(selectedItems.Count == 0)
                {
                    return Array.Empty<Guid>();
                }

                return selectedItems.Cast<ModInfo>().Select(x => x.Guid).ToArray();
            }
        }

        private void ActivateMod(object sender, MouseButtonEventArgs e)
        {
            foreach(var mod in this.SelectedMods)
            {
                SetModActive(mod, true);
            }
        }

        private void ActivateAllMods(object sender, MouseButtonEventArgs e)
        {
            foreach (var mod in this.KnownModsData.Select(x => x.Guid).ToArray())
            {
                SetModActive(mod, true);
            }
        }

        private void MoveModUp(object sender, MouseButtonEventArgs e)
        {
            //TODO:
        }

        private void MoveAllModsUp(object sender, MouseButtonEventArgs e)
        {
            //TODO:
        }

        private void MoveAllModsDown(object sender, MouseButtonEventArgs e)
        {
            //TODO:
        }

        private void MoveModDown(object sender, MouseButtonEventArgs e)
        {
            //TODO:
        }

        private void DeactivateAllMods(object sender, MouseButtonEventArgs e)
        {
            foreach(var mod in this.ActiveModsData.Select(x => x.Guid).ToArray())
            {
                SetModActive(mod, false);
            }
        }

        private void DeactivateMod(object sender, MouseButtonEventArgs e)
        {
            foreach(var mod in this.SelectedMods)
            {
                SetModActive(mod, false);
            }
        }

        private void SetModActive(Guid modId, bool active, bool notifyChange = true)
        {
            var mod = this.Config.KnownMods.Find(x => x.Guid == modId);
            if(mod != null)
            {
                mod.Active = active;
            }

            if(notifyChange)
            {
                NotifyConfigurationChanged();
            }
        }

        private void NotifyConfigurationChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KnownModsData)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveModsData)));
        }

        private void AddMod(object sender, RoutedEventArgs e)
        {
            var window = new AddModWindow(this.Config);
            window.Owner = this;
            window.Closed += (_, __) =>
            {
                NotifyConfigurationChanged();
            };

            window.Show();
        }
    }
}
