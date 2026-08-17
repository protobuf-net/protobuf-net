; ServiceContractAnalyzer's PBN2001-PBN2010 (the gRPC analyzers) shipped in #735 and were
; recorded here by NOBODY until 2026-08-16 - which is half of why the AOT generator was later
; given a "PBN2000+ block of its own" that was nothing of the kind. They are listed below as
; new rules because that is where a reader will look; they are not new, they are newly tracked.
; Release tracking is not enforced in this repo (the RS2000 rules are inactive), so this table
; is documentation - and the ONLY register of which ids are taken. Check it before adding one.

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
PBN2001  | Usage    | Error    | gRPC: invalid member kind on a service contract
PBN2002  | Usage    | Error    | gRPC: invalid payload type
PBN2003  | Usage    | Error    | gRPC: invalid return type
PBN2004  | Usage    | Error    | gRPC: generic method
PBN2005  | Usage    | Error    | gRPC: generic service
PBN2006  | Usage    | Error    | gRPC: invalid context type
PBN2007  | Usage    | Error    | gRPC: invalid parameters
PBN2008  | Usage    | Warning  | gRPC: payload possibly not serializable
PBN2009  | Usage    | Info     | gRPC: prefer an async signature
PBN2010  | Usage    | Error    | gRPC: streaming method declared synchronously
PBN3000  | ProtoBuf | Error    | Language version too low for the AOT generator
PBN3001  | ProtoBuf | Warning  | Contract omitted from the AOT model: unsupported member
PBN3002  | ProtoBuf | Warning  | Contract omitted from the AOT model: unsupported declaration
PBN3003  | ProtoBuf | Warning  | Contract omitted from the AOT model: unsupported protobuf-net option
PBN3004  | ProtoBuf | Warning  | Contract omitted from the AOT model: references an omitted contract
PBN3010  | ProtoBuf | Warning  | Call uses the runtime model, not the AOT model
PBN3011  | ProtoBuf | Warning  | Call resolves its contract type at run time
PBN3012  | ProtoBuf | Warning  | Project publishes AOT or trimmed, but has no AOT model
PBN3013  | ProtoBuf | Info     | Compile-time serializers are available
PBN4000  | ProtoBuf.Grpc | Warning | Language version too low for build-time gRPC proxies
PBN4001  | ProtoBuf.Grpc | Warning | Service interface cannot be nested
PBN4002  | ProtoBuf.Grpc | Warning | Service method shape is not supported by the generator
PBN4003  | ProtoBuf.Grpc | Warning | Open generic service interfaces are not supported
PBN4004  | ProtoBuf.Grpc | Warning | Service interface inherits an interface that is not a sub-service
PBN4005  | ProtoBuf.Grpc | Warning | A [ProtoGrpc] type must be partial
PBN4006  | ProtoBuf.Grpc | Warning | A [ProtoGrpc] type must derive from ClientFactory
PBN4007  | ProtoBuf.Grpc | Warning | Named type is not a service contract
PBN4008  | ProtoBuf.Grpc | Warning | Named implementation does not implement the contract
PBN4009  | ProtoBuf.Grpc | Warning | Service contract declares no recognised operations
PBN4010  | ProtoBuf.Grpc | Warning | No AOT serializer model named for these proxies
PBN4011  | ProtoBuf.Grpc | Warning | Service contract could not be resolved
PBN4012  | ProtoBuf.Grpc | Warning | The named serializer model is not marked [ProtoModel]
PBN4013  | ProtoBuf.Grpc | Warning | The named serializer model has no serializer for a payload type
PBN4014  | ProtoBuf.Grpc | Warning | A [ProtoGrpc] type must be top-level and non-generic
PBN4015  | ProtoBuf.Grpc | Warning | Project publishes AOT or trimmed, but has no build-time gRPC proxies
PBN4016  | ProtoBuf.Grpc | Warning | Call does not use the build-time gRPC proxies

