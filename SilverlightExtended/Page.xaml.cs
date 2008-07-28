using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Serialization;
using Nuxleus.Messaging;
using Nuxleus.Messaging.Protobuf;
using Nuxleus.Performance;
using System.Collections.ObjectModel;

namespace SilverlightExtended {

    // struct for holding the various objects and related meta-data for each 
    // serializer we will be using in our test.  This provides a generic interface
    // which we can then more easily use programmatically throughout the codebase.
    public struct SerializerPerformanceTestAgent {
        public string FileExtension { get; set; }
        public string TypeLabel { get; set; }
        public PerformanceLogCollection PerformanceLogCollection { get; set; }
        public ISerializerTestAgent ISerializerTestAgent { get; set; }
    }

    public struct SerializerTask {
        public int TaskID { get; set; }
        public SerializerPerformanceTestAgent Agent { get; set; }
    }

    public delegate void RunTestAsync(PerformanceLogSummary summary);

    public partial class Page : UserControl {

        Queue<PerformanceLog> m_logQueue = new Queue<PerformanceLog>();
        static Object m_lock = new Object();

        // Create a PerformanceTimer to measure performance
        //static PerformanceTimer timer = new PerformanceTimer();

        // Create a SerializerPerformanceTestAgent array which will then allow us to iterate through each 
        // SerializerPerformanceTestAgent contained in the array, keeping our test code clean and simple
        // by placing placing the various objects and values associated with each SerializerPerformanceTestAgent
        // within easy reach.
        static SerializerPerformanceTestAgent[] serializerPeformanceItem = new SerializerPerformanceTestAgent[] {
            new SerializerPerformanceTestAgent{ 
                TypeLabel = "ProtoBuffer", 
                ISerializerTestAgent = new TestProtoBufSerializer(), 
                PerformanceLogCollection = new PerformanceLogCollection(),
                FileExtension = "proto"
            },
            new SerializerPerformanceTestAgent{ 
                TypeLabel = "XML", 
                ISerializerTestAgent = new TestXmlSerializer(), 
                PerformanceLogCollection = new PerformanceLogCollection(), 
                FileExtension = "xml"
            },
        };

        int totalCount = 0;
        int repeatTest = 100;
        decimal percentageComplete = 0;
        DateTime startTime;
        DateTime finishTime;
        ScriptObject jsUpdateElement;
        static Queue<WebClient> webClientQueue = new Queue<WebClient>();

        public Page() {
            InitializeComponent();
            Loaded += new RoutedEventHandler(Page_Loaded);
            jsUpdateElement = (ScriptObject)HtmlPage.Window.GetProperty("updateElement");
        }

        void Page_Loaded(object sender, RoutedEventArgs e) {
            HtmlPage.RegisterScriptableObject("Silverlight", this);
            //PerformanceLogList.SelectedIndex = -1;
            //SummaryGrid.ItemsSource = new ObservableCollection<PerformanceLogSummary>();
        }

        [ScriptableMember]
        public void GetStatus(int id) {
            //Future use
        }

        private void StartSerializationTest(int repeatTest) {

            SilverlightPerformance_Hmmm_NotSoMuch_Timer timer = new SilverlightPerformance_Hmmm_NotSoMuch_Timer();

            using (timer) {
                timer.Scope = () => {
                    for (int i = 0; i < repeatTest; i++) {
                        foreach (SerializerPerformanceTestAgent agent in serializerPeformanceItem) {
                            WebClient webClient = null;

                            lock (m_lock) {
                                if (webClientQueue.Count == 0) {
                                    webClient = new WebClient();
                                } else {
                                    webClient = webClientQueue.Dequeue();
                                }
                            }

                            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
                            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadStringCompleted);

                            m_logQueue.Enqueue(RunSerializationTest((long)i, agent));

                            Uri uri = new Uri(String.Format("http://localhost:9999/Person_{0}.xml", i), UriKind.Absolute);
                            webClient.DownloadStringAsync(uri);
                        }
                    }
                };
                finishTime = DateTime.Now;
            }
        }

        void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {

        }

        //ObservableCollection<PerformanceLogSummary> BoundData {
        //    get {
        //        return (SummaryGrid.ItemsSource as ObservableCollection<PerformanceLogSummary>);
        //    }
        //}

        void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
            if (totalCount == 0) {
                startTime = DateTime.Now;
                jsUpdateElement.InvokeSelf("startTime", startTime);
            }
            lock (m_lock) {
                totalCount += 1;
                percentageComplete = Decimal.Divide((Decimal.Divide((decimal)totalCount, (decimal)repeatTest)), serializerPeformanceItem.Count()) * 100;
                webClientQueue.Enqueue((WebClient)sender);
            }
            jsUpdateElement.InvokeSelf("counter", totalCount);
            jsUpdateElement.InvokeSelf("percentage", percentageComplete);

