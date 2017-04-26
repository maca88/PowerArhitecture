﻿namespace System.IO
{
    public static class StreamExtensions
    {
        /// <summary>
        /// http://stackoverflow.com/questions/221925/creating-a-byte-array-from-a-stream
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this Stream input)
        {
            var memStream = input as MemoryStream;
            if (memStream != null) return memStream.ToArray();

            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
