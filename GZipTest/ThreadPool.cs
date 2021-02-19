using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
  public class ThreadPool
  {
    private const int DspPollRate_ms = 10;
    private static ThreadPool instance;
    private static WorkerThread[] Pool;
    private static Queue<Action> q;  //  ConcurrentQueue for !testcase
    private static Mutex mutex;
    private static bool isRunning;
    private static AutoResetEvent[] m_WTwaithandle;
    private static AutoResetEvent DspWaithandle;
    public byte MaxThreads { get; } = 4;
    public AutoResetEvent Finished { get; set; } = new AutoResetEvent(false);
    public uint Taskstodo { get; set; }
    public static ThreadPool Instance { 
      get
      {
        if(instance == null)
        {
          instance = new ThreadPool();
        }
        return instance;
      }
    }

    private ThreadPool()
    {
      Init();
    }

    //public ThreadPool(byte maxThreads)
    //{
    //  MaxThreads = maxThreads;
    //  Init();
    //}

    public void AddTask(Action a)
    {
      // protected from race
      mutex.WaitOne();
      q.Enqueue(a);
      mutex.ReleaseMutex();
      DspWaithandle.Set();
    }

    public void StopAll()
    {
      Console.WriteLine("ThreadPool Killswitch Triggered");
      isRunning = false;
      DspWaithandle.Set();
      for (int i = 0; i < MaxThreads; ++i)
      {
        Pool[i].Kill();
        m_WTwaithandle[i].Set();
      }
    }

    public void Run()
    {
      isRunning = true;
      Thread t = new Thread(() => {
        int finishedTasks = 0;
        while (isRunning)
        {
          DspWaithandle.WaitOne(DspPollRate_ms);

          for (int i = 0; i < MaxThreads; i++)
          {
            if (!Pool[i].IsBusy)
            {
              finishedTasks += Pool[i].FinishedTask;

              if (q.Count == 0)
                continue;

              Console.WriteLine("Dispatcher sending work to " + Pool[i].Name);
              Pool[i].Work = q.Dequeue(); //Simplified data flow with dequeueing by caller itself
              m_WTwaithandle[i].Set();
            }
          }

          if (Taskstodo == finishedTasks)
          {
            Finished.Set();
          }
        }
      });
      t.Start();
    }

    private void Init()
    {
      Pool = new WorkerThread[MaxThreads];
      q = new Queue<Action>();
      mutex = new Mutex();
      m_WTwaithandle = new AutoResetEvent[MaxThreads];
      DspWaithandle = new AutoResetEvent(false);
      for (int i = 0; i < MaxThreads; ++i)
      {
        m_WTwaithandle[i] = new AutoResetEvent(false);
        Pool[i] = new WorkerThread(i, m_WTwaithandle[i]);
      }
    }
  }
}
