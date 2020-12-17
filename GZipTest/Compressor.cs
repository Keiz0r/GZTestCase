using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace GZipTest
{
  class Compressor
  {
    public Compressor(string InputFileName, string OutputFileName, int BufferSize)
    {
      this.BufferSize = BufferSize;
      InputFS = new FileStream(InputFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
      RemainingBufferSize = InputFS.Length;
      OutputFS = new FileStream(OutputFileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write, BufferSize, useAsync:true);
      partitions = (int)(InputFS.Length / BufferSize) + 1;
      TaskList = new List<Task>(partitions);
    }
    public int Compress()
    {
      for(int i = 0; i < partitions; i++)
      {
        Task tk = CompressPartitionAsync(i);
        TaskList.Add(tk);
      }
      Task.WaitAll(TaskList.ToArray());
      //CRC32 and Initial filesize are not accounted for :(

      //  long a = (bb[bb.Length - 4] << 8 * 3) + (bb[bb.Length - 3] << 8 * 2) + (bb[bb.Length - 2] << 8 * 1) + (bb[bb.Length - 1] << 8 * 0);
      // long b = (bb2[bb2.Length - 4] << 8 * 3) + (bb2[bb2.Length - 3] << 8 * 2) + (bb2[bb2.Length - 2] << 8 * 1) + (bb2[bb2.Length - 1] << 8 * 0);
      //  byte[] ab = BitConverter.GetBytes(InputFS.Length);
      //  OutputFS.Write(bb, 0, bb.Length -4);
      //  OutputFS.Write(bb2, 10, bb2.Length -10);
      return 0;
    }
    public int Decompress()
    {
      for (int i = 0; i < partitions; i++)
      {
        Task tk = DecompressPartitionAsync(i);
        TaskList.Add(tk);
      }
      Task.WaitAll(TaskList.ToArray());
      return 0;
    } //TODO: try to fakemake a gz file and decompress it. might find what to substitute for it to work
    private async Task CompressPartitionAsync(int TaskId)
    { //  awaits on IO operations keep IO busy
      using (MemoryStream ms = new MemoryStream())
      {
        using (GZipStream compressionStream = new GZipStream(ms, CompressionLevel.Optimal, true))
        {
          int tempSize = BufferSize < RemainingBufferSize ? BufferSize : (int)RemainingBufferSize;
          RemainingBufferSize -= tempSize;
          byte[] tempInputBytes = new byte[tempSize];
          await InputFS.ReadAsync(tempInputBytes, 0, tempSize);
          compressionStream.Write(tempInputBytes, 0, tempInputBytes.Length);
        }
        if (TaskId > 0)
        {
          await TaskList[TaskId - 1];
        }
        ms.Position = 0;  //  CopyTo doesn't reset stream's position by itself
        await ms.CopyToAsync(OutputFS);
      }
    }
    private async Task DecompressPartitionAsync(int TaskId)
    { //  awaits on IO operations keep IO busy
      using (MemoryStream ms = new MemoryStream())
      {
        if (TaskId > 0)
        { //  insert magic number to partition header and account for it in the buffer
          byte[] magicNum = { 0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A };
          ms.Write(magicNum, 0, 10);
          int tempSize = (BufferSize - 10 < RemainingBufferSize ? BufferSize - 10 : (int)RemainingBufferSize);
          RemainingBufferSize -= tempSize;
          byte[] tempInputBytes = new byte[tempSize];
          await InputFS.ReadAsync(tempInputBytes, 0, tempSize);
          await ms.WriteAsync(tempInputBytes, 0, tempSize);
        }
        else
        {
          int tempSize = BufferSize < RemainingBufferSize ? BufferSize : (int)RemainingBufferSize;
          RemainingBufferSize -= tempSize;
          byte[] tempInputBytes = new byte[tempSize];
          await InputFS.ReadAsync(tempInputBytes, 0, tempSize);
          await ms.WriteAsync(tempInputBytes, 0, tempSize);
        }

        using (GZipStream decompressionStream = new GZipStream(ms, CompressionMode.Decompress))
        {
          if (TaskId > 0)
          {
            await TaskList[TaskId - 1];
          }
          ms.Position = 0;  //  CopyTo doesn't reset stream's position by itself
          await decompressionStream.CopyToAsync(OutputFS);  //TODO: bug! throws after 1st use
          OutputFS.Flush();
        }
        
      }
    }

    private readonly int BufferSize;
    private long RemainingBufferSize;
    private FileStream InputFS;
    private FileStream OutputFS;
    private List<Task> TaskList;
    private readonly int partitions;
  }
}
