using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    /// <summary>
    /// Reproduces Eyeshot SerializeEntityToEntityData: shared-instance preservation across
    /// nested Model.Serialize/Deserialize calls, with a custom ReferenceId field and
    /// a thread-local cache that dedups via RefId (Dictionary Equals/GetHashCode overrides).
    ///
    /// v2 (Eyeshot 2025) passed. v3 inheritance+surrogate chain must preserve this.
    ///
    /// Scenario:
    ///   Collection [0]=arc_full, [1]=composite_full
    ///   composite.EntityData = arc  (embedded as nested bytes wrapping a STUB EntitySurrogate
    ///                                 that carries only the ReferenceId)
    /// Expected: deserialized composite.EntityData must ReferenceEquals deserialized [0].
    /// </summary>
    public class EyeshotEntityDataRefTests
    {
        #region Cache (mirrors Eyeshot SerializerCache, simplified)

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

        public abstract class Entity
        {
            public object EntityData;
        }

        public class Arc : Entity
        {
            public double Radius;
        }

        public class Composite : Entity
        {
            public string Name;
        }

        #endregion

        #region Surrogate

        // ProtoObject-like wrapper to carry Entity.EntityData across the wire as a raw-byte blob.
        [ProtoContract]
        public sealed class ProtoObject
        {
            public ProtoObject() { }
            public ProtoObject(object obj) { Object = obj; }
            public object Object;

            [ProtoMember(1)] internal byte[] _object;
        }

        public sealed class ProtoObjectSurrogate
        {
            [ProtoMember(1)] internal byte[] _object;

            public static implicit operator ProtoObjectSurrogate(ProtoObject src)
            {
                if (src == null) return null;
                var s = new ProtoObjectSurrogate();
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

            public static implicit operator ProtoObject(ProtoObjectSurrogate s)
            {
                if (s == null) return null;
                var p = new ProtoObject();
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

        public class EntitySurrogate : ISurrogateWithRefId
        {
            public EntitySurrogate() { ReferenceId = -1; }
            public EntitySurrogate(int refId) { ReferenceId = refId; }

            public int ReferenceId { get; set; } = -1;
            public ProtoObject EntityData;

            public override bool Equals(object obj)
                => (ReferenceId > 0 && obj is ISurrogateWithRefId o && o.ReferenceId == ReferenceId);

            public override int GetHashCode() => ReferenceId > 0 ? ReferenceId : base.GetHashCode();

            public static implicit operator EntitySurrogate(Entity source)
            {
                if (source == null) return null;
                if (s_cache.GetWrite(source) is EntitySurrogate cached)
                    return new EntitySurrogate(cached.ReferenceId); // stub: RefId only

                EntitySurrogate s = source switch
                {
                    Arc a => new ArcSurrogate { Radius = a.Radius },
                    Composite c => new CompositeSurrogate { Name = c.Name },
                    _ => throw new NotSupportedException()
                };
                if (source.EntityData != null)
                    s.EntityData = new ProtoObject(source.EntityData);

                s_cache.AddWrite(source, s);
                return s;
            }

            public static implicit operator Entity(EntitySurrogate s)
            {
                if (s == null) return null;
                if (s_cache.GetRead(s) is Entity cached) return cached;

                Entity e = s switch
                {
                    ArcSurrogate a => new Arc { Radius = a.Radius },
                    CompositeSurrogate c => new Composite { Name = c.Name },
                    _ => null
                };
                if (e == null) return null; // stub read but not yet materialized -> should have been cache hit

                s_cache.AddRead(s, e);
                if (s.EntityData != null) e.EntityData = s.EntityData.Object;
                return e;
            }
        }

        public class ArcSurrogate : EntitySurrogate
        {
            public double Radius;
        }

        public class CompositeSurrogate : EntitySurrogate
        {
            public string Name;
        }

        #endregion

        [ThreadStatic] private static RuntimeTypeModel _currentModel;
        private static RuntimeTypeModel CurrentModel => _currentModel;

        private static RuntimeTypeModel BuildModel()
        {
            var m = RuntimeTypeModel.Create();

            m.Add(typeof(ProtoObject), false).SetSurrogate(typeof(ProtoObjectSurrogate));
            m[typeof(ProtoObjectSurrogate)].Add(1, "_object").UseConstructor = false;

            var ent = m.Add(typeof(Entity), false);
            ent.AddSubType(201, typeof(Arc));
            ent.AddSubType(202, typeof(Composite));
            ent.SetSurrogate(typeof(EntitySurrogate));

            var entSur = m[typeof(EntitySurrogate)];
            entSur.Add(10, "EntityData");
            entSur.Add(100, "ReferenceId");
            entSur.UseConstructor = false;
            entSur.AddSubType(201, typeof(ArcSurrogate));
            entSur.AddSubType(202, typeof(CompositeSurrogate));

            m[typeof(ArcSurrogate)].Add(1, "Radius").UseConstructor = false;
            m[typeof(CompositeSurrogate)].Add(1, "Name").UseConstructor = false;

            return m;
        }

        [Fact]
        public void Composite_EntityData_Is_Same_Instance_As_Earlier_Entity()
        {
            _currentModel = BuildModel();
            s_cache.Reset();

            var arc = new Arc { Radius = 7 };
            var composite = new Composite { Name = "c" };
            composite.EntityData = arc;

            var list = new List<Entity> { arc, composite };

            using var ms = new MemoryStream();
            _currentModel.Serialize(ms, list);

            ms.Position = 0;
            s_cache.Reset();
            var round = (List<Entity>)_currentModel.Deserialize(ms, null, typeof(List<Entity>));

            Assert.NotNull(round);
            Assert.Equal(2, round.Count);
            Assert.IsType<Arc>(round[0]);
            Assert.IsType<Composite>(round[1]);

            // The assertion that matters:
            Assert.True(ReferenceEquals(round[1].EntityData, round[0]),
                "composite.EntityData must be the same instance as list[0] (reference preservation via ReferenceId).");
        }
    }
}
