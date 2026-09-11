using System.Runtime.CompilerServices;

// The gRPC stream shims in Internal/ are wanted by both halves: the server bridges protoc's
// reader/writer-shaped handlers, the client bridges protoc's generated client through a CallInvoker,
// and the reader shim is literally the same type. Sharing it beats a second copy of a subtle bridge,
// and it is not public API - nothing outside these two assemblies should be writing one.
[assembly: InternalsVisibleTo("protobuf-net.Connect.AspNetCore, PublicKey=002400000480000094000000060200000024000052534131000400000100010009ed9caa457bfc205716c3d4e8b255a63ddf71c9e53b1b5f574ab6ffdba11e80ab4b50be9c46d43b75206280070ddba67bd4c830f93f0317504a76ba6a48243c36d2590695991164592767a7bbc4453b34694e31e20815a096e4483605139a32a76ec2fef196507487329c12047bf6a68bca8ee9354155f4d01daf6eec5ff6bc")]
