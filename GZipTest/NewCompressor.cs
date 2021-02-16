using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
  class NewCompressor
  {
    private const int BufferSize = 1024 * 1024 * 5;  //5MB
    private readonly ThreadPool tp;
    private readonly FileStream OutputFS;
    private readonly UInt32 partitions;
    private int nextPartitionToWrite = 0;
    private AutoResetEvent WriteWaitHandle = new AutoResetEvent(false);
    private const int WriteTimeoutRate_ms = 10;
    private readonly string InputFileName;
    public NewCompressor(string InputFileName, string OutputFileName)
    {
      this.InputFileName = InputFileName;
      OutputFS = new FileStream(OutputFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize);
      partitions = (UInt32)(new FileInfo(InputFileName).Length / BufferSize) + 1;

      tp = ThreadPool.Instance;
      tp.Taskstodo = partitions;
    }

    public void Compress()
    {
      tp.Run();
      for (int i = 0; i < partitions; i++)
      {
        //  enum tasks to ensure correct final write order
        int temp = i;
        // for each partition add task to threadpool
        tp.AddTask(() => {
          int taskNum = temp;
          using (MemoryStream ms = new MemoryStream())
          {
            using (GZipStream compressionStream = new GZipStream(ms, CompressionLevel.Optimal, true))
            {
              //  each task needs it's own InputFilestream in order to avoid usage of mutex
              FileStream InputFS = new FileStream(InputFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
              // temp buffer size is smaller for the last partition
              int tempSize = taskNum + 1 != partitions ? BufferSize : (int)(InputFS.Length - taskNum * BufferSize);
              byte[] tempInputBytes = new byte[tempSize];
              InputFS.Position = taskNum * BufferSize;
              InputFS.Read(tempInputBytes, 0, tempSize);
              compressionStream.Write(tempInputBytes, 0, tempInputBytes.Length);
            }
            // Check that writes are in correct order
            while(taskNum != nextPartitionToWrite)
            {
              WriteWaitHandle.WaitOne(WriteTimeoutRate_ms);
            }
            //  Write to file and signal to others
            ms.Position = 0;  //  CopyTo doesn't reset memorystream's position by itself
            ms.CopyTo(OutputFS);
            OutputFS.Flush();
            nextPartitionToWrite++;
            WriteWaitHandle.Set();
            return;
          }
        });
      }
      tp.Finished.WaitOne();
      tp.StopAll();
    }
    public void Decompress()
    {
      tp.Run();
      for (int i = 0; i < partitions; i++)
      {
        //  enum tasks to ensure correct final write order
        int temp = i;
        // for each partition add task to threadpool
        tp.AddTask(() => {
          int taskNum = temp;
          using (MemoryStream ms = new MemoryStream())
          {
            //  each task needs it's own InputFilestream in order to avoid usage of mutex
            FileStream InputFS = new FileStream(InputFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
            // temp buffer size is smaller for the last partition
            int tempSize = taskNum + 1 != partitions ? BufferSize : (int)(InputFS.Length - taskNum * BufferSize);
            byte[] tempInputBytes = new byte[tempSize];
            InputFS.Position = taskNum * BufferSize;
            InputFS.Read(tempInputBytes, 0, tempSize);
            ms.Write(tempInputBytes, 0, tempSize);

            using (GZipStream decompressionStream = new GZipStream(ms, CompressionMode.Decompress))
            {
              // Check that writes are in correct order
              while (taskNum != nextPartitionToWrite)
              {
                WriteWaitHandle.WaitOne(WriteTimeoutRate_ms);
              }
              //  Write to file and signal to others
              Console.WriteLine(taskNum + " is writing");
              ms.Position = 0;  // underlying ms stream position matters in this case, even though ms holds input data
              decompressionStream.CopyTo(OutputFS);
              OutputFS.Flush();
              Console.WriteLine(taskNum + " finished writing");
              nextPartitionToWrite++;
              WriteWaitHandle.Set();
              return;
            }
          }
        });
      }
      tp.Finished.WaitOne();
      tp.StopAll();
    }
  }
}
