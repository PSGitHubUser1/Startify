﻿using Shell.Shell.Start.Tiles;
using ShellApp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;
using Windows.UI.Xaml.Controls;


namespace WPF.Helpers
{
    internal class TilesLoadHelper
    {
        static async Task<AppListEntry> GetAppByPackageFamilyNameAsync(string packageFamilyName)
        {
            var pkgManager = new PackageManager();
            var pkg = pkgManager.FindPackagesForUser("", packageFamilyName).FirstOrDefault();

            if (pkg == null) return null;

            var apps = await pkg.GetAppListEntriesAsync();
            var firstApp = apps.FirstOrDefault();
            return firstApp;
        }
        public async void TileLaunchHandler(object sender, ItemClickEventArgs e, Window window, bool runasadmin)
        {
            Tile clickedItem = e.ClickedItem as Tile;

            if (clickedItem != null)
            {
                string path = clickedItem.Path;
                string pathuwp = clickedItem.PathUWP;

                window.Hide();

                if (path != null)
                {
                    if (path != "")
                    {
                        if (runasadmin == false)
                        {
                            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        }
                        else if (runasadmin == true)
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo(path)
                            {
                                UseShellExecute = true,
                                Verb = "runas" // This will request admin privileges
                            };
                            Process.Start(startInfo);
                        }
                    }
                }
                if (pathuwp != null)
                {
                    if (pathuwp != "")
                    {
                        var app = await GetAppByPackageFamilyNameAsync(pathuwp);
                        if (app != null)
                        {
                            await app.LaunchAsync();
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("This UWP app couldn't be launched.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }


        public event EventHandler<EventArgs> CouldNotLoadTiles;
        public void LoadTileGroups(ObservableCollection<TileGroup> TileGroupscollection)
        {
            string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Startify", "PinnedTiles", "TileLayout.xml");

            if (File.Exists(configFile))
            {
                try
                {
                    XDocument doc = XDocument.Load(configFile);

                    foreach (XElement tileGroupElement in doc.Descendants("TileGroup"))
                    {
                        TileGroup tileGroup = new TileGroup
                        {
                            Name = tileGroupElement.Attribute("Name")?.Value,
                            Tiles = tileGroupElement.Descendants("Tile").Select(tileElement => new Tile
                            {
                                DisplayName = tileElement.Element("DisplayName")?.Value,
                                Path = tileElement.Element("Path")?.Value,
                                PathUWP = tileElement.Element("PathUWP")?.Value,
                                Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(uriString: IconHelper.GetFileIcon(tileElement.Element("Path")?.Value))),
                                Size = tileElement.Element("Size")?.Value,
                                IsLiveTileEnabled = tileElement.Element("IsLiveTileEnabled")?.Value,
                                TileCustomColor = tileElement.Element("TileCustomColor")?.Value
                            }).ToList()
                        };

                        TileGroupscollection.Add(tileGroup);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error loading tiles Configuration XML: " + ex.Message);
                    CouldNotLoadTiles(this, null);
                }
            }
            else
            {
                Debug.WriteLine("Tiles configuration file not found.");
                CouldNotLoadTiles(this, null);
            }
        }

    }
}
