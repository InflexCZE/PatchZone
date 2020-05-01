using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public const string PackageTypeZip = "application/x-zip-compressed";

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

        private Brush DefaultBorderBrush;

        public AddModWindow(Config config)
        {
            this.Config = config;
            this.DataContext = this;

            InitializeComponent();

            this.DefaultBorderBrush = this.ModURLBox.BorderBrush;

            this.ModTypeCombo.Items.Add(new ComboBoxItem() {Content = "Local mod", Tag = ModType.Local });
            this.ModTypeCombo.Items.Add(new ComboBoxItem() {Content = "Remote mod", Tag = ModType.Remote });
            this.ModTypeCombo.SelectedIndex = 1;
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
                    RefreshRemoteMod(false);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void AddMod(object sender, RoutedEventArgs e)
        {
            switch(this.SelectedModType)
            {
                case ModType.Local:
                    foreach(ModInfo selected in this.LocalModsView.SelectedItems)
                    {
                        this.Config.KnownMods.Add(selected);
                    }
                    break;

                case ModType.Remote:
                    if(await InstallRemoteMod() == false)
                    {
                        return;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            Close();
        }

        private async Task<bool> InstallRemoteMod()
        {
            var mod = this.SelectedRemoteRelease;
            var modDirectory = ModUtils.GetModDirectory(mod.Manifest);

            try
            {
                if(NotifyBackgroundTaskRunning(true) == false)
                {
                    throw new Exception("Mod fetching is currently in progress");
                }

                try
                {
                    this.ProgressText.Text = "Clearing mod directory";

                    var prevInstallationIndex = this.Config.KnownMods.FindIndex(x => x.Guid == this.SelectedRemoteRelease.Manifest.Guid);

                    ModInfo prevModInfo = null;
                    if(prevInstallationIndex >= 0)
                    {
                        prevModInfo = this.Config.KnownMods[prevInstallationIndex];
                        this.Config.KnownMods.RemoveAt(prevInstallationIndex);
                    }

                    await Task.Run(() =>
                    {
                        if(Directory.Exists(modDirectory))
                        {
                            Directory.Delete(modDirectory, true);
                        }

                        Directory.CreateDirectory(modDirectory);
                    });


                    this.ProgressText.Text = "Downloading package";
                    var package = this.SelectedRemoteRelease.Assets.First(x => x.ContentType == PackageTypeZip);
                    await DownloadModPackageAsync(package.BrowserDownloadUrl, modDirectory);

                    this.ProgressText.Text = "Verifying package integrity";
                    var modInfo = await Task.Run(() =>
                    {
                        var manifestPath = ModUtils.GetManifestPath(modDirectory);
                        var manifest = XML.Deserialize<ModManifest>(manifestPath);

                        var expectedManifest = this.SelectedRemoteRelease.Manifest;
                        if(manifest.Guid != expectedManifest.Guid)
                        {
                            throw new Exception("Mod guid doesn't match");
                        }

                        return ModUtils.BuildModInfoFromManifest(manifest, prevModInfo?.Active ?? false);
                    });

                    if(prevInstallationIndex >= 0)
                    {
                        this.Config.KnownMods.Insert(prevInstallationIndex, modInfo);
                    }
                    else
                    {
                        this.Config.KnownMods.Add(modInfo);
                    }
                }
                finally
                {
                    //Kill any running queries
                    this.RemoteUrlIndex++;
                    NotifyBackgroundTaskRunning(false);
                }
            }
            catch(Exception e)
            {
                this.ModDescriptionText.Text = e.Message;
                return false;
            }

            return true;
        }


        private void RemoteModUrlChanged(object sender, TextChangedEventArgs e)
        {
            RefreshRemoteMod(true);
        }
        
        private int RemoteUrlIndex;
        private Release SelectedRemoteRelease;
        private async void RefreshRemoteMod(bool addDelay)
        {
            this.RemoteUrlIndex++;
            this.ModURLBox.BorderBrush = this.DefaultBorderBrush;

            var match = Regex.Match(this.ModURLBox.Text.Trim(), @"github\.com\/([^\/]+\/[^\/]+)");
            if(match.Success == false)
            {
                this.ModURLBox.BorderBrush = Brushes.Red;
                return;
            }

            var taskIndex = this.RemoteUrlIndex;
            var userRepositoryPair = match.Groups[1].Value;

            if(addDelay)
            {
                //Don't start downloading if user is still typing
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            if(taskIndex != this.RemoteUrlIndex)
                return;

            while(NotifyBackgroundTaskRunning(true) == false)
            {
                if (taskIndex != this.RemoteUrlIndex)
                    return;

                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            try
            {
                if(string.IsNullOrEmpty(this.ModNameText.Text))
                {
                    this.ModNameText.Text = userRepositoryPair;
                }

                this.ProgressText.Text = "Querying releases";
                var latestRelease = await DownloadLatestReleaseAsync(userRepositoryPair);

                if(taskIndex != this.RemoteUrlIndex)
                    return;

                this.ProgressText.Text = "Downloading manifest";
                var modManifest = await DownloadModManifestAsync(latestRelease, userRepositoryPair);

                if(taskIndex != this.RemoteUrlIndex)
                    return;

                latestRelease.Manifest = modManifest;

                this.SelectedRemoteRelease = latestRelease;
                this.ModNameText.Text = modManifest.DisplayName;
                this.ModDescriptionText.Text = modManifest.Description;

            }
            catch(Exception exception)
            {
                if(taskIndex != this.RemoteUrlIndex)
                    return;

                this.ModDescriptionText.Text = "Error occured during mod download:" + Environment.NewLine + exception.Message;
            }
            finally
            {
                NotifyBackgroundTaskRunning(false);
            }
        }

        private bool BackgroundTaskRunning;

        private bool NotifyBackgroundTaskRunning(bool running)
        {
            if(this.BackgroundTaskRunning && running)
                return false;

            this.BackgroundTaskRunning = running;

            var enabled = running == false;
            this.AddButton.IsEnabled = enabled;
            this.ModTypeCombo.IsEnabled = enabled;
            this.RefreshButton.IsEnabled = enabled;
            this.ProgressIndicatorRow.Height = running ? GridLength.Auto : new GridLength(0);

            return true;
        }

        private async Task<ModManifest> DownloadModManifestAsync(Release release, string userRepositoryPair)
        {
            var tagUrl = HttpUtility.UrlEncode(release.TagName);
            var url = $"https://raw.githubusercontent.com/{userRepositoryPair}/{tagUrl}/{ModUtils.ManifestName}";
            var manifestString = await DownloadStringAsync(url).ConfigureAwait(false);
            return XML.Deserialize<ModManifest>(new StringReader(manifestString));
        }

        private class Release
        {
            public class Asset
            {
                [JsonProperty("content_type")]
                public string ContentType;

                [JsonProperty("browser_download_url")]
                public string BrowserDownloadUrl;
            }

            [JsonProperty("assets")]
            public Asset[] Assets;

            [JsonProperty("published_at")]
            public DateTime PublishedAt;

            [JsonProperty("tag_name")]
            public string TagName;

            public ModManifest Manifest;
        }

        private async Task<Release> DownloadLatestReleaseAsync(string userRepositoryPair)
        {
            var baseUrl = $"https://api.github.com/repos/{userRepositoryPair}/";

            var releasesJSON = await DownloadStringAsync(baseUrl + "releases").ConfigureAwait(false);
            var releases = JsonConvert.DeserializeObject<Release[]>(releasesJSON);

            var latestDate = releases.Max(x => x.PublishedAt);
            var latestRelease = releases.First(x => x.PublishedAt == latestDate);

            if(latestRelease.Assets.Any(x => x.ContentType == PackageTypeZip) == false)
            {
                throw new Exception("Release contains no zip packages");
            }

            return latestRelease;
        }

        private const string AgentIdentifier = "github.com_InflexCZE_PatchZone";

        private async Task<string> DownloadStringAsync(string url)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.TryParseAdd(AgentIdentifier);

                return await client.GetStringAsync(url).ConfigureAwait(false);
            }
        }

        private async Task DownloadModPackageAsync(string url, string path)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.TryParseAdd(AgentIdentifier);

                var dataStream = await client.GetStreamAsync(url).ConfigureAwait(false);
                using(var source = new ZipArchive(dataStream))
                {
                    source.ExtractToDirectory(path);
                }
            }
        }
    }
}
