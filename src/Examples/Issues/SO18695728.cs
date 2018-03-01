using System;
using System.Collections.Generic;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SO18695728
    {
        [Fact]
        public void Execute()
        {
             RuntimeTypeModel.Default[typeof(WebSyncedObject)].AddSubType(10, typeof(GPSReading));
             RuntimeTypeModel.Default[typeof(WebSyncedObject)].AddSubType(11, typeof(TemperatureReading));

             var list = new List<WebSyncedObject>
             {
                 new GPSReading { SpeedKM = 123.45M },
                 new TemperatureReading { Temperature = 67.89M }
             };
             var clone = Serializer.DeepClone(list);

             Assert.Equal(2, clone.Count);
             Assert.IsType<GPSReading>(clone[0]);
             Assert.IsType<TemperatureReading>(clone[1]);
        }
        [ProtoContract]
        public abstract class WebSyncedObject
        {
            [ProtoMember(1)]
            public DateTime SystemTime { get; set; }

            [ProtoMember(2)]
            public bool TimeSynchronized { get; set; }

            [ProtoMember(3)]
            public ulong RelativeTime { get; set; }

            [ProtoMember(4)]
            public Guid BootID { get; set; }

            protected WebSyncedObject()
            {
                BootID = Guid.NewGuid();
                SystemTime = DateTime.Now;
            }
        }

        [ProtoContract]
        public class GPSReading : WebSyncedObject
        {
            [ProtoMember(1)]
            public DateTime SatelliteTime { get; set; }

            [ProtoMember(2)]
            public decimal Latitude { get; set; }

            [ProtoMember(3)]
            public decimal Longitude { get; set; }

            [ProtoMember(4)]
            public int NumSatellites { get; set; }

            [ProtoMember(5)]
            public decimal SpeedKM { get; set; }
        }

        [ProtoContract]
        public class TemperatureReading : WebSyncedObject
        {
            [ProtoMember(1)]
            public decimal Temperature { get; set; }

            [ProtoMember(2)]
            public int NodeID { get; set; }

            [ProtoMember(3)]
            public string ProbeIdentifier { get; set; }
        }
    }
}
