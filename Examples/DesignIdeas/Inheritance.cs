using System.Collections.Generic;
using ProtoBuf;
using ProtoBuf.NetExtensions;

namespace Examples.DesignIdeas
{
    /// <summary>
    /// would like to be able to support something similar to [XmlInclude]/[KnownType];
    /// not supported by .proto spec, though, so "NetExtensions".
    /// 
    /// List/entity serializers would need to be aware; perhaps with a set of sub-serializers?
    /// No need to support on primative serializers.
    /// 
    /// During single entity deserialization, merge message (to merge) would need to check existing type
    /// against perceived type:
    /// * if same type merge directly
    /// * if new message is for a base-class of the current value then merge directly
    /// * if new message is for a sub-class of the current value, then:
    ///     * serialize the current value into a buffer
    ///     * deserialize the current value into a new instance of the new type
    ///     * merge the stream into the new instance
    ///         (see new ChangeType method)
    /// </summary>
    [ProtoContract]
    class Message {
        [ProtoMember(1)]
        [ProtoInclude(2, typeof(Sub1))]
        [ProtoInclude(3, typeof(Sub2))]
        public List<SomeBase> Data { get; private set; }
    }
    /* 
     * repeated somebase data = 1
     * repeated sub1 data_sub1 = 2
     * repeated sub2 data_sub2 = 3
     */ 
    [ProtoContract] class SomeBase { }
    [ProtoContract] class Sub1 : SomeBase { }
    [ProtoContract] class Sub2 : SomeBase { }
}
