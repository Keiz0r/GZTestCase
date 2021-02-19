using System;
using System.Threading;

namespace GZipTest
{
  public class WorkerThread
  {
    private Thread t;
    private AutoResetEvent p_starthandle;
    private bool active;
    private bool isBusy;
    private byte finishedTask;
    public Action Work { get; set; }
    public string Name { get { return t.ManagedThreadId.ToString(); } }
    public bool IsBusy { get { return isBusy; } }
    public byte FinishedTask { get { return finishedTask; } } // part of ThreadPool counter

    public WorkerThread(AutoResetEvent wh)
    {
      p_starthandle = wh;
      active = true;
      isBusy = false;
      finishedTask = 0;
      t = new Thread(Run);
      t.Start();
    }

    public WorkerThread(int name, AutoResetEvent wh) : this(wh)
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
        finishedTask = 0;
        isBusy = true;
        if(Work != null)
        {
          Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " invokes work");
          Work.Invoke();
          Work = null;
          finishedTask = 1;
        }
        isBusy = false;
      }
    }
  }
}
