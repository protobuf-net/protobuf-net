using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ProtoBuf;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Metro_DevRig
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {            
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ReadProto(((Button)sender));
            //((Button)sender).Content = clone.Foo + ", " + clone.Bar;
        }
        private async void ReadProto(Button button)
        {
            var path = @"nwind.proto.bin";
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await folder.GetFileAsync(path);

            var readStream = await file.OpenReadAsync();
            IInputStream inputSteam = readStream.GetInputStreamAt(0);
            DataReader dataReader = new DataReader(inputSteam);
            uint numBytesLoaded = await dataReader.LoadAsync((uint)readStream.Size);
            byte[] raw = new byte[readStream.Size];
            dataReader.ReadBytes(raw);
            var ms = new MemoryStream(raw);
            var dal = (DAL.DatabaseCompat)new Foo().Deserialize(ms, null, typeof(DAL.DatabaseCompat));
            button.Content = dal.Orders.Count;
        }
    }
}
[ProtoContract]
public class MyDto
{
    [ProtoMember(1)]
    public string Foo { get; set; }
    [ProtoMember(2)]
    public int Bar { get; set; }
}
