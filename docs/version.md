What version of ProtoBuf does protobuf-net use?
=

"Version" can mean many things:

- the binary protocol
- the schema processing tools (aka `protoc`)
- the `.proto` syntax
- the library/runtime
- other features

### The Binary Protocol

The *binary ProtoBuf protocol* has not changed at all in the time in which it has been available open-source; protobuf-net provides
an implementation of the binary protocol, and as such it is not currently directly meaningful to ask what version of the binary protocol
is supported: there is only one version, and it is supported.

### The Schema Processing Tools

Back a few years ago, protobuf-net's schema-processing tools (which are entirely optional, to support users with `.proto` files) made
use of `protoc`. However, [the tools have now been re-written using entirely managed code](https://protogen.marcgravell.com/),
and as such have no dependency on `protoc` *at all*, and therefore have no versioning relevance to `protoc`.

### The .proto Syntax

The `.proto` schema language currently has 2 major versions; `"proto2"` and `"proto3"`; [protogen](https://protogen.marcgravell.com/) fully
supports both syntax versions and has been tested against a wide corpus of available schemas. The generated code includes all conventions
known up to 3.5.1 (the current version at time of writing), and some not-yet-released post-3.5.1 changes such as the re-addition of unknown
field support for `"proto3"` messages.

### The Library/Runtime

protobuf-net does not make use of any part of the Google codebase (except for example `.proto` files, for testing purposes),
and so is not tied in any way to ProtoBuf library versions.

### Other Features

There *are*, however, some ProtoBuf features that protobuf-net does not support; for example:

- the `Any` type (which makes use of external `.proto` schema resolution) is not implemented
- the JSON format is not implemented