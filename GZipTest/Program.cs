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
      
      
      if (String.Equals(args[0], "compress", StringComparison.OrdinalIgnoreCase))
      {
        NewCompressor newc = new NewCompressor(args[1], args[2]);
        newc.Compress();
      }
      else if (String.Equals(args[0], "decompress", StringComparison.OrdinalIgnoreCase))
      {
        NewCompressor newc = new NewCompressor(args[1], args[2]);
        newc.Decompress();
      }
      return 0;
    }
  }
}