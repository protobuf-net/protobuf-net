using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;
using System.Linq;

namespace ProtoBuf.AotSmoke;

[ProtoContract]
public class Customer
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
}

[ProtoContract]
public class Order
{
    [ProtoMember(1)] public int Number { get; set; }
    [ProtoMember(2)] public string Description { get; set; }
    [ProtoMember(3)] public Customer Customer { get; set; }
    [ProtoMember(4)] public byte[] Payload { get; set; }
    [ProtoMember(5)] public Status Status { get; set; }
    [ProtoMember(6)] public double? Weight { get; set; }
}

public enum Status { Unknown = 0, Open = 1, Closed = 2 }

[ProtoModel]
[ProtoSerializable(typeof(Order))]
public partial class SmokeModel : TypeModel
{
}

internal static class Program
{
    /// <summary>
    /// Round-trips through the generated model using only the generic APIs, so nothing is resolved
    /// reflectively. Returns 0 on success; any failure is reported and returns non-zero.
    /// </summary>
    private static int Main()
    {
        var failures = 0;

        var model = new SmokeModel();
        var original = new Order
        {
            Number = 42,
            Description = "hello",
            Customer = new Customer { Id = 7, Name = "marc" },
            Payload = [1, 2, 3],
            Status = Status.Closed,
            Weight = 2.5d,
        };

        using var ms = new MemoryStream();
        model.Serialize(ms, original);
        var bytes = ms.ToArray();
        Console.WriteLine($"serialized {bytes.Length} bytes: {BitConverter.ToString(bytes)}");

        ms.Position = 0;
        var clone = model.Deserialize<Order>(ms);

        Check(ref failures, "Number", original.Number, clone.Number);
        Check(ref failures, "Description", original.Description, clone.Description);
        Check(ref failures, "Customer.Id", original.Customer.Id, clone.Customer?.Id);
        Check(ref failures, "Customer.Name", original.Customer.Name, clone.Customer?.Name);
        Check(ref failures, "Payload", BitConverter.ToString(original.Payload),
            clone.Payload is null ? null : BitConverter.ToString(clone.Payload));
        Check(ref failures, "Status", original.Status, clone.Status);
        Check(ref failures, "Weight", original.Weight, clone.Weight);

        // and the bytes must be stable across a second pass
        using var second = new MemoryStream();
        model.Serialize(second, clone);
        Check(ref failures, "round-trip bytes", BitConverter.ToString(bytes),
            BitConverter.ToString(second.ToArray()));

        Console.WriteLine(failures == 0 ? "AOT smoke test PASSED" : $"AOT smoke test FAILED ({failures})");
        return failures;
    }

    private static void Check<T>(ref int failures, string what, T expected, T actual)
    {
        if (Equals(expected, actual)) return;
        Console.Error.WriteLine($"  MISMATCH {what}: expected '{expected}', got '{actual}'");
        failures++;
    }
}
