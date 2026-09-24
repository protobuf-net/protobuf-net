using Google.Protobuf.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.IO;

namespace ProtoBuf.AotColdStart;

/// <summary>
/// Cold-start measurement: how long until the *first* serialize completes, in a fresh process.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately not a benchmark loop. The thing being measured happens exactly once per
/// process — the runtime model inspecting metadata and emitting IL for each contract on first use —
/// so running it N times and dividing measures the opposite of what is wanted: iterations 2..N are
/// all warm, and they would drown the one number that matters.
/// </para>
/// <para>
/// So: one operation, one process, and the *process* is what gets repeated. Each run prints one
/// line; the caller launches it many times and takes a median.
/// </para>
/// <para>
/// Two clocks, because they answer different questions. The internal one starts at the top of
/// <c>Main</c> and isolates the serialization work. The caller's wall clock includes host startup,
/// which is what a user actually feels — and which is itself very different between a JIT and a
/// native build, so quoting only one of the two would flatter whichever mode you preferred.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        var started = Stopwatch.GetTimestamp();
        var mode = args.Length == 0 ? "generated" : args[0];

        long bytes;
        switch (mode)
        {
            case "baseline":
                // does no serialization at all: the cost of merely starting and reaching this line,
                // so the other two can be read net of it
                bytes = 0;
                break;
            case "vanilla":
                bytes = Run(RuntimeTypeModel.Default);
                break;

            // the scaling axis: three disjoint contract sets, so the only thing that varies between
            // these runs is *how many distinct types* the model has to inspect on first use
            case "scale25":
                bytes = RunSynthetic(new Synthetic.SmallRoot());
                break;
            case "scale100":
                bytes = RunSynthetic(new Synthetic.MediumRoot());
                break;
            case "scale400":
                bytes = RunSynthetic(new Synthetic.LargeRoot());
                break;
            // the same 400 contracts through the generated model: the contrast is the argument
            case "scale400gen":
                {
                    using var ms = new MemoryStream();
                    DescriptorModel.Instance.Serialize(ms, new Synthetic.LargeRoot());
                    bytes = ms.Length;
                }
                break;
            case "generated":
                bytes = Run(DescriptorModel.Instance);
                break;
            default:
                Console.Error.WriteLine($"unknown mode '{mode}'");
                return 1;
        }

        var elapsed = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine($"{mode}\t{elapsed:0.000}\t{bytes}");
        return 0;
    }

    /// <summary>
    /// One serialize of a synthetic root through the *runtime* model, which is the arm whose cost is
    /// claimed to scale with contract count.
    /// </summary>
    private static long RunSynthetic<T>(T value)
    {
        using var ms = new MemoryStream();
        // PBN3010 is *correct* here and suppressed deliberately: the runtime model is the thing being
        // measured. Pleasingly, the analyzer found this by itself while the harness was being written.
#pragma warning disable PBN3010
        RuntimeTypeModel.Default.Serialize(ms, value);
#pragma warning restore PBN3010
        return ms.Length;
    }

    /// <summary>
    /// One serialize of a populated descriptor tree — the first thing this process does with
    /// protobuf-net, and the only thing it will do.
    /// </summary>
    private static long Run(TypeModel model)
    {
        var set = Build();
        using var ms = new MemoryStream();
        model.Serialize(ms, set);
        return ms.Length;
    }

    /// <summary>
    /// A descriptor set wide enough to touch a decent share of the contract closure: files, nested
    /// messages, fields of several shapes, enums, services and options. The point is the *number of
    /// distinct contracts* first-used, since that is what the runtime model pays per type.
    /// </summary>
    private static FileDescriptorSet Build()
    {
        var set = new FileDescriptorSet();
        for (var f = 0; f < 4; f++)
        {
            var file = new FileDescriptorProto
            {
                Name = $"file{f}.proto",
                Package = $"demo.p{f}",
                Syntax = "proto3",
                Options = new Google.Protobuf.Reflection.FileOptions
                {
                    OptimizeFor = Google.Protobuf.Reflection.FileOptions.OptimizeMode.Speed,
                },
            };
            file.Dependencies.Add("google/protobuf/any.proto");

            for (var m = 0; m < 8; m++)
            {
                var message = new DescriptorProto { Name = $"Message{m}" };
                for (var i = 1; i <= 12; i++)
                {
                    message.Fields.Add(new FieldDescriptorProto
                    {
                        Name = $"field_{i}",
                        Number = i,
                        type = (i % 3) switch
                        {
                            0 => FieldDescriptorProto.Type.TypeString,
                            1 => FieldDescriptorProto.Type.TypeInt32,
                            _ => FieldDescriptorProto.Type.TypeBool,
                        },
                        label = FieldDescriptorProto.Label.LabelOptional,
                        JsonName = $"field{i}",
                        Options = new FieldOptions { Deprecated = i % 5 == 0 },
                    });
                }
                message.NestedTypes.Add(new DescriptorProto { Name = "Nested" });
                file.MessageTypes.Add(message);
            }

            var status = new EnumDescriptorProto { Name = "Status" };
            status.Values.Add(new EnumValueDescriptorProto { Name = "UNKNOWN", Number = 0 });
            status.Values.Add(new EnumValueDescriptorProto { Name = "OK", Number = 1 });
            file.EnumTypes.Add(status);

            var service = new ServiceDescriptorProto { Name = "Service" };
            service.Methods.Add(new MethodDescriptorProto
            {
                Name = "Call",
                InputType = ".demo.Message0",
                OutputType = ".demo.Message1",
            });
            file.Services.Add(service);

            set.Files.Add(file);
        }
        return set;
    }
}

[ProtoModel]
[ProtoSerializable(typeof(FileDescriptorSet))]
[ProtoSerializable(typeof(Synthetic.LargeRoot))]
public partial class DescriptorModel : TypeModel
{
}
