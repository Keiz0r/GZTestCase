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
    private static Queue<Action> m_q = new Queue<Action>();  //  ConcurrentQueue for !testcase
    private static Mutex m_mutex = new Mutex();
    private static bool isRunning = true;
    private static AutoResetEvent[] m_WTwaithandle;
    private static AutoResetEvent DspWaithandle;
    public AutoResetEvent Finished { get; set; } = new AutoResetEvent(false);
    public UInt32 Taskstodo { get; set; } //  TODO: ugly
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
    public  byte MaxThreads { get; } = 4;
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
      m_mutex.WaitOne();
      m_q.Enqueue(a);
      m_mutex.ReleaseMutex();
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
      Thread t = new Thread(() => {
        while (isRunning)
        {
          DspWaithandle.WaitOne(DspPollRate_ms);
          //check if 
          if(Taskstodo == WorkerThread.FinishedTasks)
          {
            Finished.Set();
          }

          if(m_q.Count == 0)
          {
            continue;
          }
          // send  !Busy threads to work
          for (int i = 0; i < MaxThreads; ++i)
          {
            if (!Pool[i].IsBusy)
            {
              Console.WriteLine("Dispatcher sending work to " + Pool[i].Name);
              m_WTwaithandle[i].Set();
            }
          }
        }
      });
      t.Start();
    }

    private void Init()
    {
      DspWaithandle = new AutoResetEvent(false);
      Pool = new WorkerThread[MaxThreads];
      m_WTwaithandle = new AutoResetEvent[MaxThreads];
      for (int i = 0; i < MaxThreads; ++i)
      {
        m_WTwaithandle[i] = new AutoResetEvent(false);
        Pool[i] = new WorkerThread(i, m_q, m_mutex, m_WTwaithandle[i]);
      }
    }
  }
}
