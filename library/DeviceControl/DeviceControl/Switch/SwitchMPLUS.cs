using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DeviceControl
{
    /// <summary>
    /// 1×16 MPLUS 光开关（RS232 ASCII，MSW 协议，响应以 OK/Err 及提示符 > 结束）
    /// </summary>
    public class SwitchMPLUS : OpticalSwitchBase
    {
        private readonly AutoResetEvent mplusSwitchEvent;
        private readonly StringBuilder reserveRead = new StringBuilder();

        public SwitchMPLUS(string com, string baudrate, string switchName, ref string errMsg)
        {
            mplusSwitchEvent = new AutoResetEvent(false);
            base.Open(com, baudrate, switchName, ref errMsg);
        }

        public override void BaseSwitch_ThreadReadEvent(string readStr, string errMsg)
        {
            reserveRead.Append(readStr);
            bool isNotice = false;

            string buffered = reserveRead.ToString();
            if (buffered.IndexOf('>') >= 0 ||
                buffered.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                buffered.IndexOf("Err:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AddResponds(buffered);
                reserveRead.Clear();
                isNotice = true;
            }

            if (isNotice)
                mplusSwitchEvent.Set();
        }

        public override int CheckSum(ref List<string> cmdList, ref string errMsg)
        {
            return 0;
        }

        public override int SetSwitch(string flag, ref string errMsg)
        {
            try
            {
                List<string> cmdList = new List<string>();
                if (GetCmdByFlag(flag, ref cmdList, ref errMsg) != 0)
                {
                    if (string.IsNullOrEmpty(errMsg))
                        errMsg = "光源盒 error：未找到指令配置文件：" + flag;
                    return 1;
                }
                for (int i = 0; i < cmdList.Count; i++)
                {
                    mplusSwitchEvent.Reset();
                    reserveRead.Clear();
                    ClearResponds();

                    string send = cmdList[i] + "\r\n";
                    int err = BaseSession.WriteSerialString(send, ref errMsg);
                    if (err != 0)
                    {
                        errMsg = "光源盒 error:切换光开关写指令失败！\r";
                        return 1;
                    }
                    if (!mplusSwitchEvent.WaitOne(TimeSpan.FromSeconds(3)))
                    {
                        errMsg = "光源盒 error:接收回复指令超时（3S）！\r";
                        return 1;
                    }
                    string ackCmd = "";
                    while (true)
                    {
                        string buf = GetResponds();
                        if (buf == "")
                            break;
                        ackCmd += buf;
                    }

                    ClearResponds();
                    err = AckCmdCheck(cmdList[i], ackCmd, ref errMsg);
                    if (err != 0)
                        return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                errMsg = "光源盒 error:" + ex.Message + "\r";
                return 1;
            }
        }

        public override int AckCmdCheck(string sendCmd, string ackCmd, ref string errMsg)
        {
            try
            {
                if (string.IsNullOrEmpty(ackCmd))
                {
                    errMsg = "光源盒回复出错 error:无响应\r";
                    return 1;
                }
                if (ackCmd.IndexOf("Err:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    errMsg = "光源盒回复出错 error:" + ackCmd + "\r";
                    return 1;
                }
                if (ackCmd.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0)
                    return 0;
                if (ackCmd.IndexOf('>') >= 0)
                    return 0;

                errMsg = "光源盒回复出错 error:" + ackCmd + "\r";
                return 1;
            }
            catch (Exception ex)
            {
                errMsg = "光源盒回复出错 error:" + ex.Message + "\r";
                return 1;
            }
        }
    }
}
