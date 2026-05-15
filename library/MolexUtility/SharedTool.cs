using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MolexUtility
{
    ///<summary>
    ///文件名：SharedTool
    ///数据表：用指定用户访问文件接口
    ///作用：
    ///作者：高鹏娟
    ///编写日期：2018-05-11
    ///修改记录
    ///R1：
    ///		修改作者：作者中文名
    ///		修改日期：<模块创建日期，格式：YYYY-MM-DD>
    ///		修改内容：xxx
    ///R2：
    ///		修改作者：作者中文名
    ///		修改日期：<模块创建日期，格式：YYYY-MM-DD>
    ///		修改内容：xxx
    ///</summary>
    public class SharedTool
    {
        const int dwLogonProvider = 0;
        const int dwLogonType = 9;//域控中的需要用:Interactive = 2         
        private bool disposed;

        /// <summary>
        /// 用指定用户名访问文件
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="domain">域</param>
        public SharedTool(string username, string password, string domain)
        {
            // initialize tokens         
            IntPtr tokenHandle = new IntPtr(0);
            tokenHandle = IntPtr.Zero;

            try
            {
                // get handle to token         
                bool checkOK = Win32API.LogonUser(username, domain, password, dwLogonType, dwLogonProvider, ref tokenHandle);
                if (checkOK)
                {
                    if (!Win32API.ImpersonateLoggedOnUser(tokenHandle))
                    {
                        int nErrorCode = Marshal.GetLastWin32Error();
                        throw new Exception("ImpersonateLoggedOnUser error;Code=" + nErrorCode);
                    }
                }
                else
                {
                    int nErrorCode = Marshal.GetLastWin32Error();
                    throw new Exception("LogonUser error;Code=" + nErrorCode);
                }
            }
            finally
            {
                // close handle(s)         
                if (tokenHandle != IntPtr.Zero)
                    Win32API.CloseHandle(tokenHandle);
            }
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {    
            if (!disposed)    
            {
                Win32API.RevertToSelf();    
                disposed = true;    
            }    
        }    
    
        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {    
            Dispose(true);    
        }    
    }
}
