using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    /// <summary>
    /// Shared-instance preservation through a "stub" surrogate: once an object has been
    /// written, the surrogate conversion emits a stub that carries only its ReferenceId
    /// (no sub-type tag), and on read that stub must resolve to the instance already
    /// materialised. The instances are tracked by a thread-local cache keyed on the
    /// ReferenceId (Dictionary Equals/GetHashCode overrides), across nested
    /// Model.Serialize/Deserialize calls.
    ///
    /// Scenario:
    ///   Collection [0]=leaf_full, [1]=container_full
    ///   container.Payload = leaf  (embedded as nested bytes wrapping a STUB ItemSurrogate
    ///                              that carries only the ReferenceId)
    /// Expected: deserialized container.Payload must ReferenceEquals deserialized [0].
    /// </summary>
    public class StubSurrogateSharedInstanceTests
    {
        #region Reference cache

        internal interface ISurrogateWithRefId
        {
            int ReferenceId { get; set; }
        }

        internal sealed class Cache
        {
            private readonly ConditionalWeakTable<object, ISurrogateWithRefId> _writing = new();
            private readonly Dictionary<ISurrogateWithRefId, object> _reading = new();
            private int _counter = 1;

            public void Reset()
            {
                _counter = 1;
                _reading.Clear();
            }

            public void AddWrite(object key, ISurrogateWithRefId value)
            {
                if (value.ReferenceId == -1) value.ReferenceId = _counter++;
                _writing.Add(key, value);
            }

            public void AddRead(ISurrogateWithRefId key, object value)
            {
                if (key.ReferenceId == -1) key.ReferenceId = _counter++;
                _reading.Add(key, value);
            }

            public ISurrogateWithRefId GetWrite(object key)
                => _writing.TryGetValue(key, out var v) ? v : null;

            public object GetRead(ISurrogateWithRefId key)
                => _reading.TryGetValue(key, out var v) ? v : null;
        }

        private static readonly Cache s_cache = new();

        #endregion

        #region Domain

        public abstract class Item
        {
            public object Payload;
        }

        public class LeafItem : Item
        {
            public double Radius;
        }

        public class ContainerItem : Item
        {
            public string Name;
        }

        #endregion

        #region Surrogate

        // Wrapper that carries Item.Payload across the wire as a raw-byte blob.
        [ProtoContract]
        public sealed class BlobHolder
        {
            public BlobHolder() { }
            public BlobHolder(object obj) { Object = obj; }
            public object Object;
        }

        public sealed class BlobHolderSurrogate
        {
            [ProtoMember(1)] internal byte[] _object;

            public static implicit operator BlobHolderSurrogate(BlobHolder src)
            {
                if (src == null) return null;
                var s = new BlobHolderSurrogate();
                if (src.Object != null)
                {
                    using var ms = new MemoryStream();
                    var model = CurrentModel;
                    var type = src.Object.GetType();
                    model.SerializeWithLengthPrefix(ms, type.AssemblyQualifiedName, typeof(string), PrefixStyle.Base128, 1);
                    model.Serialize(ms, src.Object);
                    s._object = ms.ToArray();
                }
                return s;
            }

            public static implicit operator BlobHolder(BlobHolderSurrogate s)
            {
                if (s == null) return null;
                var p = new BlobHolder();
                if (s._object != null)
                {
                    using var ms = new MemoryStream(s._object);
                    var model = CurrentModel;
                    var typeName = (string)model.DeserializeWithLengthPrefix(ms, null, typeof(string), PrefixStyle.Base128, 1);
                    var type = Type.GetType(typeName);
                    p.Object = model.Deserialize(ms, null, type);
                }
                return p;
            }
        }

        public class ItemSurrogate : ISurrogateWithRefId
        {
            public ItemSurrogate() { }
            public ItemSurrogate(int refId) { ReferenceId = refId; }

            public int ReferenceId { get; set; } = -1;
            public BlobHolder Payload;

            public override bool Equals(object obj)
                => (ReferenceId > 0 && obj is ISurrogateWithRefId o && o.ReferenceId == ReferenceId);

            public override int GetHashCode() => ReferenceId > 0 ? ReferenceId : base.GetHashCode();

            public static implicit operator ItemSurrogate(Item source)
            {
                if (source == null) return null;
                if (s_cache.GetWrite(source) is ItemSurrogate cached)
                    return new ItemSurrogate(cached.ReferenceId); // stub: RefId only

                ItemSurrogate s = source switch
                {
                    LeafItem a => new LeafItemSurrogate { Radius = a.Radius },
                    ContainerItem c => new ContainerItemSurrogate { Name = c.Name },
                    _ => throw new NotSupportedException()
                };
                if (source.Payload != null)
                    s.Payload = new BlobHolder(source.Payload);

                s_cache.AddWrite(source, s);
                return s;
            }

            public static implicit operator Item(ItemSurrogate s)
            {
                if (s == null) return null;
                if (s_cache.GetRead(s) is Item cached) return cached;

                Item e = s switch
                {
                    LeafItemSurrogate a => new LeafItem { Radius = a.Radius },
                    ContainerItemSurrogate c => new ContainerItem { Name = c.Name },
                    _ => null
                };
                if (e == null) return null; // stub read but not yet materialized -> should have been cache hit

                s_cache.AddRead(s, e);
                if (s.Payload != null) e.Payload = s.Payload.Object;
                return e;
            }
        }

        public class LeafItemSurrogate : ItemSurrogate
        {
            public double Radius;
        }

        public class ContainerItemSurrogate : ItemSurrogate
        {
            public string Name;
        }

        #endregion

        [ThreadStatic] private static RuntimeTypeModel _currentModel;
        private static RuntimeTypeModel CurrentModel => _currentModel;

        private static RuntimeTypeModel BuildModel()
        {
            var m = RuntimeTypeModel.Create();

            m.Add(typeof(BlobHolder), false).SetSurrogate(typeof(BlobHolderSurrogate));
            m[typeof(BlobHolderSurrogate)].Add(1, "_object").UseConstructor = false;

            var ent = m.Add(typeof(Item), false);
            ent.AddSubType(201, typeof(LeafItem));
            ent.AddSubType(202, typeof(ContainerItem));
            ent.SetSurrogate(typeof(ItemSurrogate));

            var entSur = m[typeof(ItemSurrogate)];
            entSur.Add(10, "Payload");
            entSur.Add(100, "ReferenceId");
            entSur.UseConstructor = false;
            entSur.AddSubType(201, typeof(LeafItemSurrogate));
            entSur.AddSubType(202, typeof(ContainerItemSurrogate));

            m[typeof(LeafItemSurrogate)].Add(1, "Radius").UseConstructor = false;
            m[typeof(ContainerItemSurrogate)].Add(1, "Name").UseConstructor = false;

            return m;
        }

        [Fact]
        public void StubSurrogate_Resolves_To_Earlier_Instance()
        {
            _currentModel = BuildModel();
            s_cache.Reset();

            var leaf = new LeafItem { Radius = 7 };
            var container = new ContainerItem { Name = "c" };
            container.Payload = leaf;

            var list = new List<Item> { leaf, container };

            using var ms = new MemoryStream();
            _currentModel.Serialize(ms, list);

            ms.Position = 0;
            s_cache.Reset();
            var round = (List<Item>)_currentModel.Deserialize(ms, null, typeof(List<Item>));

            Assert.NotNull(round);
            Assert.Equal(2, round.Count);
            Assert.IsType<LeafItem>(round[0]);
            Assert.IsType<ContainerItem>(round[1]);

            // The assertion that matters:
            Assert.True(ReferenceEquals(round[1].Payload, round[0]),
                "container.Payload must be the same instance as list[0] (reference preservation via ReferenceId).");
        }
    }
}
