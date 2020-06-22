using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SC_Compression
{
    class scCompression
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);
        public static byte[] Decompress(byte[] data)
        {
            var signature = ReadSignature(data);
            if (signature == signatures.NONE)
            {
                return data;
            }
            else if (signature == signatures.LZMA)
            {
                var uncompressedSize = INT2LE(data[5]);
                var padded = data.Take(9).ToList();
                padded.Add((byte)(uncompressedSize == -1 ? 0xFF : 0));
                padded.Add((byte)(uncompressedSize == -1 ? 0xFF : 0));
                padded.Add((byte)(uncompressedSize == -1 ? 0xFF : 0));
                padded.Add((byte)(uncompressedSize == -1 ? 0xFF : 0));
                padded.AddRange(data.Skip(9));
                return decompress(padded.ToArray());
            }
            else if (signature == signatures.SIG)
            {
                data = data.Skip(68).ToArray();
                var uncompressedSize = INT2LE(data[5]);
                var padded = data.Take(9).ToList();
                padded.Add((byte)(uncompressedSize == -1 ? 0xFF : 0));
                padded.Add((byte)(uncompressedSize == -1 ? 0xFF : 0));
                padded.Add((byte)(uncompressedSize == -1 ? 0xFF : 0));
                padded.Add((byte)(uncompressedSize == -1 ? 0xFF : 0));
                padded.AddRange(data.Skip(9));
                return decompress(padded.ToArray());
            }
            else
            {
                Console.WriteLine($"unknown signature { signature }");
                return data;
            }
        }

        private static signatures ReadSignature(byte[] data)
        {
            if (memcmp(data.Take(3).ToArray(), HexToByteArray("5d0000"), HexToByteArray("5d0000").Length) == 0)
            {
                return signatures.LZMA;
            }
            else if (Encoding.UTF8.GetString(data.Take(2).ToArray()).ToLower() == "sc")
            {
                if (data.Length >= 30 && Encoding.UTF8.GetString(data.Skip(26).Take(3).ToArray()).ToLower() == "sclz")
                {
                    return signatures.SCLZ;
                }
                return signatures.SC;
            }
            else if (Encoding.UTF8.GetString(data.Take(4).ToArray()).ToLower() == "sig:")
            {
                return signatures.SIG;
            }
            return signatures.NONE;
        }
        private static byte[] HexToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        private static int INT2LE(byte data)
        {
            byte[] b = new byte[4];
            b[0] = data;
            b[1] = (byte)(((uint)data >> 8) & 0xFF);
            b[2] = (byte)(((uint)data >> 16) & 0xFF);
            b[3] = (byte)(((uint)data >> 24) & 0xFF);
            return BitConverter.ToInt32(b, 0);
        }

        private static byte[] decompress(byte[] compressed)
        {
            byte[] retVal = null;

            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

            using (System.IO.Stream strmInStream = new System.IO.MemoryStream(compressed))
            {
                strmInStream.Seek(0, 0);

                using (System.IO.MemoryStream strmOutStream = new System.IO.MemoryStream())
                {
                    byte[] properties2 = new byte[5];
                    if (strmInStream.Read(properties2, 0, 5) != 5)
                        throw (new System.Exception("input .lzma is too short"));

                    long outSize = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int v = strmInStream.ReadByte();
                        if (v < 0)
                            throw (new System.Exception("Can't Read 1"));
                        outSize |= ((long)(byte)v) << (8 * i);
                    } //Next i

                    decoder.SetDecoderProperties(properties2);

                    long compressedSize = strmInStream.Length - strmInStream.Position;
                    decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

                    retVal = strmOutStream.ToArray();
                } // End Using newOutStream

            } // End Using newInStream

            return retVal;
        }
    }
}
