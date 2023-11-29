using System;

#if NET_STANDARD_2_0
using System.Threading.Tasks;
#else
using System.Threading;
#endif

namespace Best.HTTP.Shared.PlatformSupport.Threading
{
    public static class ThreadedRunner
    {
        public static void SetThreadName(string name)
        {
            try
            {
                System.Threading.Thread.CurrentThread.Name = name;
            }
            catch(Exception ex)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Exception("ThreadedRunner", "SetThreadName", ex);
            }
        }

        public static void RunShortLiving<T>(Action<T> job, T param)
        {
#if NET_STANDARD_2_0
            var _task = new Task(() => job(param));
            _task.ConfigureAwait(false);
            _task.Start();
#else
            ThreadPool.QueueUserWorkItem(new WaitCallback(_ => job(param)));
#endif
        }

        public static void RunShortLiving<T1, T2>(Action<T1, T2> job, T1 param1, T2 param2)
        {
#if NET_STANDARD_2_0
            var _task = new Task(() => job(param1, param2));
            _task.ConfigureAwait(false);
            _task.Start();
#else
            ThreadPool.QueueUserWorkItem(new WaitCallback(_ => job(param1, param2)));
#endif
        }

        public static void RunShortLiving<T1, T2, T3>(Action<T1, T2, T3> job, T1 param1, T2 param2, T3 param3)
        {            
#if NET_STANDARD_2_0
            var _task = new Task(() => job(param1, param2, param3));
            _task.ConfigureAwait(false);
            _task.Start();
#else
            ThreadPool.QueueUserWorkItem(new WaitCallback(_ => job(param1, param2, param3)));
#endif
        }

        public static void RunShortLiving<T1, T2, T3, T4>(Action<T1, T2, T3, T4> job, T1 param1, T2 param2, T3 param3, T4 param4)
        {
#if NET_STANDARD_2_0
            var _task = new Task(() => job(param1, param2, param3, param4));
            _task.ConfigureAwait(false);
            _task.Start();
#else
            ThreadPool.QueueUserWorkItem(new WaitCallback(_ => job(param1, param2, param3, param4)));
#endif
        }

        public static void RunShortLiving(Action job)
        {
#if NET_STANDARD_2_0
            var _task = new Task(() => job());
            _task.ConfigureAwait(false);
            _task.Start();
#else
            ThreadPool.QueueUserWorkItem(new WaitCallback((param) => job()));
#endif
        }

        public static void RunLongLiving(Action job)
        {
#if NET_STANDARD_2_0
            var _task = new Task(() => job(), TaskCreationOptions.LongRunning);
            _task.ConfigureAwait(false);
            _task.Start();
#else
            var thread = new Thread(new ParameterizedThreadStart((param) => job()));
            thread.IsBackground = true;
            thread.Start();
#endif
        }
    }
}
