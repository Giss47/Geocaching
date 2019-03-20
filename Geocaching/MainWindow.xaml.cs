﻿using Geocaching.Data;
using Geocaching.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Geocaching
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key
        private const string applicationId = "ApHORC4egk6ExJWI2PwXMPFrLXa89u0Z5kUo05q-foI9r90BgdG8dqrtDyG8Nl31";

        private Dictionary<string, SolidColorBrush> colors = new Dictionary<string, SolidColorBrush> {
            ["Blue"] = new SolidColorBrush(Colors.Blue),
            ["Gray"] = new SolidColorBrush(Colors.Gray),
            ["Red"] = new SolidColorBrush(Colors.Red),
            ["Green"] = new SolidColorBrush(Colors.Green),
            ["Black"] = new SolidColorBrush(Colors.Black)
        };

        private AppDbContext db = new AppDbContext();

        private List<Pushpin> personPins = new List<Pushpin>();
        private List<Pushpin> cachePins = new List<Pushpin>();
        private int ActivePinPersonID = 0;

        private MapLayer layer;

        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.
        private Location latestClickLocation;

        private Location gothenburg = new Location(57.719021, 11.991202);

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }

            CreateMap();
            UpdateMap();
            // Load data from database and populate map here.
        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            Point? mapStartPosition = null;
            Point? mapEndPosition = null;

            // This will start tracking the pointer's position by giving mapStartPosition a value
            MouseDown += (sender, e) =>
            {
                mapStartPosition = e.GetPosition(this);
            };

            // This will occur when the mouse is released and if the pointer hasn't moved.
            // What this gives, is that if you select a Persons pin and then move the map, 
            // OnMapLeftClick will never be called. But if you just click the map, it will.
            MouseUp += (sender, e) =>
            {
                mapEndPosition = e.GetPosition(this);

                if (mapStartPosition != null && mapStartPosition.Value == mapEndPosition.Value)
                {
                    var point = e.GetPosition(this);
                    latestClickLocation = map.ViewportPointToLocation(point);

                    if (e.LeftButton == MouseButtonState.Released)
                    {
                        OnMapLeftClick();
                    }
                }
            };

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
        }

        private void UpdateMap()
        {
            layer.Children.Clear();
            cachePins.Clear();
            personPins.Clear();

            foreach (var cache in db.Geocache.Include(g => g.Person))
            {
                string tooltip = $"Latitude:\t\t{cache.Latitude}\r\nLongitude:\t{cache.Longitude}\r\n" +
                    $"Made by:\t{cache.Person.FirstName + " " + cache.Person.LastName}\r\n" +
                    $"Contents:\t{cache.Contents}\r\nMessage:\t{cache.Message}";

                var pin = AddPin(new Location(cache.Latitude, cache.Longitude), tooltip, Colors.Gray);
                pin.Tag = new Dictionary<string, int> { ["PersonID"] = cache.Person.ID, ["CacheID"] = cache.ID };
                pin.MouseLeftButtonDown += OnCachePinClick;
                cachePins.Add(pin);
            }

            foreach (var p in db.Person)
            {
                string tooltip = $"Latitude:\t\t{p.Latitude}\r\nLongitude:\t{p.Longitude}\r\n" +
                    $"Name:\t\t{p.FirstName + " " + p.LastName}\r\nStreet address:\t{p.StreetName + " " + p.StreetNumber}";

                var pin = AddPin(new Location(p.Latitude, p.Longitude), tooltip, Colors.Blue);
                pin.Tag = p.ID;
                pin.MouseLeftButtonDown += OnPersonPinClick;
                personPins.Add(pin);
            }
            // It is recommended (but optional) to use this method for setting the color and opacity of each pin after every user interaction that might change something.
            // This method should then be called once after every significant action, such as clicking on a pin, clicking on the map, or clicking a context menu option.
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.
            UpdateMap();
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            var dialog = new GeocacheDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            string contents = dialog.GeocacheContents;
            string message = dialog.GeocacheMessage;
            // Add geocache to map and database here.
            var pin = AddPin(latestClickLocation, "Person", Colors.Gray);

            pin.MouseDown += (s, a) =>
            {
                // Handle click on geocache pin here.
                MessageBox.Show("You clicked a geocache");
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            string city = dialog.AddressCity;
            string country = dialog.AddressCountry;
            string streetName = dialog.AddressStreetName;
            int streetNumber = dialog.AddressStreetNumber;
            // Add person to map and database here.
            var pin = AddPin(latestClickLocation, "Person", Colors.Blue);

            pin.MouseDown += (s, a) =>
            {
                // Handle click on person pin here.
                MessageBox.Show("You clicked a person");
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private void OnPersonPinClick(object sender, MouseButtonEventArgs e)
        {
            var pushpin = sender as Pushpin;
            int personID = (int)pushpin.Tag;
            ActivePinPersonID = personID;
            pushpin.Opacity = 1;

            personPins.Where(p => (int)p.Tag != personID).ToList().ForEach(p => p.Opacity = 0.5);

            var foundGeocaches = db.FoundGeocache.Where(f => f.PersonID == personID).Include(f => f.GeocacheID).Select(f => f.GeocacheID).ToArray();

            foreach (var pin in cachePins)
            {
                int pinPersonID = (pin.Tag as Dictionary<string, int>)["PersonID"];
                int pinCacheID = (pin.Tag as Dictionary<string, int>)["CacheID"];

                if (pinPersonID == personID)
                    pin.Background = colors["Black"];
                else if (foundGeocaches.Contains(pinCacheID))
                    pin.Background = colors["Green"];
                else
                    pin.Background = colors["Red"];
            }

            e.Handled = true;
        }

        private void OnCachePinClick(object sender, MouseButtonEventArgs e)
        {
            var pin = sender as Pushpin;
            int pinCacheID = ((sender as Pushpin).Tag as Dictionary<string, int>)["CacheID"];

            if (pin.Background == colors["Red"])
            {
                db.Add(new FoundGeocache { PersonID = ActivePinPersonID, GeocacheID = pinCacheID });
                db.SaveChanges();
                pin.Background = colors["Green"];
            }
            else if (pin.Background == colors["Green"])
            {
                db.Remove(db.FoundGeocache
                    .Where(f => f.PersonID == ActivePinPersonID && f.GeocacheID == pinCacheID )
                    .Single());
                db.SaveChanges();
                pin.Background = colors["Red"];
            }

            e.Handled = true;
        }

        private Pushpin AddPin(Location location, string tooltip, Color color)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            return pin;
        }

        private void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Read the selected file here.

            //Felhantering saknas i följande scope.
            var lines = File.ReadLines(path).ToArray();

            db.Person.RemoveRange(db.Person);
            db.Geocache.RemoveRange(db.Geocache);
            db.FoundGeocache.RemoveRange(db.FoundGeocache);
            db.SaveChanges();

            Person person = null;
            Geocache geocache = null;

            foreach (var line in lines)
            {
                if (line != "")
                {
                    string[] temp = line.Split('|').Select(l => l.Trim()).ToArray();

                    if (char.IsLetter(temp[0][0]))
                    {
                        person = new Person()
                        {
                            FirstName = temp[0],
                            LastName = temp[1],
                            Country = temp[2],
                            City = temp[3],
                            StreetName = temp[4],
                            StreetNumber = byte.Parse(temp[5]),
                            Latitude = double.Parse(temp[6]),
                            Longitude = double.Parse(temp[7])
                        };
                        db.Add(person);
                        db.SaveChanges();
                    }
                    else
                    {
                        geocache = new Geocache
                        {
                            Person = person,
                            Latitude = double.Parse(temp[0]),
                            Longitude = double.Parse(temp[1]),
                            Contents = temp[2],
                            Message = temp[3]
                        };
                        db.Add(geocache);
                        db.SaveChanges();
                    }
                }
            }
            UpdateMap();
        }

        private void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.FileName = "Geocaches";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Write to the selected file here.

            //Felhantering saknas på följande scope

            var lines = new List<string>();
            foreach (var p in db.Person.Include(p => p.Geocaches))
            {
                lines.Add(string.Join(" | ", new[] {
                            p.FirstName,
                            p.LastName,
                            p.Country,
                            p.City,
                            p.StreetName,
                            Convert.ToString(p.StreetNumber),
                            Convert.ToString(p.Latitude),
                            Convert.ToString(p.Longitude)
                    }));

                foreach (var g in db.Geocache.Where(g => g.Person == p))
                {
                    lines.Add(string.Join(" | ", new[] {
                            Convert.ToString(g.Latitude),
                            Convert.ToString(g.Longitude),
                            g.Contents,
                            g.Message
                        }));
                }

                lines.Add("");
            }
            File.WriteAllLines(path, lines);
        }
    }
}
