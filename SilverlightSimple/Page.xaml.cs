using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ProtoBuf;
using System.Reflection;
using Nuxleus.Performance;
using Nuxleus.MetaData;
using Nuxleus.Messaging;
using Nuxleus.Messaging.Protobuf;
using System.IO;

namespace SilverlightSimple {


    // struct for holding the various objects and related meta-data for each 
    // serializer we will be using in our test.  This provides a generic interface
    // which we can then more easily use programmatically throughout the codebase.
    public struct SerializerPerformanceTestAgent {
        public string FileExtension { get; set; }
        public string TypeLabel { get; set; }
        public PerformanceLogCollection PerformanceLogCollection { get; set; }
        public ISerializerTestAgent ISerializerTestAgent { get; set; }
    }

    public partial class Page : UserControl {

        // Create a PerformanceTimer to measure performance
        static PerformanceTimer m_timer = new PerformanceTimer();

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
        public Page() {

            InitializeComponent();

            PerformanceTimer.UnitPrecision = UnitPrecision.NANOSECONDS;

            int repeatTest = 500;

            this.txtBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            using (m_timer) {
                m_timer.Scope = () => {
                    for (int i = 0; i < repeatTest; i++) {
                        foreach (SerializerPerformanceTestAgent agent in serializerPeformanceItem) {
                            agent.PerformanceLogCollection.Add(RunSerializationTest(i, agent));
                        }
                    }
                };
                this.txtBox.Text += String.Format("Completed Serialization Tests in {0}", m_timer.Elapsed);
            }
            WriteValuesToConsole(CreatePerson(255));
        }

        PerformanceLog RunSerializationTest(int fileSequence, SerializerPerformanceTestAgent agent) {

            // Create a new PerformanceLog for logging the performance numbers
            PerformanceLog perfLog = new PerformanceLog {
                Entries = new List<Entry>(),
                UnitPrecision = PerformanceTimer.UnitPrecision
            };

            Person person = null;

            m_timer.Scope = () => {

                m_timer.LogScope("Create a Person object", perfLog, () => {
                    person = CreatePerson(fileSequence);
                });

                m_timer.LogScope("Write values of Person object to stdout", perfLog, () => {
                    WriteValuesToConsole(person);
                });

                Stream memoryStream = null;

                m_timer.LogScope("Serialize the Person object to a MemoryStream", perfLog, () => {
                    memoryStream = SerializeToStream<Person>(person, null, agent.ISerializerTestAgent);
                }).LogData("Length (in bytes) of memoryStream", memoryStream.Length);

                Person newPersonFromMemoryStream = null;

                using (memoryStream) {
                    m_timer.LogScope("Deserialize and parse the Person object from a MemoryStream", perfLog, () => {
                        newPersonFromMemoryStream = DeserializeFromStream<Person>(memoryStream, agent.ISerializerTestAgent);
                    });
                }

                CompareValuesAndLogResults(person, newPersonFromMemoryStream, perfLog, typeof(MemoryStream));

                //TODO: Store the uncompressed serialized object on S3

                //TODO: Retrieve the uncompressed serialized object from S3 and deserialize into a MemoryStream

                //TODO: Compress and store the serialized object on S3

                //TODO: Retrieve the compressed serialized object from S3 and deserialize into a MemoryStream

            };
            perfLog.LogData("Duration of test", m_timer.Duration);
            return perfLog;
        }

        static Person CreatePerson(int fileSequence) {

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
            this.txtBox.Text = "";
            this.txtBox.Text += String.Format("person.Name: {0}, person.Email: {1}, person.ID\n", person.Name, person.Email, person.ID);
            foreach (PhoneNumber phone in person.Phone) {
                this.txtBox.Text += String.Format("phone.Number: {0}, phone.Type: {1}\n", phone.Number, phone.Type);
            }
        }

        static void CompareValuesAndLogResults(Person person, Person newPerson, PerformanceLog perfLog, Type streamType) {
            perfLog.LogData(String.Format("newPersonFrom{0}.Name and person.Name are equal", streamType.Name), String.Equals(newPerson.Name, person.Name));
            perfLog.LogData(String.Format("newPersonFrom{0}.ID and person.ID are equal", streamType.Name), int.Equals(newPerson.ID, person.ID));
            perfLog.LogData(String.Format("newPersonFrom{0}.Email and person.Email are equal", streamType.Name), String.Equals(newPerson.Email, person.Email));

            PhoneNumber[] phone = person.Phone.ToArray();
            PhoneNumber[] newPhone = newPerson.Phone.ToArray();

            for (int i = 0; i < phone.Length; i++) {
                perfLog.LogData(String.Format("PhoneNumber[{0}].Number from newPersonFrom{1}.Phone is the same as PhoneNumber[{0}].Number from person{1}.Phone", i, streamType.Name), phone[i].Number.Equals(newPhone[i].Number));
                perfLog.LogData(String.Format("PhoneNumber[{0}].Type from newPersonFrom{1}.Phone is the same as PhoneNumber[{0}].Type from person{1}.Phone", i, streamType.Name), phone[i].Type.Equals(newPhone[i].Type));
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
