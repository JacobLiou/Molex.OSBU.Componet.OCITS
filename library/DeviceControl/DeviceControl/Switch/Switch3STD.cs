using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///<summary>
///文件名：Switch3STD
///作用：3STD光源盒类
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
    public class Switch3STD : OpticalSwitchBase
    {
        public Switch3STD(string com, string baudrate, string switchName, ref string errMsg)
        {
            base.Open(com, baudrate, SwitchName, ref errMsg);
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
                        errMsg += "光源盒 error:指令长度不够！\r";
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
        /// <returns>0-成功 1-出错 2-回复指令长度不够或设置失败</returns>
        public override int AckCmdCheck(string sendCmd, string ackCmd, ref string errMsg)
        {
            try
            {
                sendCmd = sendCmd.Trim();
                ackCmd = ackCmd.Trim();
                string[] sendCmdArr = sendCmd.Split(' ');
                if (ackCmd.Substring(0, 1) == "2")
                {//2A 30 30 30 39 30 31 30 31 30 31 30 30 4C 0D
                    if (sendCmd.Substring(0, 42) == ackCmd.Substring(0, 42))
                        return 0;
                    string[] ackCmdArr = ackCmd.Split(' ');
                    if (ackCmdArr.Length < 15)
                    {
                        errMsg += "回复指令长度不够！";
                        return 2;
                    }
                    if (sendCmdArr[5] != ackCmdArr[5] || sendCmdArr[6] != ackCmdArr[6])
                    {
                        errMsg += "A组状态设置失败，当前A组状态为" + ackCmdArr[5].Substring(1, 1) + ackCmdArr[6].Substring(1, 1) + "！";
                        return 2;
                    }
                    if (sendCmdArr[7] != ackCmdArr[7] || sendCmdArr[8] != ackCmdArr[8])
                    {
                        errMsg += "B组状态设置失败，当前B组状态为" + ackCmdArr[7].Substring(1, 1) + ackCmdArr[8].Substring(1, 1) + "！";
                        return 2;
                    }
                    if (sendCmdArr[9] != ackCmdArr[9] || sendCmdArr[10] != ackCmdArr[10])
                    {
                        errMsg += "C组状态设置失败，当前C组状态为" + ackCmdArr[9].Substring(1, 1) + ackCmdArr[10].Substring(1, 1) + "！";
                        return 2;
                    }
                    if (sendCmdArr[11] != ackCmdArr[11] || sendCmdArr[12] != ackCmdArr[12])
                    {
                        errMsg += "D组状态设置失败，当前D组状态为" + ackCmdArr[11].Substring(1, 1) + ackCmdArr[12].Substring(1, 1) + "！";
                        return 2;
                    }
                    return 2;
                }
                else if (ackCmd.Substring(0, 1) == "*")
                {
                    byte[] sendArr = new byte[14];
                    for (int j = 0; j < 14; j++)
                        sendArr[j] = Convert.ToByte(sendCmdArr[j], 16);
                    string sendCmdStr = System.Text.Encoding.UTF8.GetString(sendArr);
                    if (sendCmdStr == ackCmd.Substring(0, 14))
                        return 0;
                    if (ackCmd.Length < 14)
                    {
                        errMsg += "回复指令长度不够！";
                        return 2;
                    }
                    if (sendCmdStr.Substring(5, 2) != ackCmd.Substring(5, 2))
                    {
                        errMsg += "A组状态设置失败，当前A组状态为" + ackCmd.Substring(5, 2) + "！";
                        return 2;
                    }
                    if (sendCmdStr.Substring(7, 2) != ackCmd.Substring(7, 2))
                    {
                        errMsg += "B组状态设置失败，当前B组状态为" + ackCmd.Substring(7, 2) + "！";
                        return 2;
                    }
                    if (sendCmdStr.Substring(9, 2) != ackCmd.Substring(9, 2))
                    {
                        errMsg += "C组状态设置失败，当前C组状态为" + ackCmd.Substring(9, 2) + "！";
                        return 2;
                    }
                    if (sendCmdStr.Substring(11, 2) != ackCmd.Substring(11, 2))
                    {
                        errMsg += "D组状态设置失败，当前D组状态为" + ackCmd.Substring(11, 2) + "！";
                        return 2;
                    }
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
