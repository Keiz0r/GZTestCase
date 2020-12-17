using System;
using System.IO;

namespace GZipTest
{
  class Program
  {
    static int Main(string[] args)
    {
      if (args.Length != 3)
      {
        Console.WriteLine("Usage: [(de)compress] [file in] [file out]");
        return 1;
      }

      if (!File.Exists(args[1]))
      {
        Console.WriteLine("File " + args[1] + " does not exist");
        return 1;
      }
      if (File.Exists(args[2]))
      {
        Console.WriteLine("File " + args[2] + " already exists");
        return 1;
      }

      //Read about System.Span<T>

      if (String.Equals(args[0], "compress", StringComparison.OrdinalIgnoreCase))
      {
        Compressor comp = new Compressor(args[1], args[2], 1024 * 1024 * 10); //10MB
        comp.Compress();
      }
      else if (String.Equals(args[0], "decompress", StringComparison.OrdinalIgnoreCase))
      {
        Compressor comp = new Compressor(args[1], args[2], 1024 * 1024 * 10); //10MB
        comp.Decompress();
      }
      return 0;
    }
  }
}