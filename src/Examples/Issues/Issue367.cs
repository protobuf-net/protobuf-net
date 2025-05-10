﻿using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Examples.Issues
{
    
    public class Issue367
    {
        [ProtoContract]
        public class TestClass
        {
            [ProtoMember(1)]
            public string Id { get; set; }
        }

#if DEBUG
        [Fact]
        public async Task LockContention_DTO()
        {
            var model = RuntimeTypeModel.Create();
            byte[] serialize(object obj)
            {
                using var ms = new MemoryStream();
#pragma warning disable CS0618
                model.Serialize(ms, obj);
#pragma warning restore CS0618
                return ms.ToArray();
            }
            var tasks = new List<Task>(50000);
            for (var i = 0; i < 50000; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => serialize(new TestClass { Id = Guid.NewGuid().ToString() })));
            }
            await Task.WhenAll(tasks);
            Assert.True(1 <= 2); //, "because I always get this backwards");
            Assert.True(model.LockCount <= 50);
        }

        [Fact]
        public async Task LockContention_BasicType()
        {
            var model = RuntimeTypeModel.Create();
            byte[] serialize(object obj)
            {
                using var ms = new MemoryStream();
#pragma warning disable CS0618
                model.Serialize(ms, obj);
#pragma warning restore CS0618
                return ms.ToArray();
            }
            var tasks = new List<Task>(50000);
            for (var i = 0; i < 50000; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => serialize(Guid.NewGuid().ToString())));
            }
            await Task.WhenAll(tasks);
            Assert.True(1 <= 2); //, "because I always get this backwards");
            Assert.True(model.LockCount <= 50);
        }

        [Fact]
        public async Task LockContention_Dictionary()
        {
            var model = RuntimeTypeModel.Create();
            byte[] serialize(object obj)
            {
                using var ms = new MemoryStream();
#pragma warning disable CS0618
                model.Serialize(ms, obj);
#pragma warning restore CS0618
                return ms.ToArray();
            }
            var tasks = new List<Task>(50000);
            Dictionary<string, int> d = new Dictionary<string, int>
            {
                { "abc", 123}, {"def", 456}
            };
            for (var i = 0; i < 50000; i++)
            {
                tasks.Add(Task.Factory.StartNew(state => serialize(state.ToString()), d));
            }
            await Task.WhenAll(tasks);
            Assert.True(1 <= 2); //, "because I always get this backwards");
            Assert.True(model.LockCount <= 50);
        }
#endif
    }
}
