using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MolexUtility.Device;
using MolexUtility.SerialControl;
using System.IO.Ports;

namespace DeviceControl
{
    public class PDLController:IPDLController
    {

        /// <summary>
        /// 串口操作对象
        /// </summary>
        private SerialPort serialSession = null;

        private bool isFinished = false;

        private string finishErrMsg = "";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="com">串口号，格式“ASRL1::INSTR”/“COM1”</param>
        public PDLController(ref string errMsg, string com)
        {
            Open(ref errMsg, com);
        }

        public void FinishedThread()
        {
            while(true)
            {
                if (serialSession.BytesToRead > 2)
                {
                    string ackMsg = serialSession.ReadExisting();
                    using (Mutex m = new Mutex(true, "DOPDL"))
                    {
                        if (ackMsg.ToUpper().Contains("OK"))
                        {
                            isFinished = true;
                            break;
                        }
                        else if (ackMsg.ToUpper().Contains(("Invalid data").ToUpper()) || ackMsg.ToUpper().Contains(("Invalid command").ToUpper()) || ackMsg.ToUpper().Contains(("Execute fail").ToUpper()))
                        {
                            isFinished = true;
                            finishErrMsg = ackMsg;
                            break;
                        }
                    }
                }
                 Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 开始摇偏振控制器
        /// </summary>
        /// <param name="nPDLIdx">摇第几个偏振控制器</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败 2-不支持</returns>
        public int DoPDL(int nPDLIdx, ref string errMsg)
        {
            try
            {
                using (Mutex m = new Mutex(true, "DOPDL"))
                {
                    isFinished=false;
                }
                string sendMsg = string.Format("TPDL {0}\r\n", nPDLIdx);
                serialSession.Write(sendMsg);
                Thread finishThread = new Thread(new ThreadStart(FinishedThread));
                finishThread.Start();
                return 0;
            }
            catch(Exception ex)
            {
                errMsg = string.Format("DoPDL 异常：{0}", ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// 查询偏振控制器摇是否结束
        /// </summary>
        /// /// <param name="errMsg">错误信息</param>
        /// <returns>true--结束  false--未结束</returns>
        public bool IsPDLFinish(ref string errMsg)
        {
            bool res = false;
            using (Mutex m = new Mutex(true, "DOPDL"))
            {
                res = isFinished;
                errMsg = finishErrMsg;
            }
            return res;
            
        }

        /// <summary>
        /// 打开功率计操作
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="com">串口号</param>
        /// <param name="baudrate">波特率</param>
        ///<param name="timeout">延时</param>
        ///<param name="isStartRead">是否实时读取功率值</param>
        /// <returns>0-成功 1-出错</returns>
        private int Open(ref string errMsg, string com)
        {
            try
            {
                int baudrateInt = 115200;                
                serialSession = new SerialPort();
                serialSession.PortName = com;
                serialSession.BaudRate = baudrateInt;
                serialSession.StopBits = StopBits.One;
                serialSession.DataBits = 8;
                serialSession.Parity = Parity.None;
                serialSession.ReadTimeout = 1000;
                if (serialSession.IsOpen)
                {
                    serialSession.Close();
                }
                serialSession.Open();
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }
    }
}
