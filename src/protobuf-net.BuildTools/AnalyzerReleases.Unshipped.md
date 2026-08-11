### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
PBN0001  | Usage    | Error    | Invalid field number (`[ProtoMember]`, `[ProtoPartialMember]`, `[ProtoInclude]`)
PBN0002  | Usage    | Error    | Invalid member name (`[ProtoPartialMember]`)
PBN0003  | Usage    | Error    | Duplicate field number (`[ProtoMember]`, `[ProtoPartialMember]`, `[ProtoInclude]`)
PBN0004  | Usage    | Warning  | Reserved field name
PBN0005  | Usage    | Warning  | Reserved field number
PBN0006  | Usage    | Warning  | Duplicated field name
PBN0007  | Usage    | Info     | Overlapping reservation
PBN0008  | Usage    | Warning   | Member described multiple times
PBN0009  | Usage    | Error    | Type not marked as proto-contract
PBN0010  | Usage    | Warning   | Member described and ignored
PBN0011  | Usage    | Error    | Duplicate include
PBN0012  | Usage    | Error    | Include of non-derived type
PBN0013  | Usage    | Warning  | Include not declared
PBN0014  | Usage    | Warning  | Sub-type not marked as proto-contract
PBN0015  | Usage    | Error    | No suitable constructor
PBN0016  | Usage    | Info     | Missing compatibility-level
PBN0017  | Usage    | Info     | Redundant `[ProtoEnum]` value
PBN0018  | Usage    | Error    | Enum value not supported
PBN0019  | Usage    | Info     | Redundant `[ProtoEnum]` name
PBN0020  | Usage    | Warning  | Member should declare `[DefaultValue]`
PBN0021  | Usage    | Warning  | Member should update its `[DefaultValue]`
PBN0022  | Usage    | Warning  | Member should declare `IsRequired`
PBN0023  | Usage    | Warning  | `[ProtoContract]` on an interface is not recommended
PBN2000  | ProtoBuf | Error    | Language version too low for the AOT generator
PBN2001  | ProtoBuf | Warning  | Contract omitted from the AOT model: unsupported member
PBN2002  | ProtoBuf | Warning  | Contract omitted from the AOT model: unsupported declaration
PBN2003  | ProtoBuf | Warning  | Contract omitted from the AOT model: unsupported protobuf-net option
PBN2004  | ProtoBuf | Warning  | Contract omitted from the AOT model: references an omitted contract
PBN2010  | ProtoBuf | Warning  | Call uses the runtime model, not the AOT model
PBN2011  | ProtoBuf | Warning  | Call resolves its contract type at run time
PBN2012  | ProtoBuf | Warning  | Project publishes AOT or trimmed, but has no AOT model
PBN2013  | ProtoBuf | Info     | Compile-time serializers are available
