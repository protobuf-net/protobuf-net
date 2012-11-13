using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Phone8_DevRig.Resources;
using System.IO;
using ProtoBuf;
using DAL;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Phone8_DevRig
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private MemoryStream LoadWithoutDiskIO()
        {
            var ms = new MemoryStream();
            using (var file = File.OpenRead("nwind.proto.bin"))
            {
                file.CopyTo(ms);
            }
            ms.Position = 0;
            return ms;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (var source = LoadWithoutDiskIO())
            {
                var watch = Stopwatch.StartNew();
                var obj = Serializer.Deserialize<DatabaseCompat>(source);
                watch.Stop();
                runtimeButton.Content = string.Format("Runtime: {0} orders, {1} ms", obj.Orders.Count, watch.ElapsedMilliseconds);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var ser = new MySerializer();
            using (var source = LoadWithoutDiskIO())
            {
                var watch = Stopwatch.StartNew();
                var obj = (DatabaseCompat)ser.Deserialize(source, null, typeof(DatabaseCompat));
                watch.Stop();
                precompiledButton.Content = string.Format("Precompiled: {0} orders, {1} ms", obj.Orders.Count, watch.ElapsedMilliseconds);
            }
        }

        private void dcsButton_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCompat db;
            using (var file = File.OpenRead("nwind.proto.bin"))
            {
                db = Serializer.Deserialize<DatabaseCompat>(file);
            }
            using (var ms = new MemoryStream())
            {
                // write to MS so we can test the deserialize perf
                ser.WriteObject(ms, db);
                ms.Position = 0;
                var watch = Stopwatch.StartNew();
                var obj = (DatabaseCompat)ser.ReadObject(ms);
                watch.Stop();
                dcsButton.Content = string.Format("DCS: {0} orders, {1} ms", obj.Orders.Count, watch.ElapsedMilliseconds);
            }
        }

        private readonly DataContractSerializer ser = new DataContractSerializer(typeof(DatabaseCompat));

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}