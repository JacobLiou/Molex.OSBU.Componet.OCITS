using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MolexUtility.SerialControl;
using System.Runtime.CompilerServices;

namespace DeviceControl
{
    public class SwitchOMS:OpticalSwitchBase
    {
        /// <summary>
        /// 读写指令同步事件
        /// </summary>
        private AutoResetEvent omsSwitchEvent;

        /// <summary>
        /// 读取的串口缓存，不是完整指令部分
        /// </summary>
        private string reserveRead = "";

        public SwitchOMS(string com, string baudrate, string switchName, ref string errMsg)
        {
            omsSwitchEvent = new AutoResetEvent(false);
            base.Open(com, baudrate, switchName, ref errMsg);
        }

        /// <summary>
        /// 串口读取处理事件。只要功能是解析完整指令，存放到回复列表，通知其他线程有回复
        /// </summary>
        /// <param name="readStr">串口读取到的所以信息</param>
        /// <param name="errMsg">出错信息</param>
        public override void BaseSwitch_ThreadReadEvent(string readStr, string errMsg)
        {
            reserveRead += readStr;
            char[] chSplit = new char[1] { '\r' };
            bool isNotice = false;
            while(reserveRead.Contains("\r"))
            {
                isNotice = true;
                string cmdStr = reserveRead;
                AddResponds(cmdStr);
                reserveRead = "";
                /*string[] splits = reserveRead.Split(chSplit,2);
                AddResponds(splits[0]);
                isNotice = true;
                if (splits.Length==2)
                {
                    reserveRead = splits[1];
                }   */
            }

            //通知正在等待的线程，收到完整指令了
            if(isNotice)
            {
                omsSwitchEvent.Set();
            }
        }

        /// <summary>
        /// 校验和计算
        /// </summary>
        /// <param name="cmdList">需要计算校验和的指令</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错 2-指令长度不够</returns>
        public override int CheckSum(ref List<string> cmdList, ref string errMsg)
        {
            try
            {
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += "光源盒 error:" + ex.Message + "\r";
                return 1;
            }
        }


        /// <summary>
        /// 切换光源盒
        /// </summary>
        /// <param name="flag">切换的标志，格式暂定 产品序号:波长:通道:参数</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错</returns>
        public override int SetSwitch(string flag, ref string errMsg)
        {
            try
            {
                List<string> cmdList = new List<string>();
                if(GetCmdByFlag(flag, ref cmdList, ref errMsg)!=0)
                {
                    errMsg = "光源盒 error：未找到指令配置文件：" + flag;
                    return 1;
                }
                for (int i = 0; i < cmdList.Count; i++)
                {
                    omsSwitchEvent.Reset();
                    int err = -1;
                    string send = cmdList[i] + "\r\n";
                    err = BaseSession.WriteSerialString(send, ref errMsg);
                    if (err != 0)
                    {
                        errMsg = "光源盒 error:切换光开关写指令失败！\r";
                        return 1;
                    }
                    while(!omsSwitchEvent.WaitOne(TimeSpan.FromSeconds(2)))
                    {
                        errMsg = "光源盒 error:接收回复指令超时（2S）！\r";
                        return 1;
                    }
                    string ackCmd = "";
                    while(true)
                    {
                        string buf= GetResponds();
                        if (buf == "")
                            break;
                        ackCmd += buf;
                    }
                    
                    ClearResponds();
                    err = AckCmdCheck(cmdList[i], ackCmd, ref errMsg);
                    if (err != 0)
                    {
                        return 1;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                errMsg = "光源盒 error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 回复指令确认
        /// </summary>
        /// <param name="sendCmd">发送的指令</param>
        /// <param name="ackCmd">回复的指令</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错</returns>
        public override int AckCmdCheck(string sendCmd, string ackCmd, ref string errMsg)
        {
            try
            {
                if (ackCmd.Contains("OK"))
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
