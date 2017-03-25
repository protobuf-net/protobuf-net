using System.Collections.Generic;
using System.Xml.Serialization;

namespace Nuxleus.Performance {

    public enum PerformanceLogEntryType {
        Sequence,
        CompiledObjectCreation,
        StreamSize,
        Serialization,
        Deserialization,
        DeserializationCorrect,
        SendSerializedObjectTime,
        ReceiveSerializedObjectTime,
        TotalDuration
    }

    [XmlType(Namespace = "http://nuxleus.com/Nuxleus/Performance")]
    [XmlRootAttribute(Namespace = "http://nuxleus.com/Nuxleus/Performance", IsNullable = false)]
    public class PerformanceLogCollection {

        List<PerformanceLog> m_log = new List<PerformanceLog>();

        public void Add(PerformanceLog log) {
            Log.Add(log);
        }

        [XmlElement(ElementName = "PerformanceLog")]
        public List<PerformanceLog> Log {
            get {
                return m_log;
            }
        }
    }

    [XmlTypeAttribute(Namespace = "http://nuxleus.com/Nuxleus/Performance")]
    public struct PerformanceLog {

        public void LogData(string description, double value, PerformanceLogEntryType type) {
            Entries.Add(new Entry {
                Description = description,
                Value = value,
                PerformanceLogEntryType = type
            });
        }

        public void LogData(string description, bool value, PerformanceLogEntryType type) {
            Entries.Add(new Entry {
                Description = description,
                Value = value,
                PerformanceLogEntryType = type
            });
        }

        [XmlElement(ElementName = "Entry")]
        public List<Entry> Entries { get; set; }

        [XmlAttribute(AttributeName = "UnitPrecision")]
        public UnitPrecision UnitPrecision { get; set; }

    }

    [XmlType(Namespace = "http://nuxleus.com/Nuxleus/Performance")]
    public struct Entry {
        public PerformanceLogEntryType PerformanceLogEntryType { get; set; }
        public string Description { get; set; }
        public object Value { get; set; }
    }
}
