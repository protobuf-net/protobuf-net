using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Issues
{
    public class SO60842788
    {
        private readonly ITestOutputHelper _log;
        public SO60842788(ITestOutputHelper log) => _log = log;
        private void Log(string message) => _log?.WriteLine(message);

        static readonly bool IsCoreFX = typeof(int).Assembly.GetName().Name == "mscorlib", IsNet5 = typeof(string).Assembly.GetName().Version >= new Version(5,0);

        [Fact]
        public void CanRoundTripWrapped()
        {
            string EXPECTED = IsCoreFX
                ? @"0A-59-53-79-73-74-65-6D-2E-49-6E-74-33-32-2C-20-6D-73-63-6F-72-6C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-62-37-37-61-35-63-35-36-31-39-33-34-65-30-38-39-12-5A-53-79-73-74-65-6D-2E-53-74-72-69-6E-67-2C-20-6D-73-63-6F-72-6C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-62-37-37-61-35-63-35-36-31-39-33-34-65-30-38-39-12-5A-53-79-73-74-65-6D-2E-55-49-6E-74-33-32-2C-20-6D-73-63-6F-72-6C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-62-37-37-61-35-63-35-36-31-39-33-34-65-30-38-39"
                : IsNet5 // Net5 hanges the Version token in the AQN
                ? @"0A-67-53-79-73-74-65-6D-2E-49-6E-74-33-32-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-35-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65-12-68-53-79-73-74-65-6D-2E-53-74-72-69-6E-67-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-35-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65-12-68-53-79-73-74-65-6D-2E-55-49-6E-74-33-32-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-35-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65"
                : @"0A-67-53-79-73-74-65-6D-2E-49-6E-74-33-32-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65-12-68-53-79-73-74-65-6D-2E-53-74-72-69-6E-67-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65-12-68-53-79-73-74-65-6D-2E-55-49-6E-74-33-32-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65";

            var data = new Wrapped { Single = typeof(int), Vector = new[] { typeof(string), typeof(uint) } };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Log(hex);
            Assert.Equal(EXPECTED, hex);
            ms.Position = 0;
            var clone = Serializer.Deserialize<Wrapped>(ms);
            Assert.Same(typeof(int), clone.Single);
            Assert.Equal(2, clone.Vector.Length);
            Assert.Equal(typeof(string), clone.Vector[0]);
            Assert.Equal(typeof(uint), clone.Vector[1]);
        }

        [Fact]
        public void CanRoundTripVector()
        {
            string EXPECTED = IsCoreFX
                ? @"0A-5A-53-79-73-74-65-6D-2E-53-74-72-69-6E-67-2C-20-6D-73-63-6F-72-6C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-62-37-37-61-35-63-35-36-31-39-33-34-65-30-38-39-0A-5A-53-79-73-74-65-6D-2E-55-49-6E-74-33-32-2C-20-6D-73-63-6F-72-6C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-62-37-37-61-35-63-35-36-31-39-33-34-65-30-38-39"
                : IsNet5 // Net5 hanges the Version token in the AQN
                ? @"0A-68-53-79-73-74-65-6D-2E-53-74-72-69-6E-67-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-35-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65-0A-68-53-79-73-74-65-6D-2E-55-49-6E-74-33-32-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-35-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65"
                : @"0A-68-53-79-73-74-65-6D-2E-53-74-72-69-6E-67-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65-0A-68-53-79-73-74-65-6D-2E-55-49-6E-74-33-32-2C-20-53-79-73-74-65-6D-2E-50-72-69-76-61-74-65-2E-43-6F-72-65-4C-69-62-2C-20-56-65-72-73-69-6F-6E-3D-34-2E-30-2E-30-2E-30-2C-20-43-75-6C-74-75-72-65-3D-6E-65-75-74-72-61-6C-2C-20-50-75-62-6C-69-63-4B-65-79-54-6F-6B-65-6E-3D-37-63-65-63-38-35-64-37-62-65-61-37-37-39-38-65";

            var data = new[] { typeof(string), typeof(uint) };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Log(hex);
            Assert.Equal(EXPECTED, hex);
            ms.Position = 0;
            var clone = Serializer.Deserialize<Type[]>(ms);
            Assert.Equal(2, clone.Length);
            Assert.Equal(typeof(string), clone[0]);
            Assert.Equal(typeof(uint), clone[1]);
        }

        [Fact]
        public void WrappedAndVectorAreSame()
        {
            var data = new[] { typeof(string), typeof(uint) };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Log(hex);
            
            ms.Position = 0;
            var clone = Serializer.Deserialize<WrappedBasic>(ms).Vector;
            Assert.Equal(2, clone.Length);
            Assert.Equal(typeof(string), clone[0]);
            Assert.Equal(typeof(uint), clone[1]);
        }
    }

    [ProtoContract]
    public class Wrapped
    {
        [ProtoMember(1)]
        public Type Single { get; set; }
        [ProtoMember(2)]
        public Type[] Vector { get; set; }
    }

    [ProtoContract]
    public class WrappedBasic
    {
        [ProtoMember(1)]
        public Type[] Vector { get; set; }
    }
}
