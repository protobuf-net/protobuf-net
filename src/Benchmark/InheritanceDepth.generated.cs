// GENERATED - companion to InheritanceDepthBenchmarks.cs. Two 16-deep [ProtoInclude] chains that
// differ ONLY in the framing of their sub-type markers: length-prefixed, and delimited
// (DataFormat.Group). One chain per framing is enough for every depth, because the RUNTIME type
// decides how many markers are written - instantiating P3 writes three, P15 writes fifteen.
//
// Field numbers repeat per layer on purpose: a layer's own members live inside that layer's
// sub-message, so each layer has its own field space.
#if NET8_0_OR_GREATER
using ProtoBuf;

namespace Benchmark
{
    [ProtoContract]
    [ProtoInclude(1000, typeof(P1))]
    public class P0
    {
        [ProtoMember(1)] public int Value0 { get; set; }
        [ProtoMember(2)] public string Label0 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P2))]
    public class P1 : P0
    {
        [ProtoMember(1)] public int Value1 { get; set; }
        [ProtoMember(2)] public string Label1 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P3))]
    public class P2 : P1
    {
        [ProtoMember(1)] public int Value2 { get; set; }
        [ProtoMember(2)] public string Label2 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P4))]
    public class P3 : P2
    {
        [ProtoMember(1)] public int Value3 { get; set; }
        [ProtoMember(2)] public string Label3 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P5))]
    public class P4 : P3
    {
        [ProtoMember(1)] public int Value4 { get; set; }
        [ProtoMember(2)] public string Label4 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P6))]
    public class P5 : P4
    {
        [ProtoMember(1)] public int Value5 { get; set; }
        [ProtoMember(2)] public string Label5 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P7))]
    public class P6 : P5
    {
        [ProtoMember(1)] public int Value6 { get; set; }
        [ProtoMember(2)] public string Label6 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P8))]
    public class P7 : P6
    {
        [ProtoMember(1)] public int Value7 { get; set; }
        [ProtoMember(2)] public string Label7 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P9))]
    public class P8 : P7
    {
        [ProtoMember(1)] public int Value8 { get; set; }
        [ProtoMember(2)] public string Label8 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P10))]
    public class P9 : P8
    {
        [ProtoMember(1)] public int Value9 { get; set; }
        [ProtoMember(2)] public string Label9 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P11))]
    public class P10 : P9
    {
        [ProtoMember(1)] public int Value10 { get; set; }
        [ProtoMember(2)] public string Label10 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P12))]
    public class P11 : P10
    {
        [ProtoMember(1)] public int Value11 { get; set; }
        [ProtoMember(2)] public string Label11 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P13))]
    public class P12 : P11
    {
        [ProtoMember(1)] public int Value12 { get; set; }
        [ProtoMember(2)] public string Label12 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P14))]
    public class P13 : P12
    {
        [ProtoMember(1)] public int Value13 { get; set; }
        [ProtoMember(2)] public string Label13 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(P15))]
    public class P14 : P13
    {
        [ProtoMember(1)] public int Value14 { get; set; }
        [ProtoMember(2)] public string Label14 { get; set; }
    }

    [ProtoContract]
    public class P15 : P14
    {
        [ProtoMember(1)] public int Value15 { get; set; }
        [ProtoMember(2)] public string Label15 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D1), DataFormat = DataFormat.Group)]
    public class D0
    {
        [ProtoMember(1)] public int Value0 { get; set; }
        [ProtoMember(2)] public string Label0 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D2), DataFormat = DataFormat.Group)]
    public class D1 : D0
    {
        [ProtoMember(1)] public int Value1 { get; set; }
        [ProtoMember(2)] public string Label1 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D3), DataFormat = DataFormat.Group)]
    public class D2 : D1
    {
        [ProtoMember(1)] public int Value2 { get; set; }
        [ProtoMember(2)] public string Label2 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D4), DataFormat = DataFormat.Group)]
    public class D3 : D2
    {
        [ProtoMember(1)] public int Value3 { get; set; }
        [ProtoMember(2)] public string Label3 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D5), DataFormat = DataFormat.Group)]
    public class D4 : D3
    {
        [ProtoMember(1)] public int Value4 { get; set; }
        [ProtoMember(2)] public string Label4 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D6), DataFormat = DataFormat.Group)]
    public class D5 : D4
    {
        [ProtoMember(1)] public int Value5 { get; set; }
        [ProtoMember(2)] public string Label5 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D7), DataFormat = DataFormat.Group)]
    public class D6 : D5
    {
        [ProtoMember(1)] public int Value6 { get; set; }
        [ProtoMember(2)] public string Label6 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D8), DataFormat = DataFormat.Group)]
    public class D7 : D6
    {
        [ProtoMember(1)] public int Value7 { get; set; }
        [ProtoMember(2)] public string Label7 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D9), DataFormat = DataFormat.Group)]
    public class D8 : D7
    {
        [ProtoMember(1)] public int Value8 { get; set; }
        [ProtoMember(2)] public string Label8 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D10), DataFormat = DataFormat.Group)]
    public class D9 : D8
    {
        [ProtoMember(1)] public int Value9 { get; set; }
        [ProtoMember(2)] public string Label9 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D11), DataFormat = DataFormat.Group)]
    public class D10 : D9
    {
        [ProtoMember(1)] public int Value10 { get; set; }
        [ProtoMember(2)] public string Label10 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D12), DataFormat = DataFormat.Group)]
    public class D11 : D10
    {
        [ProtoMember(1)] public int Value11 { get; set; }
        [ProtoMember(2)] public string Label11 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D13), DataFormat = DataFormat.Group)]
    public class D12 : D11
    {
        [ProtoMember(1)] public int Value12 { get; set; }
        [ProtoMember(2)] public string Label12 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D14), DataFormat = DataFormat.Group)]
    public class D13 : D12
    {
        [ProtoMember(1)] public int Value13 { get; set; }
        [ProtoMember(2)] public string Label13 { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1000, typeof(D15), DataFormat = DataFormat.Group)]
    public class D14 : D13
    {
        [ProtoMember(1)] public int Value14 { get; set; }
        [ProtoMember(2)] public string Label14 { get; set; }
    }

    [ProtoContract]
    public class D15 : D14
    {
        [ProtoMember(1)] public int Value15 { get; set; }
        [ProtoMember(2)] public string Label15 { get; set; }
    }

}
#endif
