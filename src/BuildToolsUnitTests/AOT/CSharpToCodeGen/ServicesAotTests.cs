using System.Linq;
using BuildToolsUnitTests.AOT.CSharpToCodeGen.Abstractions;
using ProtoBuf.Reflection.Internal.CodeGen;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.AOT.CSharpToCodeGen;

public class ServicesAotTests : CSharpToCodeGenTestsBase
{
    private const string SchemaType = "Services";

    public ServicesAotTests(ITestOutputHelper output) : base(output, SchemaType)
    {
    }
    
    [Fact]
    public void RawReturnType_Passes()
    {
        var codeGenSet = GetCodeGenSet("RawReturnTypecs.cs", out _);
        Assert.NotNull(codeGenSet);
        
        var serviceMethod = codeGenSet.Files.First().Services.First().ServiceMethods.First();
        Assert.NotNull(serviceMethod);
        Assert.Equal(CodeGenTypeRepresentation.Raw, serviceMethod.RequestType.Representation);
        Assert.Equal(CodeGenTypeRepresentation.Raw, serviceMethod.ResponseType.Representation);
        Assert.Equal(CodeGenServiceMethodParametersDescriptor.HasCallContext | CodeGenServiceMethodParametersDescriptor.HasCancellationToken, serviceMethod.ParametersDescriptor);
    }
    
    [Fact]
    public void StreamableResponse_Passes()
    {
        var codeGenSet = GetCodeGenSet("IAsyncEnumerableReturnType.cs", out _);
        Assert.NotNull(codeGenSet);

        var serviceMethod = codeGenSet.Files.First().Services.First().ServiceMethods.First();
        Assert.NotNull(serviceMethod);
        Assert.Equal(CodeGenTypeRepresentation.AsyncEnumerable, serviceMethod.RequestType.Representation);
        Assert.Equal(CodeGenTypeRepresentation.AsyncEnumerable, serviceMethod.ResponseType.Representation);
        Assert.Equal(CodeGenServiceMethodParametersDescriptor.HasCallContext | CodeGenServiceMethodParametersDescriptor.HasCancellationToken, serviceMethod.ParametersDescriptor);
    }
    
    [Fact]
    public void TaskReturnType_Passes()
    {
        var codeGenSet = GetCodeGenSet("TaskReturnType.cs", out _);
        Assert.NotNull(codeGenSet);

        var serviceMethod = codeGenSet.Files.First().Services.First().ServiceMethods.First();
        Assert.NotNull(serviceMethod);
        Assert.Equal(CodeGenTypeRepresentation.Raw, serviceMethod.RequestType.Representation);
        Assert.Equal(CodeGenTypeRepresentation.Task, serviceMethod.ResponseType.Representation);
        Assert.Equal(CodeGenServiceMethodParametersDescriptor.HasCallContext | CodeGenServiceMethodParametersDescriptor.HasCancellationToken, serviceMethod.ParametersDescriptor);
    }
    
    [Fact]
    public void ValueTaskReturnType_Passes()
    {
        var codeGenSet = GetCodeGenSet("ValueTaskReturnType.cs", out _);
        Assert.NotNull(codeGenSet);

        var serviceMethod = codeGenSet.Files.First().Services.First().ServiceMethods.First();
        Assert.NotNull(serviceMethod);
        Assert.Equal(CodeGenTypeRepresentation.Raw, serviceMethod.RequestType.Representation);
        Assert.Equal(CodeGenTypeRepresentation.ValueTask, serviceMethod.ResponseType.Representation);
        Assert.Equal(CodeGenServiceMethodParametersDescriptor.HasCallContext | CodeGenServiceMethodParametersDescriptor.HasCancellationToken, serviceMethod.ParametersDescriptor);
    }
}