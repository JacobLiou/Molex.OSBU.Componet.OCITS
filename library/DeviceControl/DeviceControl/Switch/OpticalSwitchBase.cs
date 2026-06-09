using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Device;
using MolexUtility.SerialControl;
using MolexUtility;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

///<summary>
///文件名：OpticalSwitchBase
///作用：光源盒抽象类，实现了光源盒操作的公共功能部分
///作者：阮锦芳
///编写日期：2018-01-22
///修改记录
///R1：
///		修改作者：高鹏娟
///		修改日期：2018-04-19
///		修改内容：实现基类功能
///</summary>

namespace DeviceControl
{
    public abstract class OpticalSwitchBase : IOpticalSwitch
    {
        /// <summary>
        /// 读写指令同步事件
        /// </summary>
        private AutoResetEvent baseSwitchEvent;

        /// <summary>
        /// 用于存放读取的指令，两条线程都需要访问，注意线程同步
        /// </summary>
        private List<string> switchResponds = new List<string>();

        /// <summary>
        /// 读取的串口缓存，不是完整指令部分
        /// </summary>
        private string reserveRead = "";

        /// <summary>
        /// 光源盒名称，和配置文件名称一致，程序用来决定使用哪个开关
        /// </summary>
        public string SwitchName { get; set; }
        public string SwitchPath = Environment.CurrentDirectory + "\\switch\\";

        /// <summary>
        /// 串口操作对象
        /// </summary>
        public ISerial BaseSession = null;

        struct CommandStruct
        {
            public string com;
            public List<string> cmdList;
            public int priority;
        }
        private List<CommandStruct> allCommand = new List<CommandStruct>();

