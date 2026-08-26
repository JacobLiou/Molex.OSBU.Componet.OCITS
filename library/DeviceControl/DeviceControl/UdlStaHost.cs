using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DeviceControl
{
    /// <summary>
    /// 进程内唯一 UDL COM STA 宿主线程；所有 deviceEngine/fstpCtrl/tccCtrl 访问须在此线程。
    /// </summary>
    internal static class UdlStaHost
    {
        private static readonly object StartLock = new object();
        private static Thread hostThread;
        private static BlockingCollection<Action> queue;
        private static volatile bool started = false;

        public static bool IsOnHostThread()
        {
            return started && Thread.CurrentThread == hostThread;
        }

        public static void EnsureStarted()
        {
            if (started)
                return;
            lock (StartLock)
            {
                if (started)
                    return;
                queue = new BlockingCollection<Action>();
                hostThread = new Thread(HostLoop)
                {
                    IsBackground = true,
                    Name = "UdlStaHost"
                };
                hostThread.SetApartmentState(ApartmentState.STA);
                hostThread.Start();
                started = true;
            }
        }

        private static void HostLoop()
        {
            foreach (Action work in queue.GetConsumingEnumerable())
            {
                try
                {
                    work();
                }
                catch (Exception ex)
                {
                    MolexUtility.CommonFunction.WriteLog("UdlStaHost work exception: " + ex);
                    throw;
                }
            }
        }

        public static void Invoke(Action work)
        {
            if (work == null)
                return;
            if (IsOnHostThread())
            {
                work();
                return;
            }
            EnsureStarted();
            Exception caught = null;
            var done = new ManualResetEvent(false);
            queue.Add(() =>
            {
                try
                {
                    work();
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
                finally
                {
                    done.Set();
                }
            });
            done.WaitOne();
            if (caught != null)
                throw caught;
        }

        public static T Invoke<T>(Func<T> work)
        {
            if (work == null)
                return default(T);
            if (IsOnHostThread())
                return work();

            T result = default(T);
            Exception caught = null;
            Invoke(() =>
            {
                try
                {
                    result = work();
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
            });
            if (caught != null)
                throw caught;
            return result;
        }
    }
}
