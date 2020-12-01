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
PBN0008  | Usage    | Error    | Member described multiple times
PBN0009  | Usage    | Error    | Type not marked as proto-contract
PBN0010  | Usage    | Error    | Member described and ignored
PBN0011  | Usage    | Error    | Duplicate include
PBN0012  | Usage    | Error    | Include of non-derived type
PBN0013  | Usage    | Warning  | Include not declared
PBN0014  | Usage    | Warning  | Sub-type not marked as proto-contract
PBN0015  | Usage    | Error    | No suitable constructor
PBN0016  | Usage    | Info     | Missing compatibility-level
