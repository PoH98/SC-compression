using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC_Compression
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var file in Directory.GetFiles(Environment.CurrentDirectory, "*.csv"))
            {
                try
                {
                    Console.WriteLine(file);
                    var buffer = File.ReadAllBytes(file);
                    File.WriteAllBytes(file.Replace(".csv", "Decrypted.csv"), scCompression.Decompress(buffer));
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
        }
    }

    enum signatures
    {
        NONE = 0,
        LZMA = 1, // starts with 5D 00 00 04
        SC = 2, // starts with SC
        SCLZ = 3, // starts with SC and contains SCLZ
        SIG = 4, // starts with Sig:
    }
}
