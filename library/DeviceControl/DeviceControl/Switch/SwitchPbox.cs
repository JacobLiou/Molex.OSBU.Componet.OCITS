using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///<summary>
///文件名：SwitchPbox
///作用：P-Box光源盒类
///作者：阮锦芳
///编写日期：2018-01-24
///修改记录
///R1：
///		修改作者：高鹏娟
///		修改日期：2018-04-19
///		修改内容：功能实现
///</summary>

namespace DeviceControl
{
    public class SwitchPbox : OpticalSwitchBase
    {
        public SwitchPbox(string com, string baudrate, string switchName, ref string errMsg)
        {
            base.Open(com, baudrate, switchName, ref errMsg);
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
                for (int i = 0; i < cmdList.Count; i++)
                {
                    string[] cmdArr = cmdList[i].Split(' ');
                    if (cmdArr.Length < 13)
                    {
                        errMsg += "发送指令长度不够,请检查指令配置文件！";
                        return 2;
                    }
                    byte[] cmdBuffer = new byte[15];
                    for (int j = 0; j < 13; j++)
                        cmdBuffer[j] = Convert.ToByte(cmdArr[j], 16);
                    int checkSum = 0;
                    for (int k = 1; k < 13; k++)
                        checkSum += cmdBuffer[k];
                    byte check = (byte)checkSum;
                    if (check < 0x21)
                        check = 0x21;
                    if (check > 0x7E)
                        check = 0x7E;
                    cmdBuffer[13] = check;
                    cmdBuffer[14] = 0x0D;
                    string cmd = "";
                    int l = 0;
                    for (; l < cmdBuffer.Length - 1; l++)
                        cmd += (Convert.ToString(cmdBuffer[l], 16)).PadLeft(2, '0') + " ";
                    cmd += (Convert.ToString(cmdBuffer[l], 16)).PadLeft(2, '0');
                    cmdList[i] = cmd;
                }
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += "光源盒 error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 回复指令确认
        /// </summary>
        /// <param name="sendCmd">发送的指令</param>
        /// <param name="ackCmd">回复的指令</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错 2-回复指令长度不够或回复指令不等于*000300000000C</returns>
        public override int AckCmdCheck(string sendCmd, string ackCmd, ref string errMsg)
        {
            try
            {
                if (ackCmd.Substring(0, 1) == "2")
                {//2A 30 30 30 33 30 30 30 30 30 30 30 30 43 0D成功
                    string[] ackArr = ackCmd.Split(' ');
                    if (ackArr.Length < 15)
                    {
                        errMsg += "回复指令长度不够！";
                        return 2;
                    }
                    if (ackArr[0].ToUpper() == "2A" && ackArr[1] == "30" && ackArr[2] == "30" && ackArr[3] == "30" && ackArr[4] == "33"
                        && ackArr[5].ToUpper() == "30" && ackArr[6] == "30" && ackArr[7] == "30" && ackArr[8] == "30" && ackArr[9] == "30"
                        && ackArr[10] == "30" && ackArr[11] == "30" && ackArr[12] == "30" && ackArr[13] == "43" && ackArr[14].ToUpper() == "0D")
                        return 0;
                    else
                        return 2;
                }
                else if (ackCmd.Substring(0, 1) == "*")
                {//*000300000000C成功
                    if (ackCmd.Length < 14)
                    {
                        errMsg += "回复指令长度不够！";
                        return 2;
                    }
                    if (ackCmd.Substring(0, 14).ToUpper() == "*000300000000C")
                        return 0;
                    else
                        return 2;
                }
                else
                {
                    errMsg += "回复指令错误！";
                    return 2;
                }
            }
            catch (Exception ex)
            {
                errMsg += "光源盒 error:" + ex.Message + "\r";
                return 1;
            }
        }
    }
}
