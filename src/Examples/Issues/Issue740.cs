using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue740
    {
        [Fact]
        public void MessagesInServicesHaveFullNamespaceIfExternal()
        {
            const string mainProtoText = @"syntax = ""proto3"";
package MainPackage;

import ""child_proto_text.proto"";

service MyService {
    rpc MyMethod(ChildPackage.Request) returns (ChildPackage.Reply);
}
";

            const string childProtoText = @"syntax = ""proto3"";
package ChildPackage;

message Request { }
message Reply { }
";

            var set = new FileDescriptorSet();
            set.Add("main_proto_text.proto", source: new StringReader(mainProtoText));
            set.Add("child_proto_text.proto", source: new StringReader(childProtoText));
            set.Process();
            var errors = set.GetErrors();
            Assert.Empty(errors);

            var generateOptions = new Dictionary<string, string> { ["services"] = "true" };

            var sourceFiles = CSharpCodeGenerator
                .Default
                .Generate(set, options: generateOptions)
                .Select(x => x.Text)
                .ToArray();

            // Find the request/reply types from the service interface
            var mainProtoSource = sourceFiles[0];

            var regex = new Regex(@"$\s+global::System\.Threading\.Tasks\.ValueTask<(?<Reply>\S+)> MyMethodAsync\((?<Request>\S+) value,", RegexOptions.Multiline);
            var match = regex.Match(mainProtoSource);

            Assert.Equal("global::ChildPackage.Reply", match.Groups["Reply"].Value);
            Assert.Equal("global::ChildPackage.Request", match.Groups["Request"].Value);
        }
    }
}
