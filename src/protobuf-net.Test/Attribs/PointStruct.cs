﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Meta;
using System.Threading;
using ProtoBuf.unittest.Meta;

namespace ProtoBuf.unittest.Attribs
{
    
    public class PointStructTests
    {

        public static RuntimeTypeModel BuildModelWithSurrogate()
        {
            RuntimeTypeModel model = RuntimeTypeModel.Create();
            model.Add(typeof(PointSurrogate), true);
            model.Add(typeof(Point), false).SetSurrogate(typeof(PointSurrogate));
            return model;
        }

        [ProtoContract]
        public struct PointSurrogate {
            private static int toPoint, fromPoint;
            public static int ToPoint { get { return toPoint; } }
            public static int FromPoint { get { return fromPoint; } }
            public PointSurrogate(int x, int y) {
                this.X = x;
                this.Y = y;
            }
            [ProtoMember(1)] public int X;
            [ProtoMember(2)] public int Y;

            public static explicit operator PointSurrogate (Point value) {
                Interlocked.Increment(ref fromPoint);
                return new PointSurrogate(value.X, value.Y);
            }
            public static implicit operator Point(PointSurrogate value) {
                Interlocked.Increment(ref toPoint);
                return new Point(value.X, value.Y);
            }
        }

        [ProtoContract]
        public struct Point
        {
            [ProtoMember(1)] private readonly int x;
            [ProtoMember(2)] private readonly int y;
            public readonly int X { get { return x; } }
            public readonly int Y { get { return y; } }
            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        static RuntimeTypeModel BuildModel()
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add(typeof(Point), true);
            var fields = mt.GetFields();
            Assert.Equal(2, fields.Length);
            return model;
        }
        [Fact]
        public void RoundTripPoint()
        {
            Point point = new Point(26, 13);
            var model = BuildModel();

            ClonePoint(model, point, "Runtime");

            model.CompileInPlace();
            ClonePoint(model, point, "CompileInPlace");
        }
        [Fact]
        public void FullyCompileWithPrivateField_KnownToFail()
        {
            try {
                var model = BuildModel();
                Point point = new Point(26, 13);
                ClonePoint(model.Compile(), point, "Compile");
                Assert.Equal(42, 0); // fail
            } catch (InvalidOperationException ex)
            {
                Assert.Equal("Non-public member cannot be used with full dll compilation: ProtoBuf.unittest.Attribs.PointStructTests+Point.x", ex.Message);
            }
        }
        static void ClonePoint(TypeModel model, Point original, string message)
        {
            Point clone = (Point)model.DeepClone(original);
            Assert.Equal(original.X, clone.X); //, message + ": X");
            Assert.Equal(original.Y, clone.Y); //, message + ": Y");
        }

        static void ClonePointCountingConversions(TypeModel model, Point original, string message,
            int toPoint, int fromPoint)
        {
            int oldTo = PointSurrogate.ToPoint, oldFrom = PointSurrogate.FromPoint;
            Point clone = (Point)model.DeepClone(original);
            int newTo = PointSurrogate.ToPoint, newFrom = PointSurrogate.FromPoint;
            Assert.Equal(original.X, clone.X); //, message + ": X");
            Assert.Equal(original.Y, clone.Y); //, message + ": Y");
            Assert.Equal(toPoint, newTo - oldTo); //, message + ": Surrogate to Point");
            Assert.Equal(fromPoint, newFrom - oldFrom); //, message + ": Point to Surrogate");
        }

        [Fact]
        public void VerifyPointWithSurrogate()
        {
            var model = BuildModelWithSurrogate();
            model.Compile("PointWithSurrogate", "PointWithSurrogate.dll");
            PEVerify.Verify("PointWithSurrogate.dll");
        }

        [Fact]
        public void VerifyPointDirect()
        {
            try {
                var model = BuildModel();
                model.Compile("PointDirect", "PointDirect.dll");
                PEVerify.Verify("PointDirect.dll", 1); // expect failure due to field access
            } catch(InvalidOperationException ex)
            {
                Assert.Equal("Non-public member cannot be used with full dll compilation: ProtoBuf.unittest.Attribs.PointStructTests+Point.x", ex.Message);
            }
        }

        [Fact(Skip = "needs investigation, but minor")]
        public void RoundTripPointWithSurrogate()
        {
            Point point = new Point(26, 13);
            var model = BuildModelWithSurrogate();

            // two Point => Surrogate (one write, one read)
            // one Point <= Surrogate (one read)
            ClonePointCountingConversions(model, point, "Runtime", 1, 2);

            model.CompileInPlace();
            ClonePointCountingConversions(model, point, "CompileInPlace", 1, 2);
            ClonePointCountingConversions(model.Compile(), point, "Compile", 1, 2);
        }
    }

}
