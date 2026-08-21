using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests
{
    public partial class ProtobufFieldAnalyzerTests : AnalyzerTestBase<DataContractAnalyzer>
    {
        public ProtobufFieldAnalyzerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public async Task DoesntReportOnUnrelatedCode()
        {
            var diagnostics = await AnalyzeAsync(@"
public class Foo
{
    public void Bar() {}
}");
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(18999)]
        [InlineData(20000)]
        [InlineData(536870911)]
        // The smallest field number you can specify is 1, and the largest is 229 - 1, or 536,870,911. You also cannot use the numbers 19000 through 19999
        // from https://developers.google.com/protocol-buffers/docs/proto3#assigning_field_numbers
        public async Task DoesntReportOnLegalDto(int fieldNumber)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
public class Foo
{{
    [ProtoMember({fieldNumber})]
    public int Bar {{get;set;}}
}}");
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(18999)]
        [InlineData(20000)]
        [InlineData(536870911)]
        // The smallest field number you can specify is 1, and the largest is 229 - 1, or 536,870,911. You also cannot use the numbers 19000 through 19999
        // from https://developers.google.com/protocol-buffers/docs/proto3#assigning_field_numbers
        public async Task DoesntReportOnLegalDto_Partial(int fieldNumber)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
[ProtoPartialMember({fieldNumber}, nameof(Bar))]
public class Foo
{{
    public int Bar {{get;set;}}
}}");
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task DoesntReportOnPartialClassDefinition()
        {
            var diagnostics = await AnalyzeMultiFileAsync(
                new List<string> {
@"
using ProtoBuf;
[ProtoContract(SkipConstructor = true)]
public partial class Foo
{
    [ProtoMember(2)]
    [System.ComponentModel.DefaultValue(3)]
    public int Bar {get;set;} =3;
} ",
//next file
@"
public partial class Foo
{
    public Foo(int bar){
        Bar = bar;
    }
    public Foo (){}
} "});
            // this fixture pairs SkipConstructor with [DefaultValue(3)], which PBN0026 reports and
            // is right to: the instance is built without running a constructor, so Bar arrives as 0
            // while a sender holding 3 writes nothing. Asserted as the *only* diagnostic, so the
            // test still fails if a partial declaration provokes anything else - which is its point
            var diag = Assert.Single(diagnostics);
            Assert.Equal(DataContractAnalyzer.DeclaredDefaultUnderSkipConstructor, diag.Descriptor);
        }



        [Theory]
        [InlineData(0)]
        [InlineData(-42)]
        [InlineData(19000, true)]
        [InlineData(19500, true)]
        [InlineData(19999, true)]
        [InlineData(536870912)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        // The smallest field number you can specify is 1, and the largest is 229 - 1, or 536,870,911. You also cannot use the numbers 19000 through 19999
        // from https://developers.google.com/protocol-buffers/docs/proto3#assigning_field_numbers
        public async Task ReportsOnIllegalDto(int fieldNumber, bool warningOnly = false)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
public class Foo
{{
    [ProtoMember({fieldNumber})]
    public int Bar {{get;set;}}
}}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.InvalidFieldNumber);
            Assert.Equal(warningOnly ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The specified field number {fieldNumber} is invalid; the valid range is 1-536870911, omitting 19000-19999.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsOnIllegalPartialName()
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
[ProtoPartialMember(42, ""Bar"")]
public class Foo
{{
    public int Blap {{get;set;}}
}}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.MemberNotFound);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal("The specified type member 'Bar' could not be resolved.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-42)]
        [InlineData(19000, true)]
        [InlineData(19500, true)]
        [InlineData(19999, true)]
        [InlineData(536870912)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        // The smallest field number you can specify is 1, and the largest is 229 - 1, or 536,870,911. You also cannot use the numbers 19000 through 19999
        // from https://developers.google.com/protocol-buffers/docs/proto3#assigning_field_numbers
        public async Task ReportsOnIllegalDto_Partial(int fieldNumber, bool warningOnly = false)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
[ProtoPartialMember({fieldNumber}, nameof(Bar))]
public class Foo
{{
    public int Bar {{get;set;}}
}}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.InvalidFieldNumber);
            Assert.Equal(warningOnly ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The specified field number {fieldNumber} is invalid; the valid range is 1-536870911, omitting 19000-19999.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsOnIllegalConst()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public class Foo
{
    private const int FieldNumber = -42;
    [ProtoMember(FieldNumber)]
    public int Bar {get;set;}
}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.InvalidFieldNumber);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The specified field number -42 is invalid; the valid range is 1-536870911, omitting 19000-19999.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsDuplicateFieldBetweenFields()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public class Foo
{
    [ProtoMember(1)]
    public int A {get;set;}

    [ProtoMember(2)]
    public int B {get;set;}

    [ProtoMember(2)]
    public int C {get;set;}

    [ProtoMember(3)]
    public int D {get;set;}
}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateFieldNumber);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The specified field number 2 is duplicated; field numbers must be unique between all declared members and includes on a single type.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsDuplicateFieldBetweenPartialsAndIncludes()
        {
            // note we don't need to test all combinations of fields vs partials vs includes; it is all one bucket - it
            // is sufficient that it *finds the problem*
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(3, typeof(SuperFoo))]
[ProtoPartialMember(3, ""C"")]
public class Foo
{
    [ProtoMember(1)]
    public int A {get;set;}

    [ProtoMember(2)]
    public int B {get;set;}

    public int C {get;set;}
}

[ProtoContract]
public class SuperFoo : Foo {}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateFieldNumber);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The specified field number 3 is duplicated; field numbers must be unique between all declared members and includes on a single type.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsShouldBeContract_Member()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
public class Foo
{
    [ProtoMember(1)]
    public int A {get;set;}
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ShouldBeProtoContract);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The type is not marked as a proto-contract; additional annotations will be ignored.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsShouldBeContract_Include()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoInclude(typeof(SuperFoo))]
public class Foo
{
    [ProtoMember(1)]
    public int A {get;set;}
}
[ProtoContract]
public class SuperFoo : Foo {}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ShouldBeProtoContract);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The type is not marked as a proto-contract; additional annotations will be ignored.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsShouldBeContract_Reservation()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoReserved(123)]
public class Foo
{
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ShouldBeProtoContract);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The type is not marked as a proto-contract; additional annotations will be ignored.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsBothDeclaredAndIgnored()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoPartialIgnore(""A"")]
public class Foo
{
    [ProtoMember(1)]
    public int A {get;set;}
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DeclaredAndIgnored);
            // Warning rather than Error, deliberately: [ProtoPartialIgnore] wins over everything,
            // including a [ProtoMember] the member declares itself, so this shape has *defined*
            // behaviour and builds working code. Worth flagging as a contradiction; not worth
            // breaking a build over. Partial.input.cs relies on that same precedence.
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal($"The member 'A' is marked to be ignored; additional annotations will be ignored.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsReservedField_Number()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract, ProtoReserved(42)]
public class Foo
{
    [ProtoMember(42)]
    public int A {get;set;}
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ReservedFieldNumber);
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal($"The specified field number 42 is explicitly reserved.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsReservedField_Number_Range()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract, ProtoReserved(40,50)]
public class Foo
{
    [ProtoMember(42)]
    public int A {get;set;}
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ReservedFieldNumber);
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal($"The specified field number [40-50] is explicitly reserved.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsReservedField_Name()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract, ProtoReserved(""A"")]
public class Foo
{
    [ProtoMember(42)]
    public int A {get;set;}
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ReservedFieldName);
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal($"The specified field name 'A' is explicitly reserved.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsDuplicateName()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public class Foo
{
    [ProtoMember(1, Name = ""C"")]
    public int A {get;set;}
    [ProtoMember(2, Name = ""C"")]
    public int B {get;set;}
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateFieldName);
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal($"The specified field name 'C' is duplicated; field names should be unique between all declared members on a single type.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsDuplicateReservationName()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract, ProtoReserved(""A""), ProtoReserved(""A"")]
public class Foo
{
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateReservation);
            Assert.Equal(DiagnosticSeverity.Info, diag.Severity);
            Assert.Equal($"The reservations 'A' and 'A' overlap each-other.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsDuplicateReservationNumbers()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract, ProtoReserved(10, 25), ProtoReserved(20, 30)]
public class Foo
{
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateReservation);
            Assert.Equal(DiagnosticSeverity.Info, diag.Severity);
            Assert.Equal($"The reservations [20-30] and [10-25] overlap each-other.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsDuplicateInclude()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(1, typeof(SuperFoo))]
[ProtoInclude(2, typeof(SuperFoo))]
public class Foo
{
}
[ProtoContract]
public class SuperFoo : Foo {}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateInclude);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The type 'SuperFoo' is declared as an include multiple times.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsNonDerivedInclude()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(1, typeof(SuperFoo))]
public class Foo
{
}
[ProtoContract]
public class SuperFoo {}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.IncludeNonDerived);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The type 'SuperFoo' is declared as an include, but is not a direct sub-type.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsMissingIncludeDeclaration()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public class Foo {}
[ProtoContract]
public class SuperFoo : Foo {}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.IncludeNotDeclared);
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal($"The base-type 'Foo' is a proto-contract, but no include is declared for 'SuperFoo' and the IgnoreUnknownSubTypes flag is not set.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DoesNotReportMissingIncludeDeclarationWhenIgnoreSubTypesSpecified()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract(IgnoreUnknownSubTypes = true)]
public class Foo {}
public class SuperFoo : Foo {}
");
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task ReportsIncludeNotContract()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract, ProtoInclude(3, typeof(SuperFoo))]
public class Foo {}
public class SuperFoo : Foo {}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.SubTypeShouldBeProtoContract);
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal($"The base-type 'Foo' is a proto-contract and the IgnoreUnknownSubTypes flag is not set; 'SuperFoo' should also be a proto-contract.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsMissingConstructor()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public class Foo {
    public Foo(string _) {}
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ConstructorMissing);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"There is no suitable (parameterless) constructor available for the proto-contract, and the SkipConstructor flag is not set.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DoesNotReportMissingConstructorOnAbstractType()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public abstract class Foo {
    public Foo(string _) {}
}
");
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task ReportsMissingConstructor_ExplicitNoSkip()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract(SkipConstructor = false)]
public class Foo {
    public Foo(string _) {}
}
");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ConstructorMissing);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"There is no suitable (parameterless) constructor available for the proto-contract, and the SkipConstructor flag is not set.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DoesNotReportsMissingConstructor_ExplicitSkip()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract(SkipConstructor = true)]
public class Foo {
    public Foo(string _) {}
}
");
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task SuggestsCompatibilityLevelWhenNonMarkedContracts()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public class Foo {}
", ignoreCompatibilityLevelAdvice: false);
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.MissingCompatibilityLevel);
            Assert.Equal(DiagnosticSeverity.Info, diag.Severity);
            Assert.Equal($"It is recommended to declare a module or assembly level CompatibilityLevel (or declare it for each contract type); new projects should use the highest currently available - old projects should use Level200 unless fully considered.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DoesNotSuggestCompatibilityLevelWhenNoContracts()
        {
            var diagnostics = await AnalyzeAsync(@"
public class Foo {}
", ignoreCompatibilityLevelAdvice: false);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task DoesNotSuggestCompatibilityLevelWhenNoNonMarkedContracts()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
public class Foo {}
", ignoreCompatibilityLevelAdvice: false);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task DoesNotSuggestCompatibilityLevelWhenDeclaredAtModule()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[module:CompatibilityLevel(CompatibilityLevel.Level300)]
[ProtoContract]
public class Foo {}
", ignoreCompatibilityLevelAdvice: false);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task DoesNotSuggestCompatibilityLevelWhenDeclaredAtAssembly()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[assembly:CompatibilityLevel(CompatibilityLevel.Level300)]
[ProtoContract]
public class Foo {}
", ignoreCompatibilityLevelAdvice: false);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task DoNotWarnOnEnumsWithoutValue()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public enum SomeEnum {
    None = 0,
    [ProtoEnum]
    Foo = 1
}");
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task NotifiesAboutRedundantEnumValueOverride()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public enum SomeEnum {
    None = 0,
    [ProtoEnum(Value = 1)]
    Foo = 1
}");
            // we'll overlook that the setter isn't actually available
            var diag = diagnostics.Single(x => x.Id is not "CS0618" and not "CS0619");
            Assert.Equal(DataContractAnalyzer.EnumValueRedundant, diag.Descriptor);
            Assert.Equal(DiagnosticSeverity.Info, diag.Severity);
            Assert.Equal("This ProtoEnumAttribute.Value declaration is redundant; it is recommended to omit it.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task NotifiesAboutIncompatibleEnumValueOverride()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public enum SomeEnum {
    None = 0,
    [ProtoEnum(Value = 42)]
    Foo = 1
}");
            // we'll overlook that the setter isn't actually available
            var diag = diagnostics.Single(x => x.Id is not "CS0618" and not "CS0619");
            Assert.Equal(DataContractAnalyzer.EnumValueNotSupported, diag.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity); // error since we're targeting v3
            Assert.Equal("This ProtoEnumAttribute.Value declaration conflicts with the underlying value; this scenario is not supported from protobuf-net v3 onwards.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task NotifiesAboutRedundantEnumNameOverride()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public enum SomeEnum {
    None = 0,
    [ProtoEnum(Name = ""Foo"")]
    Foo = 1
}");
            var diag = Assert.Single(diagnostics);
            Assert.Equal(DataContractAnalyzer.EnumNameRedundant, diag.Descriptor);
            Assert.Equal(DiagnosticSeverity.Info, diag.Severity);
            Assert.Equal("This ProtoEnumAttribute.Name declaration is redundant; it is recommended to omit it.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DoesNotNotifyAboutMeaningfulEnumNameOverride()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public enum SomeEnum {
    None = 0,
    [ProtoEnum(Name = ""foo"")]
    Foo = 1
}");
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task DoesNotifyAboutBasicRecordsWithConflictingFields()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
using System.ComponentModel;
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class IsExternalInit{}
}
[ProtoContract]
public record Test
{
	[ProtoMember(1)]
	public string Name { get; init; } = string.Empty;

	[ProtoMember(1)] // This does NOT get flagged with PBN0003
	public string Value { get; init; } = string.Empty;
}");
            var diag = Assert.Single(diagnostics);
            Assert.Equal(DataContractAnalyzer.DuplicateFieldNumber, diag.Descriptor);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal("The specified field number 1 is duplicated; field numbers must be unique between all declared members and includes on a single type.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task DoesNotifyAboutRecordsWithRecordConstructor()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
using System.ComponentModel;
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class IsExternalInit{}
}
[ProtoContract]
public record Test(
	[property: ProtoMember(1)]
	string Name,

	[property: ProtoMember(1)] // This does NOT get flagged with PBN0003
	string Value)
{}");
            Assert.Equal(2, diagnostics.Count);

            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateFieldNumber);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal("The specified field number 1 is duplicated; field numbers must be unique between all declared members and includes on a single type.", diag.GetMessage(CultureInfo.InvariantCulture));

            diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ConstructorMissing);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal("There is no suitable (parameterless) constructor available for the proto-contract, and the SkipConstructor flag is not set.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsProtoContractOnInterface()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(10, typeof(Bar))]
public interface IFoo
{
    [ProtoMember(1)] string Name {get;set;}
}
[ProtoContract]
public class Bar : IFoo
{
    [ProtoMember(1)] public string Name {get;set;}
}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.ProtoContractOnInterface);
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        }

        [Fact]
        public async Task DoesntReportIncludeNonDerivedForAnInterfaceRoot()
        {
            // an interface include is a legal hierarchy - protobuf-net treats implementing one exactly
            // as deriving - so PBN0012 (an *error*) must not fire on it
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(10, typeof(Bar))]
public interface IFoo { }
[ProtoContract]
public class Bar : IFoo
{
    [ProtoMember(1)] public string Name {get;set;}
}");
            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.IncludeNonDerived));
        }

        [Fact]
        public async Task DoesntReportIncludeNotDeclaredWhenLinkedOutOfBand()
        {
            // [ProtoSubType] is the answer *for* the shape PBN0013 complains about - a base that
            // cannot name its sub-type - so nagging for a [ProtoInclude] the consumer is unable to
            // write would be noise. Only the assembly and module sites are seen; see DeclaredOutOfBand
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
#pragma warning disable PBN9001
[assembly: ProtoSubType(typeof(Foo), typeof(Bar), 10)]
#pragma warning restore PBN9001
[ProtoContract]
public class Foo
{
    [ProtoMember(1)] public string Name {get;set;}
}
[ProtoContract]
public class Bar : Foo
{
    [ProtoMember(2)] public string Extra {get;set;}
}");
            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.IncludeNotDeclared));
        }

        [Fact]
        public async Task DoesReportIncludeNotDeclaredWhenTheDeclarationNamesSomethingElse()
        {
            // ...and the suppression has to be specific: a declaration about another pair says
            // nothing about this one
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
#pragma warning disable PBN9001
[assembly: ProtoSubType(typeof(Foo), typeof(Other), 10)]
#pragma warning restore PBN9001
[ProtoContract]
public class Foo
{
    [ProtoMember(1)] public string Name {get;set;}
}
[ProtoContract]
public class Other : Foo
{
    [ProtoMember(2)] public string Extra {get;set;}
}
[ProtoContract]
public class Bar : Foo
{
    [ProtoMember(3)] public string More {get;set;}
}");
            Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.IncludeNotDeclared);
        }

        [Fact]
        public async Task DoesReportIncludeOfAnUnrelatedType()
        {
            // ...but the check still has to catch a genuinely unrelated include
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(10, typeof(Bar))]
public interface IFoo { }
[ProtoContract]
public class Bar
{
    [ProtoMember(1)] public string Name {get;set;}
}");
            Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.IncludeNonDerived);
        }

        [Fact]
        public async Task DoesntReportIncludeNonDerivedForAGenericInterfaceRoot()
        {
            // the attribute lives on the *open* IFoo<T> while Bar implements the *closed* IFoo<int>,
            // so a symbol comparison finds no link - but ref-emit resolves it on both its paths, so
            // PBN0012 (an error) firing here would fail the build for a working pattern
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(10, typeof(Bar))]
public interface IFoo<T> { }
[ProtoContract]
public class Bar : IFoo<int>
{
    [ProtoMember(1)] public string Name {get;set;}
}");
            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.IncludeNonDerived));
        }

        [Fact]
        public async Task DoesntReportIncludeNonDerivedForAGenericBaseClass()
        {
            // the same shape one level down: a generic base class rather than a generic interface
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(10, typeof(Bar))]
public class Foo<T> { }
[ProtoContract]
public class Bar : Foo<int>
{
    [ProtoMember(1)] public string Name {get;set;}
}");
            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.IncludeNonDerived));
        }

        [Fact]
        public async Task DoesntReportDuplicateFieldNumberAcrossGenericConstructions()
        {
            // a generic base declares its include list once and shares it with every construction,
            // but each one matches only the includes that derive from it - so Holder<int> sees Bar
            // at 1 and Holder<string> sees Baz at 1, never two at once. PBN0003 is an *error*, so
            // reporting it here fails the build for a pattern ref-emit serializes happily
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(1, typeof(Bar))]
[ProtoInclude(1, typeof(Baz))]
public class Holder<T> { }
[ProtoContract]
public class Bar : Holder<int> { }
[ProtoContract]
public class Baz : Holder<string> { }");
            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DuplicateFieldNumber));
        }

        [Fact]
        public async Task DoesReportDuplicateFieldNumberWithinOneGenericConstruction()
        {
            // ...but two includes on the *same* construction still collide, so the relaxation is
            // per-construction rather than a blanket exemption for generics
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(1, typeof(Bar))]
[ProtoInclude(1, typeof(Baz))]
public class Holder<T> { }
[ProtoContract]
public class Bar : Holder<int> { }
[ProtoContract]
public class Baz : Holder<int> { }");
            Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateFieldNumber);
        }

        [Fact]
        public async Task DoesReportIncludeClashingWithAMemberOnAGenericType()
        {
            // and a member still cannot share a number with an include, whatever the construction:
            // the member belongs to every one of them
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(1, typeof(Bar))]
public class Holder<T>
{
    [ProtoMember(1)] public int Value {get;set;}
}
[ProtoContract]
public class Bar : Holder<int> { }");
            Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.DuplicateFieldNumber);
        }

        [Fact]
        public async Task DoesReportIncludeOfAnUnrelatedGenericType()
        {
            // ...and an unrelated generic is still caught, so the relaxation is not blanket
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
[ProtoInclude(10, typeof(Bar))]
public interface IFoo<T> { }
[ProtoContract]
public class Bar : System.Collections.Generic.List<int>
{
    [ProtoMember(1)] public string Name {get;set;}
}");
            Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.IncludeNonDerived);
        }

        /// <summary>
        /// `[DataContract]` and `[XmlType]` are contract markers in their own right, and the families
        /// *mix*: a `[DataContract]` type may pin one member with `[ProtoMember]` and let
        /// `[DataMember(Order)]` supply the rest. protobuf-net honours both — verified by serializing
        /// this shape, which gives field 1 from the DataMember and field 3 from the ProtoMember — so
        /// PBN0009's "additional annotations will be ignored" was both false and, as an error, a
        /// build break for working code.
        /// </summary>
        [Fact]
        public async Task MixedContractFamiliesAreNotReported()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
using System.Runtime.Serialization;
[DataContract]
public class Mixed
{
    [ProtoMember(3)] public int Pinned { get; set; }
    [DataMember(Order = 1)] public int Ordered { get; set; }
}");
            Assert.Empty(diagnostics.Where(x => x.Id == "PBN0009"));
        }

        /// <summary>...but a type with no contract marker at all really does ignore them.</summary>
        [Fact]
        public async Task AnnotationsWithNoContractMarkerAtAllAreStillAnError()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
public class NoContract
{
    [ProtoMember(1)] public int Nope { get; set; }
}");
            var hit = Assert.Single(diagnostics.Where(x => x.Id == "PBN0009"));
            Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, hit.Severity);
        }
    }
}

