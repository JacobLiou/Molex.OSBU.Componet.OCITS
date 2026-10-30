using System;
using System.Threading;

namespace MolexUtility.Device
{
    /// <summary>
    /// UDL COM 宜在 UI/STA 线程调用。宿主在 UI 线程 Capture，设备层通过 Send 封送。
    /// </summary>
    public static class UdlUiSync
    {
        public static SynchronizationContext UiSyncContext { get; private set; }

        public static void Capture()
        {
            SynchronizationContext ctx = SynchronizationContext.Current;
            if (ctx == null)
            {
                CommonFunction.WriteLog("UdlUiSync.Capture: SynchronizationContext.Current is null");
                return;
            }
            UiSyncContext = ctx;
            CommonFunction.WriteLog("UdlUiSync.Capture: " + ctx.GetType().FullName);
        }

        public static T Send<T>(Func<T> work)
        {
            if (work == null)
                throw new ArgumentNullException("work");

            SynchronizationContext ui = UiSyncContext;
            SynchronizationContext current = SynchronizationContext.Current;
            if (ui == null || ReferenceEquals(current, ui))
                return work();

            T result = default(T);
            Exception caught = null;
            ui.Send(_ =>
            {
                try
                {
                    result = work();
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
            }, null);

            if (caught != null)
                throw caught;
            return result;
        }
    }
}
