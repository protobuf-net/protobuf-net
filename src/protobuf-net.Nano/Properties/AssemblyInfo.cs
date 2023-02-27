﻿using System.Runtime.CompilerServices;

[module: SkipLocalsInit]
[assembly:InternalsVisibleTo("Benchmark, PublicKey=002400000480000094000000060200000024000052534131000400000100010009ed9caa457bfc205716c3d4e8b255a63ddf71c9e53b1b5f574ab6ffdba11e80ab4b50be9c46d43b75206280070ddba67bd4c830f93f0317504a76ba6a48243c36d2590695991164592767a7bbc4453b34694e31e20815a096e4483605139a32a76ec2fef196507487329c12047bf6a68bca8ee9354155f4d01daf6eec5ff6bc")]

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Interface, Inherited = false)]
    internal sealed class SkipLocalsInitAttribute : Attribute { }
}
#endif
