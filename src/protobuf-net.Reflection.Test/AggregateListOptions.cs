using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    /// <summary>
    /// Covers the list form of aggregate option values, i.e. `foo: [a, b]` and
    /// `foo: [{..}, {..}]`, which protoc accepts for repeated fields.
    /// </summary>
    public class AggregateListOptions
    {
        private const int MetaFieldNumber = 1088;

        private const string Preamble = @"
syntax = ""proto3"";
package rtest;
import ""google/protobuf/descriptor.proto"";

message Meta {
    string doc = 1;
    repeated string tags = 3;
    repeated Rule rules = 4;
}
message Rule {
    string name = 1;
    string expr = 3;
}
extend google.protobuf.FieldOptions { Meta field_meta = 1088; }
extend google.protobuf.MessageOptions { Meta message_meta = 1088; }
";

        /// <summary>
        /// The custom option as read back from the resolved extension data; this is what
        /// consumers see, so it exercises both the parser and the option hive.
        /// </summary>
        private sealed class Meta
        {
            public string Doc { get; set; }
            public List<string> Tags { get; } = new List<string>();
            public List<Rule> Rules { get; } = new List<Rule>();
        }

        private sealed class Rule
        {
            public string Name { get; set; }
            public string Expr { get; set; }
        }

        private static Meta ParseFieldMeta(string body)
            => Parse(body, msg => msg.Fields[0].Options);

        private static Meta Parse(string body, Func<DescriptorProto, IExtensible> selector)
        {
            var set = new FileDescriptorSet();
            using (var reader = new StringReader(Preamble + body))
            {
                Assert.True(set.Add("list_options.proto", true, reader));
            }
            set.Process();
            Assert.Empty(set.GetErrors());

            var options = selector(set.Files[0].MessageTypes.Find(x => x.Name == "Subject"));
            var extension = options?.GetExtensionObject(false);
            Assert.NotNull(extension);

            var stream = extension.BeginQuery();
            try
            {
                var state = ProtoReader.State.Create(stream, null);
                try
                {
                    return ReadMeta(ref state);
                }
                finally
                {
                    state.Dispose();
                }
            }
            finally
            {
                extension.EndQuery(stream);
            }
        }

        private static Meta ReadMeta(ref ProtoReader.State state)
        {
            var meta = new Meta();
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                if (field != MetaFieldNumber)
                {
                    state.SkipField();
                    continue;
                }
                var outer = state.StartSubItem();
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            meta.Doc = state.ReadString();
                            break;
                        case 3:
                            meta.Tags.Add(state.ReadString());
                            break;
                        case 4:
                            meta.Rules.Add(ReadRule(ref state));
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                state.EndSubItem(outer);
            }
            return meta;
        }

        private static Rule ReadRule(ref ProtoReader.State state)
        {
            var rule = new Rule();
            var token = state.StartSubItem();
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        rule.Name = state.ReadString();
                        break;
                    case 3:
                        rule.Expr = state.ReadString();
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            state.EndSubItem(token);
            return rule;
        }

        [Fact]
        public void ListOfAggregatesKeepsEveryElement()
        {
            var meta = ParseFieldMeta(@"
message Subject {
    string id = 1 [(rtest.field_meta) = {
        rules: [
            { name: ""a"", expr: ""e1"" },
            { name: ""b"", expr: ""e2"" }
        ]
    }];
}");
            Assert.Collection(meta.Rules,
                x => { Assert.Equal("a", x.Name); Assert.Equal("e1", x.Expr); },
                x => { Assert.Equal("b", x.Name); Assert.Equal("e2", x.Expr); });
        }

        [Fact]
        public void ListOfScalarsKeepsEveryElement()
        {
            var meta = ParseFieldMeta(@"
message Subject {
    string id = 1 [(rtest.field_meta) = { tags: [ ""PII"", ""SENSITIVE"" ] }];
}");
            Assert.Equal(new[] { "PII", "SENSITIVE" }, meta.Tags);
        }

        [Fact]
        public void TrailingCommaIsAllowed()
        {
            var meta = ParseFieldMeta(@"
message Subject {
    string id = 1 [(rtest.field_meta) = { tags: [ ""x"", ""y"", ] }];
}");
            Assert.Equal(new[] { "x", "y" }, meta.Tags);
        }

        [Fact]
        public void EmptyListAddsNothing()
        {
            var meta = ParseFieldMeta(@"
message Subject {
    string id = 1 [(rtest.field_meta) = { doc: ""d"", rules: [] }];
}");
            Assert.Equal("d", meta.Doc);
            Assert.Empty(meta.Rules);
        }

        [Fact]
        public void ListCombinesWithOtherValues()
        {
            var meta = ParseFieldMeta(@"
message Subject {
    string id = 1 [(rtest.field_meta) = {
        doc: ""docs""
        rules: [ { name: ""r1"", expr: ""e1"" } ]
        tags: [ ""t1"" ]
    }];
}");
            Assert.Equal("docs", meta.Doc);
            Assert.Equal(new[] { "t1" }, meta.Tags);
            var rule = Assert.Single(meta.Rules);
            Assert.Equal("r1", rule.Name);
            Assert.Equal("e1", rule.Expr);
        }

        [Fact]
        public void ListElementsAppendToEarlierBlocks()
        {
            var meta = ParseFieldMeta(@"
message Subject {
    string id = 1 [(rtest.field_meta) = {
        rules { name: ""m1"" }
        rules: [ { name: ""m2"" }, { name: ""m3"" } ]
    }];
}");
            Assert.Equal(new[] { "m1", "m2", "m3" }, meta.Rules.ConvertAll(x => x.Name));
        }

        [Fact]
        public void ListWorksOnMessageOptions()
        {
            var meta = Parse(@"
message Subject {
    option (rtest.message_meta) = {
        rules: [ { name: ""mA"" }, { name: ""mB"" } ]
    };
    string id = 1;
}", msg => msg.Options);
            Assert.Equal(new[] { "mA", "mB" }, meta.Rules.ConvertAll(x => x.Name));
        }
        [Fact]
        public void SeparateListStatementsAppendRatherThanCollide()
        {
            var meta = ParseFieldMeta(@"
message Subject {
    string id = 1 [(rtest.field_meta) = {
        rules: [ { name: ""a"" } ]
        rules: [ { name: ""b"", expr: ""e2"" } ]
    }];
}");
            // Each element gets its own index for the whole parse, so the second statement
            // appends instead of merging into the first element.
            Assert.Equal(new[] { "a", "b" }, meta.Rules.ConvertAll(x => x.Name));
            Assert.Equal("e2", meta.Rules[1].Expr);
        }

        [Fact]
        public void SeparateScalarListStatementsKeepTheirWrittenOrder()
        {
            var meta = ParseFieldMeta(@"
message Subject {
    string id = 1 [(rtest.field_meta) = {
        tags: [ ""a"", ""b"" ]
        tags: [ ""c"" ]
    }];
}");
            Assert.Equal(new[] { "a", "b", "c" }, meta.Tags);
        }

        [Fact]
        public void LongListsKeepTheirWrittenOrder()
        {
            // Every element is a same-field-number sibling, so the sub-field sort has 20 ties
            // to resolve. List<T>.Sort only behaves stably below its insertion-sort cutoff of
            // 16, so a shorter list would not catch a reordering here.
            const int count = 20;
            var body = new StringBuilder("\nmessage Subject {\n    string id = 1 [(rtest.field_meta) = {\n        rules: [\n");
            for (int i = 0; i < count; i++)
            {
                body.Append("            { name: \"r").Append(i).Append("\" }");
                body.Append(i == count - 1 ? "\n" : ",\n");
            }
            body.Append("        ]\n    }];\n}");

            var meta = ParseFieldMeta(body.ToString());
            var expected = new List<string>();
            for (int i = 0; i < count; i++)
            {
                expected.Add("r" + i);
            }
            Assert.Equal(expected, meta.Rules.ConvertAll(x => x.Name));
        }
    }
}
