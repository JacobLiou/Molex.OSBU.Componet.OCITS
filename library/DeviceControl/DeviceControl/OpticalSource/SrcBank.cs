using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Device;
using MolexUtility;
using System.Threading;

namespace DeviceControl
{
    public class SrcBank:MolexUtility.Device.IOpticalSource
    {
        private DeviceControl.ClientSocket srcBankClient;
        private int srcClientID;
        private string recMsg;
        private AutoResetEvent manualEvent = new AutoResetEvent(false);
        private bool isSuccess = true;
        private string ackMsg = "";
        private string muxFlag = "";
        private string clientIP = "";
        private int clientPort = 0;
        
        
        public SrcBank(ref string errMsg, string host, int port,int clientID)
        {
            muxFlag = "CLIENT" + clientID.ToString();
            srcBankClient = new DeviceControl.ClientSocket(host, port);
            DeviceControl.ClientSocket.SeverDataDeal += SeverDataDealFun;
            srcBankClient.ConnectSever(ref errMsg);
            srcClientID = clientID;
            string clientIDStr = string.Format("{0:D2}", srcClientID);
            srcBankClient.SendData(clientIDStr,ref errMsg);
            clientIP = host;
            clientPort = port;
        }

        private void Reconnect()
        {
            string errMsg = "";
            srcBankClient = new DeviceControl.ClientSocket(clientIP, clientPort);
            DeviceControl.ClientSocket.SeverDataDeal += SeverDataDealFun;
            srcBankClient.ConnectSever(ref errMsg);
            string clientIDStr = string.Format("{0:D2}", srcClientID);
            srcBankClient.SendData(clientIDStr, ref errMsg);
            
        }

        public void SeverDataDealFun(string recData)
        {
            recMsg += recData;
            string logStr = string.Format("{0}:{1} {2}", srcClientID, "SeverDataDealFun", recMsg);
            CommonFunction.WriteLog(logStr);

            string[] msgSplits = recMsg.Split('*');
            for(int i=0;i<msgSplits.Length;i++)
            {
                if (msgSplits[i].Length == 0)
                    continue;
                int recID =Convert.ToInt32(msgSplits[i].Substring(0, 2));
                CommonFunction.WriteLog(recID.ToString());
                if (recID!=srcClientID)
                {
                    CommonFunction.WriteLog("ID不对！");
                    CommonFunction.WriteLog(srcClientID.ToString());
                    recMsg = "";
                    continue;
                }
                if (!msgSplits[i].Contains("#"))
                {
                    CommonFunction.WriteLog("未找到结束符");
                    continue;
                }
                using (Mutex m = new Mutex(true, muxFlag))
                {
                    ackMsg = msgSplits[i].Clone().ToString();
                }
                logStr = string.Format("{0}:{1} {2} 事件触发", srcClientID, "SeverDataDealFun", ackMsg);
                CommonFunction.WriteLog(logStr);
                Thread.Sleep(10);
            }
            if(!msgSplits[msgSplits.Length-1].Contains("#"))
            {
                recMsg = "*"+msgSplits[msgSplits.Length - 1];
            }
            else
            {
                recMsg = "";
            }
        }
        /// <summary>
        /// 获取设备类型
        /// </summary>
        /// <returns>返回设备类型</returns>
        public Devices GetDeviceType()
        {
            return Devices.OpiticalSourceBank;
        }

        /// <summary>
        /// 激光器扫描功能
        /// </summary>
        /// <param name="param">扫描相关参数</param>
        /// <param name="dataPath">扫描结果数据，放在文件中</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败 2-不支持</returns>
        public int DoScan(ScanParam param, out List<string> dataPath, ref string errMsg)
        {
            dataPath = new List<string>();
            return 2;
        }

        /// <summary>
        /// 切换激光器波长
        /// </summary>
        /// <param name="wavelength">波长点</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败 2-不支持</returns>
        public int SetWavelength(double wavelength, ref string errMsg)
        {
            try
            {
                DateTime nowTime = DateTime.Now.ToLocalTime();
                string sendTimeStr = string.Format("{0}{1}{2}", nowTime.Hour, nowTime.Minute, nowTime.Second);
                int sendTime = Convert.ToInt32(sendTimeStr);
                string sendMsg = string.Format("*{0:D2}{1:0000000.000},{2:D6}#", srcClientID, wavelength, sendTime);

                string logStr = string.Format("{0}:{1} {2}", srcClientID, "SetWavelength", sendMsg);
                CommonFunction.WriteLog(logStr);
                if (!srcBankClient.SendData(sendMsg, ref errMsg))
                {
                    Reconnect();
                    if (!srcBankClient.SendData(sendMsg, ref errMsg))
                    {
                        return 1;
                    }
                }
                //等待回复，如果3S没有回复，重发，重发3次，重连
                string wlAckMsg = "";
                int nCount = 0;
                while (true)
                {
                    using (Mutex m = new Mutex(true, muxFlag))
                    {                        
                        if (ackMsg.Length > 0)
                        {
                            logStr = string.Format("{0}:{1} {2}接收事件", srcClientID, "SetWavelength", ackMsg);
                            CommonFunction.WriteLog(logStr);

                            CommonFunction.WriteLog(ackMsg);
                            //CommonFunction.WriteLog("接收事件");
                            wlAckMsg = ackMsg.Clone().ToString();
                            ackMsg = "";
                            CommonFunction.WriteLog(wlAckMsg);
                            if (wlAckMsg.ToUpper().Contains(sendTimeStr))
                            {
                                if (wlAckMsg.ToUpper().Contains("ERROR"))
                                {
                                    errMsg = wlAckMsg.Replace(sendTimeStr, "");
                                    errMsg = errMsg.Replace(string.Format("*{0:D2}", srcClientID), "");
                                    return 1;
                                }
                                return 0;
                            }
                            else
                            {
                                return 1;
                            }
                        }
                    }
                    Thread.Sleep(10);
                    nCount++;
                    if (nCount == 900)
                    {
                        return 1;
                    }
                    if (nCount % 300 == 0)
                    {
                        Reconnect();
                        if (!srcBankClient.SendData(sendMsg, ref errMsg))
                        {
                            return 1;
                        }
                    }
                }
                          
            }
            catch(Exception ex)
            {
                errMsg = "切换波长出错：" + ex.Message;
                return 1;
            }
        }

        /// <summary>
        /// 设置光输出功率
        /// </summary>
        /// <param name="power">光输出功率</param>
        /// <param name="iUnit">光功率单位0：dbm 1：watt</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持</returns>
        public int SetPower(double power, int iUnit, ref string errMsg)
        {
            return 2;
        }

        /// <summary>
        /// 设置光输出口，高功率或者低功率
        /// </summary>
        /// <param name="opticalOutput">光输出口 0-高功率口 1-低功率口</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持</returns>
        public int SetOpticalOutput(long opticalOutput, ref string errMsg)
        {
            return 2;
        }
    }
}
