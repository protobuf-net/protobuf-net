# Security policy

## Reporting a vulnerability

Please **do not open a public issue** for a suspected vulnerability.

Use GitHub's private vulnerability reporting instead:
<https://github.com/protobuf-net/protobuf-net/security/advisories/new>. That opens a private thread
visible only to the maintainers, and is the fastest route to a fix and an advisory.

If that is unavailable to you for any reason, email <marc.gravell@gmail.com> with `protobuf-net
security` in the subject.

Please include enough to reproduce: the package and version, the target framework, and a minimal
contract plus the payload bytes that show the problem. A failing test against `RuntimeTypeModel` is
ideal.

## What is in scope

protobuf-net deserializes bytes that a process often did not produce itself, so **deserializing
untrusted input is the surface that matters**. Worth reporting:

- a payload that causes unbounded allocation, non-terminating work, or a crash on deserialize -
  including via nesting depth, declared lengths, or repeated fields;
- a payload that causes a type to be constructed, or a member to be reached, that the contract does
  not describe;
- anything where the compile-time serializers (the AOT generator in `protobuf-net.BuildTools`)
  disagree with `RuntimeTypeModel` in a way that changes what a payload can do, rather than merely
  what it decodes to.

Note that a contract which is simply *permissive* - one that describes a type you would rather not
let a caller construct - is a property of that contract, not of the library. protobuf-net will
faithfully do what the contract says.

The gRPC layer lives in a separate repository -
<https://github.com/protobuf-net/protobuf-net.Grpc> - and service binding, endpoint metadata and
transport concerns belong there; if you are unsure which, report it here and it will be moved.

## Supported versions

Fixes land on `main` and ship in the next release of the affected package. There is no long-term
servicing branch, so "supported" means the current release line.
