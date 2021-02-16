using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
  public class WorkerThread
  {
    private static Queue<Action> p_q;
    private static Mutex p_mutex;
    private Thread t;
    private AutoResetEvent p_starthandle;
    private bool active = true;
    private bool isBusy = false;
    private static UInt32 finishedTasks;
    private static Mutex m_finishedtasksMutex;
    public string Name { get { return t.ManagedThreadId.ToString(); } }
    public bool IsBusy { get { return isBusy; } }
    public static UInt32 FinishedTasks { get { return finishedTasks; } }
    public WorkerThread(Queue<Action> que, Mutex m, AutoResetEvent wh)
    {
      p_q = que;
      p_mutex = m;
      p_starthandle = wh;
      m_finishedtasksMutex = new Mutex();
      t = new Thread(Run);
      t.Start();
    }
    public WorkerThread(int name, Queue<Action> que,  Mutex m, AutoResetEvent wh) : this(que, m, wh)
    {
      t.Name = "Worker thread #" + name.ToString();
    }

    public void Kill()
    {
      active = false;
    }

    private void Run()
    {
      // delegate func
      while (active)
      {
        // activate on signal from dispatcher
        p_starthandle.WaitOne();
        isBusy = true;
        Action a;
        p_mutex.WaitOne();
        p_q.TryDequeue(out a);
        p_mutex.ReleaseMutex();
        if(a != null)
        {
          Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " invokes work");
          a.Invoke();
          //show # of tasks finished
          m_finishedtasksMutex.WaitOne();
          finishedTasks++;
          m_finishedtasksMutex.ReleaseMutex();
        }
        isBusy = false;
      }
    }
  }
}
