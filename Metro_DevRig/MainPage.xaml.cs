using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

            var ser = new Foo();
            var dal = (DAL.DatabaseCompat)ser.Deserialize(ms, null, typeof(DAL.DatabaseCompat));

            StringBuilder perfStats = new StringBuilder();

            perfStats.AppendLine(".NET for Metro style apps");
            perfStats.AppendFormat("{0} orders from NWind", dal.Orders.Count).AppendLine();
            
            var dcs = new DataContractSerializer(typeof(DAL.DatabaseCompat));
            var xs = new XmlSerializer(typeof(DAL.DatabaseCompat));
            using (var buffer = new MemoryStream())
            {
                const int loop = 50;
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < loop; i++)
                {
                    buffer.SetLength(0);
                    dcs.WriteObject(buffer, dal);
                }
                watch.Stop();
                perfStats.AppendLine().AppendLine().AppendLine("DataContractSerializer:").AppendFormat("WriteObject x {0}: {1:###,###}ms, {2:###,###} bytes", loop, watch.ElapsedMilliseconds, buffer.Length);

                watch = Stopwatch.StartNew();
                for (int i = 0; i < loop; i++)
                {
                    buffer.Position = 0;
                    dcs.ReadObject(buffer);
                }
                watch.Stop();
                perfStats.AppendLine().AppendFormat("ReadObject x {0}: {1:###,###}ms", loop, watch.ElapsedMilliseconds);

                watch = Stopwatch.StartNew();
                for (int i = 0; i < loop; i++)
                {
                    buffer.SetLength(0);
                    xs.Serialize(buffer, dal);
                }
                watch.Stop();
                perfStats.AppendLine().AppendLine().AppendLine("XmlSerializer:").AppendFormat("Serialize x {0}: {1:###,###}ms, {2:###,###} bytes", loop, watch.ElapsedMilliseconds, buffer.Length);

                watch = Stopwatch.StartNew();
                for (int i = 0; i < loop; i++)
                {
                    buffer.Position = 0;
                    xs.Deserialize(buffer);
                }
                watch.Stop();
                perfStats.AppendLine().AppendFormat("Deserialize x {0}: {1:###,###}ms", loop, watch.ElapsedMilliseconds);

                watch = Stopwatch.StartNew();
                for (int i = 0; i < loop; i++)
                {
                    buffer.SetLength(0);
                    ser.Serialize(buffer, dal);
                }
                watch.Stop();
                perfStats.AppendLine().AppendLine().AppendLine("protobuf-net").AppendFormat("Serialize x {0}: {1:###,###}ms, {2:###,###} bytes", loop, watch.ElapsedMilliseconds, buffer.Length);

                watch = Stopwatch.StartNew();
                for (int i = 0; i < loop; i++)
                {
                    buffer.Position = 0;
                    ser.Deserialize(buffer, null, typeof(DAL.DatabaseCompat));
                }
                watch.Stop();
                perfStats.AppendLine().AppendFormat("Deserialize x {0}: {1:###,###}ms", loop, watch.ElapsedMilliseconds);
            }
            button.Content = perfStats.ToString();


            // test SM2Stats
            bool isSer = ser.CanSerializeContractType(typeof(SM2Stats));

            SM2Stats stats = new SM2Stats
            {
                acqreps = 1,
                difficultyhistory =  {
                    new DiffHistPair { date = 123, values = { 3.4f }}
                }
            };
            var clone = (SM2Stats)ser.DeepClone(stats); // checked by eye; is the same

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
