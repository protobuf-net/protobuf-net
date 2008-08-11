
using System;
using System.IO;
namespace ProtoBuf
{
    internal static class Base128Variant
    {
        internal const long Int64Msb = ((long)1) << 63;
        internal const int Int32Msb = ((int)1) << 31;

        
        
        

        


       

        private const long AllButFirstChunk = ~((long)127);
        private const long AllButFirstTwoChunks = AllButFirstChunk << 7;



        

        
    }
}