        /// <summary>
        /// 打开光源盒通信串口
        /// </summary>
        /// <param name="com">串口</param>
        /// <param name="baudrate">波特率</param>
        /// <param name="switchName">光源盒对应指令文件名称</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错</returns>
        public int Open(string com, string baudrate, string switchName, ref string errMsg)
        {
            try
            {
                baseSwitchEvent = new AutoResetEvent(false);
                int baudrateInt = 0;
                Int32.TryParse(baudrate, out baudrateInt);
                SwitchName = OpticalSwitchConfigNames.SanitizeMplusSwitchShowName(switchName);
                SwitchPath = Path.Combine(Environment.CurrentDirectory, "switch", SwitchName);
                BaseSession = new SerialDotNet(com, baudrateInt, ref errMsg, 100, true);
                BaseSession.ThreadReadEvent += BaseSwitch_ThreadReadEvent;
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += "光源盒 error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 串口读取处理事件。只要功能是解析完整指令，存放到回复列表，通知其他线程有回复
        /// </summary>
        /// <param name="readStr">串口读取到的所以信息</param>
        /// <param name="errMsg">出错信息</param>
        public virtual void BaseSwitch_ThreadReadEvent(string readStr, string errMsg)
        {
            reserveRead += readStr;
            char[] chSplit = new char[1] { '\r' };
            bool isNotice = false;
            while (reserveRead.Contains("\r"))
            {
                //以"*"（2A）开头，"\r"（0D）结束
                string[] splits = reserveRead.Split(chSplit, 2);
                if(splits[0].ElementAt(0)=='*')
                {
                    AddResponds(splits[0]);
                    isNotice = true;
                }
                
                if (splits.Length == 2)
                {
                    reserveRead = splits[1];
                }
            }

            //通知正在等待的线程，收到完整指令了
            if (isNotice)
            {
                baseSwitchEvent.Set();
            }
        }

        /// <summary>
        /// 存放完整的指令
        /// </summary>
        /// <param name="respond">解析到的完整指令</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddResponds(string respond)
        {
            switchResponds.Add(respond);
        }

        /// <summary>
        /// 获取读取的完整指令
        /// </summary>
        /// <returns>返回第一条完整指令</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public string GetResponds()
        {
            if (switchResponds.Count > 0)
            {
                string respond = switchResponds[0];
                switchResponds.RemoveAt(0);
                return respond;
            }
            return "";
        }

        /// <summary>
        /// 清除所有的返回指令
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ClearResponds()
        {
            switchResponds.Clear();
        }

        /// <summary>
        /// 从指令文件中获取当前要发送的指令
        /// </summary>
        /// <param name="flag">切换的标志，格式暂定 产品序号:波长:通道:参数</param>
        /// <param name="cmdList">获取到要发送的所有指令</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错 2-指令文件不存在</returns>
        public int GetCmdByFlag(string flag, ref List<string> cmdList, ref string errMsg)
        {
            try
            {
                allCommand.Clear();
                string flagKey = NormalizeSwitchFlag(flag);
                if (string.IsNullOrEmpty(flagKey))
                {
                    errMsg += "光源盒 error：切换标志为空\r";
                    return 1;
                }
                if (!File.Exists(SwitchPath))
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:光源盒指令配置文件不存在!\r";
                    return 2;
                }
                using (StreamReader sr = new StreamReader(SwitchPath, Encoding.UTF8))
                {
                    string readLine;
                    while ((readLine = sr.ReadLine()) != null)
                    {
                        readLine = TrimSwitchLine(readLine);
                        if (readLine.Length == 0)
                            continue;
                        if (!TryParseBracketFlag(readLine, out string bracketFlag))
                            continue;
                        if (!FlagEquals(flagKey, bracketFlag))
                            continue;

                        CommandStruct temp;
                        temp.com = bracketFlag;
                        temp.cmdList = new List<string>();
                        int err = GetCmdList(bracketFlag, ref temp.cmdList, ref errMsg);
                        if (err != 0)
                            return err;
                        temp.priority = CountFlagSegments(bracketFlag);
                        allCommand.Add(temp);
                    }
                }
                if (allCommand.Count == 0)
                {
                    errMsg += "光源盒 error：指令配置文件中未找到匹配 flag=" + flagKey + "，文件:" + SwitchPath + "\r";
                    return 2;
                }
                cmdList = allCommand[0].cmdList;
                int maxPriority = allCommand[0].priority;
                for (int i = 1; i < allCommand.Count; i++)
                {
                    if (allCommand[i].priority > maxPriority)
                    {
                        cmdList = allCommand[i].cmdList;
                        maxPriority = allCommand[i].priority;
                    }
                }
                if (cmdList == null || cmdList.Count == 0)
                {
                    errMsg += "光源盒 error：flag=" + flagKey + " 未配置 MSW 指令，文件:" + SwitchPath + "\r";
                    return 2;
                }
                CheckSum(ref cmdList, ref errMsg);
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += "光源盒 error:" + ex.Message + "\r";
                return 1;
            }
        }

        private static string TrimSwitchLine(string line)
        {
            if (line == null)
                return "";
            return line.Trim().Trim('\uFEFF', '\r', '\n');
        }

        private static string NormalizeSwitchFlag(string flag)
        {
            return TrimSwitchLine(flag);
        }

        private static bool TryParseBracketFlag(string line, out string innerFlag)
        {
            innerFlag = "";
            line = TrimSwitchLine(line);
            if (line.Length < 3 || line[0] != '[' || line[line.Length - 1] != ']')
                return false;
            innerFlag = line.Substring(1, line.Length - 2).Trim();
            return innerFlag.Length > 0;
        }

        private static bool FlagEquals(string flag, string bracketInner)
        {
            return string.Equals(NormalizeSwitchFlag(flag), NormalizeSwitchFlag(bracketInner),
                StringComparison.OrdinalIgnoreCase);
        }

        private static int CountFlagSegments(string flag)
        {
            if (string.IsNullOrEmpty(flag))
                return 0;
            int count = 0;
            foreach (string part in flag.Split(':', '：'))
            {
                if (!string.IsNullOrEmpty(part))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 找出所有符合条件的指令
        /// 并附于优先级
        /// </summary>
        /// <param name="flagArr">发送的切换指令标志</param>
        /// <param name="readLine">配置文件中的指令标志</param>
        /// <param name="errMsg"></param>
        /// <returns>0-成功 1-出错 2-指令格式有误</returns>
        private int GetAllCommand(string[] flagArr, string readLine, ref string errMsg)
        {
            try
            {
                readLine = readLine.Substring(1, readLine.Length - 2);
                string[] readLineArr = readLine.Split(':', '：');
                int i = 0;
                for (; i < readLineArr.Length; i++)
                    if(readLineArr [i]!="")
                        if (flagArr[i] != readLineArr[i])
                            break;
                if (i >= readLineArr.Length)
                {
                    CommandStruct temp;
                    temp.com = readLine;
                    temp.cmdList = new List<string>();
                    int err = GetCmdList(readLine, ref temp.cmdList, ref errMsg);
                    if (err != 0)
                        return err;
                    temp.priority = 0;
                    for (int j = 0; j < readLineArr.Length; j++)
                        if (readLineArr[j] != "")
                            temp.priority++;
                    allCommand.Add(temp);
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
        /// 通过指令标志获取发送指令
        /// </summary>
        /// <param name="readLine"></param>
        /// <param name="cmdList"></param>
        /// <param name="errMsg"></param>
        /// <returns>0-成功 1-出错 2-指令文件不存在</returns>
        private int GetCmdList(string readLine, ref List<string> cmdList, ref string errMsg)
        {
            try
            {
                string targetFlag = NormalizeSwitchFlag(readLine);
                if (!File.Exists(SwitchPath))
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:光源盒指令配置文件不存在!\r";
                    return 2;
                }
                using (StreamReader sr = new StreamReader(SwitchPath, Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = TrimSwitchLine(line);
                        if (line.Length == 0)
                            continue;
                        if (!TryParseBracketFlag(line, out string bracketFlag))
                            continue;
                        if (!FlagEquals(targetFlag, bracketFlag))
                            continue;

                        while ((line = sr.ReadLine()) != null)
                        {
                            line = TrimSwitchLine(line);
                            if (line.Length == 0)
                                continue;
                            if (line[0] == '[')
                                break;
                            cmdList.Add(line);
                        }
                        break;
                    }
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
        /// 校验和计算
        /// </summary>
        /// <param name="cmdList">需要计算校验和的指令</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错</returns>
        public abstract int CheckSum(ref List<string> cmdList, ref string errMsg);

        /// <summary>
        /// 回复指令确认
        /// </summary>
        /// <param name="sendCmd">发送的指令</param>
        /// <param name="ackCmd">回复的指令</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错</returns>
        public abstract int AckCmdCheck(string sendCmd, string ackCmd, ref string errMsg);

        /// <summary>
        /// 切换光源盒
        /// </summary>
        /// <param name="flag">切换的标志，格式暂定 产品序号:波长:通道:参数</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错</returns>
        public virtual int SetSwitch(string flag, ref string errMsg)
        {
            try
            {
                List<string> cmdList = new List<string>();
                if (GetCmdByFlag(flag, ref cmdList, ref errMsg) != 0)
                {
                    errMsg = "光源盒 error：未找到指令配置文件：" + flag;
                    return 1;
                }
                for (int i = 0; i < cmdList.Count; i++)
                {
                    baseSwitchEvent.Reset();
                    string[] cmdArr = cmdList[i].Split(' ');
                    byte[] cmdArrBytes = new byte[cmdArr.Length];
                    for (int j = 0; j < cmdArr.Length; j++)
                        cmdArrBytes[j] = Convert.ToByte(cmdArr[j], 16);
                    int err = -1;
                    err = BaseSession.WriteSerailBytes(cmdArrBytes, ref errMsg);
                    if (err != 0)
                    {
                        errMsg += "光源盒 error:切换光开关写指令失败！\r";
                        return 1;
                    }
                    while (!baseSwitchEvent.WaitOne(TimeSpan.FromSeconds(2)))
                    {
                        errMsg = "光源盒 error:接收回复指令超时（2S）！\r";
                        return 1;
                    }
                    string ackCmd = GetResponds();
                    ClearResponds();
                    err = AckCmdCheck(cmdList[i], ackCmd, ref errMsg);
                    if (err != 0)
                    {
                        errMsg += "光源盒 error:切换光开关回复错误指令！\r";
                        return 1;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += "光源盒 error:" + ex.Message + "\r";
                return 1;
            }
        }

        ~OpticalSwitchBase()
        {
            if (BaseSession != null)
                BaseSession = null;
        }
    }
}
