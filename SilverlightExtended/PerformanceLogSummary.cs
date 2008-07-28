
namespace SilverlightExtended {

    public class PerformanceLogSummary {
        public int Sequence { get; set; }
        public double CompiledObjectCreationTime { get; set; }
        public double StreamSize { get; set; }
        public double SerializationTime { get; set; }
        public double DeserializationTime { get; set; }
        public double SendSerializedObjectTime { get; set; }
        public double ReceiveSerializedObjectTime { get; set; }
        public double TotalDuration { get; set; }
        public bool DeserializationWasCorrect { get; set; }
    }
}
