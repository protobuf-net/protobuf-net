using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.IO;

namespace ProtoBufGenerator
{
    public static class ExtensionMethods
    {
        public static ProjectItem Find(this ProjectItems projectItems, string fileName)
        {
            return (from pi in projectItems.Cast<ProjectItem>()
                    where string.Compare(pi.Name, fileName, true) == 0
                    select pi).FirstOrDefault();                   
        }

        #region System.IO.Stream
        public static byte[] ReadToEnd(this Stream stream, bool closeStream, int bufferSize)
        {
            MemoryStream writeStream =
                stream.CanSeek ?
                new MemoryStream((int)stream.Length) :
                new MemoryStream();

            byte[] buffer = new byte[bufferSize];

            int bytesRead = stream.Read(buffer, 0, bufferSize);

            while (bytesRead > 0)
            {
                writeStream.Write(buffer, 0, bytesRead);
                bytesRead = stream.Read(buffer, 0, bufferSize);
            }

            if (closeStream)
            {
                stream.Close();
            }

            return writeStream.ToArray();
        }

        public static byte[] ReadToEnd(this Stream stream)
        {
            return ReadToEnd(stream, true, 4096);
        }

        public static byte[] ReadToEnd(this Stream stream, bool closeStream)
        {
            return ReadToEnd(stream, closeStream, 4096);
        }

        public static void Write(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }
        #endregion
    }
}
