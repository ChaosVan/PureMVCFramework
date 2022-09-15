using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace PureMVCFramework
{
    public interface IThreadTask
    {
        bool Processed { get; set; }
        void Process();
    }

    public class MultiThread
    {
        [DomainReload(0)]
        public static int CurrentThreadCount;

        private Thread thread;
        private AutoResetEvent wakeupEvent = new AutoResetEvent(false);
        private volatile bool stop;
        private Queue<IThreadTask> tasks = new Queue<IThreadTask>();

        public string Name { get; private set; }
        public bool IsStop
        {
            get
            {
                return stop;
            }
        }

        public MultiThread(string name)
        {
            Name = name;
            CurrentThreadCount++;
        }

        public void Start()
        {
            stop = false;
            thread = new Thread(ThreadProc)
            {
                Name = Name
            };
            thread.Start();
        }

        public void Stop()
        {
            stop = true;
            Wakeup();
            if (thread != null)
            {
                thread.Join();
                thread = null;
            }
            tasks.Clear();
        }

        public void AddTask(IThreadTask task)
        {
            lock (tasks)
            {
                tasks.Enqueue(task);
            }

            Wakeup();
        }

        private void Sleep()
        {
            wakeupEvent.WaitOne();
        }

        private void Wakeup()
        {
            wakeupEvent.Set();
        }

        private void ThreadProc()
        {
            while (!stop)
            {
                IThreadTask task = null;
                lock (tasks)
                {
                    if (tasks.Count > 0)
                    {
                        task = tasks.Dequeue();
                    }
                }

                if (task != null)
                {
                    try
                    {
                        task.Process();
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("[{0} Exception]{1}", Name, e);
                    }
                }
                else
                {
                    Sleep();
                }
            }
        }
    }
}
