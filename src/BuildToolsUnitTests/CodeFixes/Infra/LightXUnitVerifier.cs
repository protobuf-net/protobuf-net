using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;
using Xunit.Sdk;

namespace BuildToolsUnitTests.CodeFixes.Infra
{
    public class LightXUnitVerifier : XUnitVerifier
    {
        public LightXUnitVerifier()
            : this(ImmutableStack<string>.Empty)
        {
        }

        public LightXUnitVerifier(ImmutableStack<string> context)
            : base(context)
        {
        }
        
        public override IVerifier PushContext(string context)
        {
            Assert.IsType<LightXUnitVerifier>(this);
            return new LightXUnitVerifier(Context.Push(context));
        }
        
        public override void Equal<T>(T expected, T actual, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.Equal(expected, actual);
            }
            else
            {
                if (expected is string expectedStr && actual is string actualStr)
                {
                    try
                    {
                        Assert.Equal(expectedStr, actualStr, ignoreLineEndingDifferences: true);
                    }
                    catch (EqualException _)
                    {
                        throw new EqualWithMessageException(expected, actual, CreateMessage(message));
                    }
                }
                else
                {
                    if (!EqualityComparer<T>.Default.Equals(expected, actual))
                    {
                        throw new EqualWithMessageException(expected, actual, CreateMessage(message));
                    }
                }
            }
        }
    }
}