            if (e.Error == null) {
                PerformanceLog perfLog = m_logQueue.Dequeue();
                var summary = from entry in m_logQueue.Dequeue().Entries
                              select new {
                                  Description = entry.Description,
                                  Value = entry.Value,
                                  EntryType = entry.PerformanceLogEntryType
                              };
                //HtmlPage.Window.Alert(summary.Count().ToString());

                PerformanceLogSummary perfLogSummary = new PerformanceLogSummary();
                perfLogSummary.Sequence = totalCount;

                List<bool> deserializationWasCorrect = new List<bool>();

                foreach (var item in summary) {
                    switch (item.EntryType) {
                        case PerformanceLogEntryType.CompiledObjectCreation:
                            perfLogSummary.CompiledObjectCreationTime = (double)item.Value;
                            break;
                        case PerformanceLogEntryType.Serialization:
                            perfLogSummary.SerializationTime = (double)item.Value;
                            break;
                        case PerformanceLogEntryType.Deserialization:
                            perfLogSummary.DeserializationTime = (double)item.Value;
                            break;
                        case PerformanceLogEntryType.SendSerializedObjectTime:
                            perfLogSummary.SendSerializedObjectTime = (double)item.Value;
                            break;
                        case PerformanceLogEntryType.ReceiveSerializedObjectTime:
                            perfLogSummary.ReceiveSerializedObjectTime = (double)item.Value;
                            break;
                        case PerformanceLogEntryType.StreamSize:
                            perfLogSummary.StreamSize = (double)item.Value;
                            break;
                        case PerformanceLogEntryType.DeserializationCorrect:
                            deserializationWasCorrect.Add((bool)item.Value);
                            break;
                        case PerformanceLogEntryType.TotalDuration:
                            perfLogSummary.TotalDuration = (double)item.Value;
                            break;
                        default:
                            break;

                    }
                    
                }
                perfLogSummary.DeserializationWasCorrect = !deserializationWasCorrect.Contains(false);
                PerformanceLogList.Items.Add(perfLogSummary);
                //BoundData.Insert(totalCount, perfLogSummary);

                //double totalSum = summary.Sum(total => total.Value);

                //PerformanceLogList.Items.Add(new PerformanceLogSummary {
                //    AverageDeserializationTime = totalSum,
                //    AverageSerializationTime = totalSum
                //});
            }
            if (percentageComplete == 100) {
                finishTime = DateTime.Now;
                jsUpdateElement.InvokeSelf("finishTime", finishTime);
                jsUpdateElement.InvokeSelf("totalTime", finishTime.Subtract(startTime).TotalMilliseconds);
            }
        }



        private void btnRunTest_Click(object sender, RoutedEventArgs e) {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimer.Start();
            StartSerializationTest(repeatTest);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            //Add any operations you want performed every X intervals, X representing the TimeSpan
            //specified in dispatchTimer.Interval set above.
        }

        void RunTest(int repeatTest) {
            SilverlightPerformance_Hmmm_NotSoMuch_Timer timer = new SilverlightPerformance_Hmmm_NotSoMuch_Timer();

            using (timer) {
                timer.Scope = () => {
                    for (int i = 0; i < repeatTest; i++) {
                        foreach (SerializerPerformanceTestAgent agent in serializerPeformanceItem) {
                            PerformanceLog result = RunSerializationTest((long)i, agent);
                        }
                    }
                };
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(PerformanceLogCollection));

            foreach (SerializerPerformanceTestAgent agent in serializerPeformanceItem) {
                using (MemoryStream stream = new MemoryStream()) {
                    xmlSerializer.Serialize(stream, agent.PerformanceLogCollection);
                }
            }
        }

        void PerformanceLogList_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }

        public PerformanceLog RunSerializationTest(long fileSequence, SerializerPerformanceTestAgent agent) {

            SilverlightPerformance_Hmmm_NotSoMuch_Timer timer = new SilverlightPerformance_Hmmm_NotSoMuch_Timer();
            SilverlightPerformance_Hmmm_NotSoMuch_Timer.UnitPrecision = UnitPrecision.NANOSECONDS;
            PerformanceLog perfLog = new PerformanceLog { 
                Entries = new List<Entry>(), 
                UnitPrecision = SilverlightPerformance_Hmmm_NotSoMuch_Timer.UnitPrecision 
            };
            Person person = null;

            using (timer) {
                timer.Scope = () => {

                    timer.LogScope("Create a Person object", perfLog, PerformanceLogEntryType.CompiledObjectCreation, () => {
                        person = CreatePerson(fileSequence);
                    });

                    Stream memoryStream = null;

                    timer.LogScope("Serialize the Person object to a MemoryStream", perfLog, PerformanceLogEntryType.Serialization, () => {
                        memoryStream = SerializeToStream<Person>(person, null, agent.ISerializerTestAgent);
                    }).LogData("Length (in bytes) of memoryStream", memoryStream.Length, PerformanceLogEntryType.StreamSize);

                    Person newPersonFromMemoryStream = null;

                    using (memoryStream) {
                        timer.LogScope("Deserialize and parse the Person object from a MemoryStream", perfLog, PerformanceLogEntryType.Deserialization, () => {
                            newPersonFromMemoryStream = DeserializeFromStream<Person>(memoryStream, agent.ISerializerTestAgent);
                        });
                    }

                    CompareValuesAndLogResults(person, newPersonFromMemoryStream, perfLog, typeof(MemoryStream), PerformanceLogEntryType.DeserializationCorrect);


                    //TODO: Store the uncompressed serialized object on S3

                    //TODO: Retrieve the uncompressed serialized object from S3 and deserialize into a MemoryStream

                    //TODO: Compress and store the serialized object on S3

                    //TODO: Retrieve the compressed serialized object from S3 and deserialize into a MemoryStream

                };
                perfLog.LogData("Duration of test", timer.Duration, PerformanceLogEntryType.TotalDuration);
            }

            return perfLog;
        }

        static Person CreatePerson(long fileSequence) {

            List<PhoneNumber> phoneNumbers = new List<PhoneNumber>();
            phoneNumbers.Add(new PhoneNumber { Number = "555.555.1234", Type = PhoneType.HOME });
            phoneNumbers.Add(new PhoneNumber { Number = "555.555.5678", Type = PhoneType.MOBILE });
            phoneNumbers.Add(new PhoneNumber { Number = "555.555.9012", Type = PhoneType.WORK });

            return new Person {
                Name = String.Format("John Doe{0}", fileSequence),
                Email = String.Format("jdoe{0}@example.com", fileSequence),
                Phone = phoneNumbers
            };
        }


        void WriteValuesToConsole(Person person) {
            //this.txtBox.Text += String.Format("person.Name: {0}, person.Email: {1}, person.ID\n", person.Name, person.Email, person.ID);
            foreach (PhoneNumber phone in person.Phone) {
                //this.txtBox.Text += String.Format("phone.Number: {0}, phone.Type: {1}\n", phone.Number, phone.Type);
            }
        }

        static void CompareValuesAndLogResults(Person person, Person newPerson, PerformanceLog perfLog, Type streamType, PerformanceLogEntryType type) {
            perfLog.LogData(String.Format("newPersonFrom{0}.Name and person.Name are equal", streamType.Name), String.Equals(newPerson.Name, person.Name), type);
            perfLog.LogData(String.Format("newPersonFrom{0}.ID and person.ID are equal", streamType.Name), int.Equals(newPerson.ID, person.ID), type);
            perfLog.LogData(String.Format("newPersonFrom{0}.Email and person.Email are equal", streamType.Name), String.Equals(newPerson.Email, person.Email), type);

            PhoneNumber[] phone = person.Phone.ToArray();
            PhoneNumber[] newPhone = newPerson.Phone.ToArray();

            for (int i = 0; i < phone.Length; i++) {
                perfLog.LogData(String.Format("PhoneNumber[{0}].Number from newPersonFrom{1}.Phone is the same as PhoneNumber[{0}].Number from person{1}.Phone", i, streamType.Name), phone[i].Number.Equals(newPhone[i].Number), type);
                perfLog.LogData(String.Format("PhoneNumber[{0}].Type from newPersonFrom{1}.Phone is the same as PhoneNumber[{0}].Type from person{1}.Phone", i, streamType.Name), phone[i].Type.Equals(newPhone[i].Type), type);
            }
        }

        static bool StoreObjectToS3(string fileName, bool compressFile) {
            return true;
        }

        static bool GetObjectFromS3(string fileUri) {
            return true;
        }

        static Stream SerializeToStream<T>(T obj, String fileName, ISerializerTestAgent serializer) where T : class, new() {

            Stream stream = null;
            if (fileName == null) {
                stream = new MemoryStream();
            } else {
                stream = new FileStream(fileName, FileMode.Create);
            }

            return serializer.Serialize<T>(stream, obj);
        }

        static T DeserializeFromStream<T>(Stream stream, ISerializerTestAgent serializer) where T : class, new() {
            stream.Seek(0, 0);
            return serializer.Deserialize<T>(stream);
        }

    }
}
