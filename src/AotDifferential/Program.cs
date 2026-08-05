using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProtoBuf.AotDifferential;

/// <summary>
/// Runs the AOT generator over every <c>[ProtoContract]</c> in the already-built target assemblies —
/// as <c>AotCoverage</c> does — and then <em>executes</em> the result, comparing its bytes against
/// <see cref="RuntimeTypeModel"/>'s for a populated instance of each contract.
/// </summary>
/// <remarks>
/// The coverage sweep proves the generated code compiles. That is not the property that matters:
/// every serious bug this generator has had — the DataFormat cast that silently mis-mapped, the BCL
/// element wire types, OverwriteList on a bytes member — compiled perfectly and wrote the wrong
/// bytes. This is the same comparison <c>AotConformanceTests</c> makes, at corpus scale instead of
/// against fifty hand-written fixtures.
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        var corpus = new Corpus();
        if (corpus.Build(args.Length != 0 ? args : Corpus.DefaultTargets().ToArray()) is { } failure)
        {
            Console.Error.WriteLine(failure);
            return 1;
        }

        var generated = (TypeModel)Activator.CreateInstance(corpus.ModelType);

        // One reference model holding the *whole* corpus, not a fresh one per contract. The generated
        // model is a closed world over every contract at once, so a reference that has only heard of
        // the type under test is a differently-configured model, not ref-emit: an implementation
        // whose hierarchy root is an interface, for instance, is a standalone contract until the root
        // is also present, and then serializes without its sub-type marker. Everything has to be
        // added before anything is serialized - protobuf-net refuses to change a model once a
        // serializer has been generated from it.
        var reference = RuntimeTypeModel.Create();
        reference.AutoCompile = false;
        var refused = new Dictionary<Type, string>();
        foreach (var contract in corpus.Contracts)
        {
            try { reference.Add(contract, applyDefaultBehaviour: true); }
            catch (Exception ex) { refused[contract] = Summarize(ex); }
        }

        var filler = new Filler();
        var outcomes = new Dictionary<Outcome, int>();
        var failures = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var contract in corpus.Contracts)
        {
            var (outcome, detail) = refused.TryGetValue(contract, out var why)
                ? (Outcome.ReferenceRefused, "the reference model refused the contract: " + why)
                : Compare(generated, reference, filler, contract);
            outcomes[outcome] = outcomes.TryGetValue(outcome, out var n) ? n + 1 : 0 + 1;
            if (detail is null) continue;
            if (!failures.TryGetValue(detail, out var examples))
            {
                failures[detail] = examples = [];
            }
            if (examples.Count < 3) examples.Add(contract.FullName);
        }

        Report(corpus, outcomes, failures);
        return 0;
    }

    private enum Outcome
    {
        /// <summary>Both models produced the same bytes. The point of the exercise.</summary>
        Match,
        /// <summary>The bytes differ — a generator bug, and the reason this harness exists.</summary>
        Mismatch,
        /// <summary>The generated model threw where the reference did not, or vice versa.</summary>
        Threw,
        /// <summary>Both threw, in the same way: a shape protobuf-net does not handle either.</summary>
        BothThrew,
        /// <summary>No instance could be built, so there was nothing to compare.</summary>
        NotBuildable,
        /// <summary>
        /// <see cref="RuntimeTypeModel"/> would not even build a serializer for the contract, so
        /// there is no reference behaviour to compare against. Mostly deliberately-invalid fixtures.
        /// </summary>
        ReferenceRefused,
    }

    private static (Outcome, string) Compare(TypeModel generated, TypeModel reference,
        Filler filler, Type contract)
    {
        var instance = filler.Create(contract);
        if (instance is null)
        {
            return (Outcome.NotBuildable, "could not build an instance: " + (filler.LastFailure ?? "no route"));
        }

        var fromGenerated = TrySerialize(generated, instance, out var generatedError);
        var fromReference = TrySerialize(reference, instance, out var referenceError);

        if (generatedError is not null && referenceError is not null)
        {
            // both refuse it: a shape protobuf-net does not support, so we are matching it
            return (Outcome.BothThrew, null);
        }
        if (generatedError is not null)
        {
            return (Outcome.Threw, "generated model threw, reference did not: " + generatedError);
        }
        if (referenceError is not null)
        {
            return (Outcome.Threw, "reference threw, generated model did not: " + referenceError);
        }
        if (fromGenerated.AsSpan().SequenceEqual(fromReference))
        {
            return (Outcome.Match, null);
        }
        return (Outcome.Mismatch, "bytes differ" + Describe(fromGenerated, fromReference));
    }

    private static byte[] TrySerialize(TypeModel model, object instance, out string error)
    {
        try
        {
            using var ms = new MemoryStream();
            model.Serialize(ms, instance);
            error = null;
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            error = Summarize(ex);
            return null;
        }
    }

    /// <summary>Where the two payloads part company, which is usually enough to name the cause.</summary>
    private static string Describe(byte[] generated, byte[] reference)
    {
        var shared = Math.Min(generated.Length, reference.Length);
        var at = 0;
        while (at < shared && generated[at] == reference[at]) at++;
        return $" at byte {at} (generated {generated.Length}b, reference {reference.Length}b): "
            + $"generated {Hex(generated, at)} vs reference {Hex(reference, at)}";
    }

    private static string Hex(byte[] bytes, int from)
    {
        if (from >= bytes.Length) return "<end>";
        return BitConverter.ToString(bytes, from, Math.Min(6, bytes.Length - from));
    }

    private static string Summarize(Exception ex)
    {
        while (ex is TypeInitializationException or AggregateException && ex.InnerException is not null)
        {
            ex = ex.InnerException;
        }
        var message = ex.Message.Replace("\r", " ").Replace("\n", " ");
        // the type name is what varies between otherwise-identical failures, so drop it: the point
        // of grouping is that one cause counts once
        message = System.Text.RegularExpressions.Regex.Replace(message, @"[\w\.\+`\[\]<>,]{8,}", "…");
        return ex.GetType().Name + ": " + (message.Length > 110 ? message[..110] + "…" : message);
    }

    private static void Report(Corpus corpus, Dictionary<Outcome, int> outcomes,
        Dictionary<string, List<string>> failures)
    {
        int Count(Outcome outcome) => outcomes.TryGetValue(outcome, out var n) ? n : 0;

        Console.WriteLine("<!-- generated by src/AotDifferential; re-run it after any generator change -->");
        Console.WriteLine();
        Console.WriteLine("# AOT generator differential");
        Console.WriteLine();
        Console.WriteLine("Every `[ProtoContract]` the generator emits, serialized from a populated");
        Console.WriteLine("instance and compared byte-for-byte against `RuntimeTypeModel`.");
        Console.WriteLine();
        Console.WriteLine($"seeded: **{corpus.Seeded}**, of which {corpus.Dropped} dropped with a diagnostic");
        foreach (var pair in corpus.Skipped.OrderByDescending(static x => x.Value))
        {
            Console.WriteLine($"- not seedable, {pair.Key}: {pair.Value}");
        }
        Console.WriteLine();

        var compared = Count(Outcome.Match) + Count(Outcome.Mismatch);
        Console.WriteLine("| outcome | count |");
        Console.WriteLine("| --- | ---: |");
        Console.WriteLine($"| **bytes match ref-emit** | {Count(Outcome.Match)} |");
        Console.WriteLine($"| **bytes differ** | {Count(Outcome.Mismatch)} |");
        Console.WriteLine($"| one model threw | {Count(Outcome.Threw)} |");
        Console.WriteLine($"| both threw (a shape protobuf-net refuses too) | {Count(Outcome.BothThrew)} |");
        Console.WriteLine($"| no instance could be built | {Count(Outcome.NotBuildable)} |");
        Console.WriteLine($"| the reference model refused the contract | {Count(Outcome.ReferenceRefused)} |");
        Console.WriteLine();
        if (compared != 0)
        {
            Console.WriteLine($"of the {compared} actually compared, "
                + $"**{Count(Outcome.Match) * 100 / compared}% match**.");
            Console.WriteLine();
        }

        if (failures.Count == 0) return;
        Console.WriteLine("## What went wrong");
        Console.WriteLine();
        foreach (var pair in failures.OrderByDescending(static x => x.Value.Count).Take(25))
        {
            Console.WriteLine($"- **{pair.Value.Count}x** {pair.Key}");
            Console.WriteLine($"  - e.g. {string.Join(", ", pair.Value)}");
        }
    }
}
