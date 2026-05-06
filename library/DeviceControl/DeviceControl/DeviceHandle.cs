using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using MolexUtility;
using MolexUtility.Device;
using UDL2_ServerLib;
using System.IO;

namespace DeviceControl
{
    [Export(typeof(IDeviceHandle))]
    public class DeviceHandle: IDeviceHandle
    {
        /// <summary>
        /// 所有配置需要使用的设备的配置信息
        /// </summary>
        private List<List<DeviceConfig>> usedDeviceConfigs;

        /// <summary>
        /// 使用到的功率计
        /// </summary>
        private static List<IPowermeter> powermeters;

        /// <summary>
        /// 使用到的光源盒实
        /// </summary>
        private static List<IOpticalSwitch> opticalSwitchs;

        /// <summary>
        /// 使用到的静电计或万用表
        /// </summary>
        private static List<ICurrent> currents;

        private static List<IInterleaverScan> interleaverScans;

        private static List<ICDScan> cdScans;

        private static List<IFSTPScan> fstpScans;

        /// <summary>
        /// 使用到的光源，集成光源或者激光器等
        /// </summary>
        private static List<IOpticalSource> opticalSources;

        private static List<IAutomation> automations;

        private static List<IPDLController> pdlControllers;

        private static List<IUDLSwitch> udlSwitchs;

        private static List<IUDLTCC> udlTccs;

        private static List<IUDLFSTP> udlFstps;

        /// <summary>
        /// 设备控制 使用UDL
        /// </summary>
        public static UDL2_Engine deviceEngine = null;
        public static UDL2_TCC tccCtrl = null;
        public static UDL2_FSTP fstpCtrl = null;
        public static UDL2_OSW oswCtrl = null;

        public DeviceHandle()
        {
            powermeters = new List<IPowermeter>();
            opticalSwitchs = new List<IOpticalSwitch>();
            currents = new List<ICurrent>();
            opticalSources = new List<IOpticalSource>();
            interleaverScans = new List<IInterleaverScan>();
            automations = new List<IAutomation>();
            pdlControllers = new List<IPDLController>();
            cdScans = new List<ICDScan>();
            fstpScans = new List<IFSTPScan>();
            udlSwitchs = new List<IUDLSwitch>();
            udlFstps = new List<IUDLFSTP>();
            udlTccs = new List<IUDLTCC>();
        }

        public static bool GetUDLMessage(ref string msg)
        {
            try
            {
                string result = "";
                sbyte[] sbMsg = new sbyte[1024];
                byte[] bMsg = new byte[1024];

                deviceEngine.GetLastErrorMessage(out sbMsg[0], 1024);
                for (int i = 0; i < 1024; i++)
                {
                    bMsg[i] = (byte)sbMsg[i];
                }
                result = System.Text.Encoding.Default.GetString(bMsg);
                result = result.Substring(0, result.IndexOf('\0'));
                //CommonFunction.WriteLog(result);
                if (result.Length > 7 && result.Substring(0, 8) == "NO ERROR")
                    return true;
                else
                {
                    msg = result;
                    return false;
                }
                
            }
            catch (Exception e)
            {
                msg = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 初始化设备
        /// </summary>
        /// <param name="configPath">配置文件</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns></returns>
        public int InitDeviceByConfig(ref string errMsg)
        {
            try
            {
                if (File.Exists(Environment.CurrentDirectory + "\\set\\UDLConfig.xml"))
                {                  
                    deviceEngine = new UDL2_Engine();
                    tccCtrl = new UDL2_TCC();
                    fstpCtrl = new UDL2_FSTP();
                    oswCtrl = new UDL2_OSW();
                    deviceEngine.SetDebugLogFile(Environment.CurrentDirectory + "\\UDLlog.txt");
                    deviceEngine.LoadConfiguration(Environment.CurrentDirectory + "\\set\\UDLConfig.xml");
                    if (!GetUDLMessage(ref errMsg))
                    {
                        errMsg="加载UDL配置出错：" + errMsg;
                        return 1;
                    }
                    
                    deviceEngine.OpenEngine();
                    if (!GetUDLMessage(ref errMsg))
                    {
                        errMsg = "UDL Open出错：" + errMsg;
                        return 1;
                    }
                    
                }
                List<string> useNameList;
                //读取当前配置设备的配置文件
                ConfigXmlParser.ParseConfig(System.Environment.CurrentDirectory + "\\set\\Deviceconfig.xml", out useNameList, out usedDeviceConfigs);
                if (usedDeviceConfigs == null)
                {
                    errMsg = "设备初始化 error:" + "未配置任何设备" + "\r";
                    return 1;
                }

                foreach (List<DeviceConfig> configs in usedDeviceConfigs)
                {
                    foreach (DeviceConfig config in configs)
                    {
                        MolexUtility.CommonFunction.WriteLog(string.Format("init device:{0}", config.ControlName));
                        if (config.ControlName == Devices.Pwm1830.GetAdditional())
                        {                           
                            string err = "";
                            Powermeter1830 powermeter = new Powermeter1830(ref err, config.Control[0], "9600");
                            if (err.Length == 0)
                                powermeters.Add(powermeter);
                            else
                            {
                                errMsg += err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));
                                
                            }
                        }
                        else if (config.ControlName == Devices.PwmJH.GetAdditional())
                        {
                            string err = "";
                            PowermeterJH powermeter = new PowermeterJH(ref err, config.Control[0], "9600");
                            if (err.Length == 0)
                                powermeters.Add(powermeter);
                            else
                            {
                                errMsg += err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));

                            }
                        }
                        else if (config.ControlName == Devices.Interleaver.GetAdditional())
                        {
                            string err = "";
                            InterleaverScan scan = new InterleaverScan();
                            scan.InitAndConnectServer(ref err, config.Control[0], Convert.ToInt32(config.Control[1]), config.Control[2]);
                            if (err.Length == 0)
                                interleaverScans.Add(scan);
                            else
                            {
                                errMsg += err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));

                            }
                        }
                        else if (config.ControlName == Devices.Min1X8Switch.GetAdditional())
                        {
                            string err = "";
                            SwitchMini1X8 mini1X8Switch = new SwitchMini1X8(config.Control[0], config.Control[1], config.ShowName, ref err);
                            if (err.Length == 0)
                                opticalSwitchs.Add(mini1X8Switch);
                            else
                            {
                                errMsg += err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));

                            }
                        }
                        else if (config.ControlName == Devices.PboxSwitch.GetAdditional())
                        {
                            string err = "";
                            SwitchPbox pboxSwitch = new SwitchPbox(config.Control[0], config.Control[1], config.ShowName, ref err);
                            if (err.Length == 0)
                                opticalSwitchs.Add(pboxSwitch);
                            else
                            {
                                errMsg += err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));

                            }
                        }
                        else if (config.ControlName == Devices.OMSSwitch.GetAdditional())
                        {
                            string err = "";
                            SwitchOMS oplinkSwitch = new SwitchOMS(config.Control[0], config.Control[1], config.ShowName, ref err);
                            if (err.Length == 0)
                                opticalSwitchs.Add(oplinkSwitch);
                            else
                            {
                                errMsg += err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));

                            }
                        }
                        if (config.ControlName == Devices.PwmOplink1830.GetAdditional())
                        {
                            string err = "";
                            PowermeterOplink1830 powermeter = new PowermeterOplink1830(ref err, config.Control[0], "9600");
                            if (err.Length == 0)
                                powermeters.Add(powermeter);
                            else
                            {
                                errMsg += err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));

                            }
                        }
                        if (config.ControlName == Devices.Automation.GetAdditional())
                        {
                            Automation automation = new Automation(config.Control[0], Convert.ToInt32(config.Control[1]));
                            automations.Add(automation);
                        }
                        if (config.ControlName == Devices.OpiticalSourceBank.GetAdditional())
                        {
                            SrcBank srcbank = new SrcBank(ref errMsg, config.Control[0], Convert.ToInt32(config.Control[1]), Convert.ToInt32(config.Control[2]));
                            opticalSources.Add(srcbank);
                        }
                        if (config.ControlName == Devices.Opitical8164.GetAdditional())
                        {
                            Hp8164X srcbank = new Hp8164X(ref errMsg, config.Control[0], "38400");
                            opticalSources.Add(srcbank);
                        }
                        if (config.ControlName==Devices.PDLController.GetAdditional())
                        {
                            PDLController pdlCtrl = new PDLController(ref errMsg, config.Control[0]);
                            pdlControllers.Add(pdlCtrl);
                        }
                        if(config.ControlName==Devices.CDScan.GetAdditional())
                        {
                            string err = "";
                            CDScan cdScan = new CDScan();
                            if(cdScan.InitAndConnectServer(ref err, config.Control[0]))
                                cdScans.Add(cdScan);
                            else
                            {
                                errMsg += err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));

                            }
                        }
                        if (config.ControlName == Devices.NEWFSTPScan.GetAdditional())
                        {
                            string err = "";
                            FSTPScan fstpScan = new FSTPScan();
                            if (fstpScan.InitAndConnectFSTP(ref err, config.Control[0], Convert.ToInt32(config.Control[1])))
                                fstpScans.Add(fstpScan);
                            else
                            {
                                errMsg += "InitAndConnectFSTP error "+err;
                                MolexUtility.CommonFunction.WriteLog(string.Format("err:{0}", err));

                            }
                        }
                        if(config.ControlName==Devices.UDLFSTP.GetAdditional())
                        {
                            UDLFSTPScan scanObj = new UDLFSTPScan();
                            scanObj.InitFSTP(Convert.ToInt32(config.Control[0]), config.Control[1]);
                            udlFstps.Add(scanObj);
                        }
                        if (config.ControlName == Devices.UDLSwitch.GetAdditional())
                        {
                            UDLSwitch swObj = new UDLSwitch();
                            swObj.switchGUID = Convert.ToInt32(config.Control[0]);
                            udlSwitchs.Add(swObj);
                        }
                        if (config.ControlName == Devices.UDLTCC.GetAdditional())
                        {
                            UDLTccCtrl tccObj = new UDLTccCtrl();
                            tccObj.deviceGUID = Convert.ToInt32(config.Control[0]);
                            udlTccs.Add(tccObj);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }

            if (errMsg.Length > 0)
                return 2;
            return 0;
        }


        /// <summary>
        /// 关闭所有设备
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-正常 1-出错</returns>
        public int CloseAllDevice(ref string errMsg)
        {
            try
            {
                foreach(IPowermeter obj in powermeters)
                {
                    obj.PowermeterClose();
                }
                foreach(IInterleaverScan obj in interleaverScans)
                {
                    
                }

                foreach(IOpticalSwitch obj in opticalSwitchs)
                {
                    
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }

            if (errMsg.Length > 0)
                return 2;
            return 0;
        }

        public int GetUDLFstpByGUID(int guid,ref IUDLFSTP fstpScan,ref string errMsg)
        {
            foreach(IUDLFSTP scan in udlFstps)
            {
                if(scan.deviceGUID==guid)
                {
                    fstpScan = scan;
                    return 0;
                }
            }
            fstpScan = null;
            errMsg += " FSTP扫描：" + guid.ToString() + ": 不存在";
            return 1;
        }

        public int GetUDLSwitchByGUID(int guid, ref IUDLSwitch switchObj, ref string errMsg)
        {
            foreach (IUDLSwitch obj in udlSwitchs)
            {
                if (obj.switchGUID == guid)
                {
                    switchObj = obj;
                    return 0;
                }
            }
            switchObj = null;
            errMsg += " UDLSwitch：" + guid.ToString() + ": 不存在";
            return 1;
        }

        public int GetUDLTCCByGUID(int guid, ref IUDLTCC tccObj, ref string errMsg)
        {
            foreach (IUDLTCC obj in udlTccs)
            {
                if (obj.deviceGUID == guid)
                {
                    tccObj = obj;
                    return 0;
                }
            }
            tccObj = null;
            errMsg += " UDLTCC：" + guid.ToString() + ": 不存在";
            return 1;
        }

        /// <summary>
        /// 根据功率计index，获取功率计的对象，通道
        /// </summary>
        /// <param name="index">功率计index，从1开始</param>
        /// <param name="channel">功率计通道</param>
        /// <param name="curPowermeter">功率计对象</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0--成功  1--出错</returns>
        public int GetPowermeterByIndex(int index, ref int channel, ref IPowermeter desPowermeter, ref string errMsg)
        {
            try
            {
                if (powermeters.Count == 0 || index == 0)
                {
                    desPowermeter = null;
                    errMsg += "功率计" + index.ToString() + ": 该功率计不存在";
                    return 1;
                }
                else
                {
                    int totalCount = 0;
                    for (int i = 0; i < powermeters.Count; i++)
                    {
                        totalCount += powermeters[i].ChannelCount;
                        if (totalCount == index)
                        {
                            desPowermeter = powermeters[i];
                            //通道index从0开始，所有再减1
                            channel = powermeters[i].ChannelCount - 1;
                            return 0;
                        }
                        else if ((totalCount - index) > 0 && powermeters[i].ChannelCount > 1)
                        {
                            desPowermeter = powermeters[i];
                            //功率计序号 -（之前的功率计总的数量），即为当前的通道，通道index从0开始，所有再减1
                            channel = (index - (totalCount - powermeters[i].ChannelCount)) - 1;
                            return 0;
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        public int GetCDScanByIndex(int index, ref ICDScan cdScan, ref string errMsg)
        {
            try
            {
                if (index > cdScans.Count)
                {
                    cdScan = null;
                    errMsg += " CD扫描：" + index.ToString() + ": 该通道不存在";
                    return 1;
                }
                cdScan = cdScans[index - 1];
               
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        public int GetFSTPScanByType(int nType,ref IFSTPScan fstpScan,ref string errMsg)
        {
            foreach (IFSTPScan scan in fstpScans)
            {
                if (scan.FSTPType == nType)
                {
                    fstpScan = scan;
                    return 0;
                }
            }
            fstpScan = null;
            errMsg += " FSTP扫描：" + nType.ToString() + ": 该通道不存在";
            return 1;
        }

        /// <summary>
        /// 根据标准获取要扫描的对象，flag为产品_ODD/EVEN
        /// </summary>
        /// <param name="flag">产品_ODD/EVEN</param>
        /// <param name="desScan">获取到的扫描对象</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功  1--出错</returns>
        public int GetInterleaverScanByFlag(int flag,ref IInterleaverScan desScan,ref string errMsg)
        {
            try
            {
                if (flag>interleaverScans.Count)
                {
                    desScan = null;
                    errMsg += " Interleaver扫描：" + flag + ": 该通道不存在";
                    return 1;
                }
                desScan = interleaverScans[flag - 1];
                /*else
                {
                    foreach (IInterleaverScan scan in interleaverScans)
                    {
                        if (scan.Flag==flag)
                        {
                            desScan = scan;
                            return 0;
                        }
                    }
                }
                desScan = null;
                errMsg += " Interleaver扫描：" + flag + ": 该通道不存在";*/
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 获取偏振控制器对象
        /// </summary>
        /// <param name="index">设备Index，从1开始</param>
        /// <param name="pdlCtrl">偏振控制器对象</param>
        /// <param name="errMsg">出错具体信息</param>
        /// <returns>0--正确  1--出错</returns>
        public int GetPDLControllerByIdx(int nIdx,ref IPDLController pdlCtrl,ref string errMsg)
        {
            try
            {
                if (pdlControllers.Count == 0 || nIdx == 0 || pdlControllers.Count < nIdx)
                {
                    pdlCtrl = null;
                    errMsg += "偏振控制器 " + nIdx.ToString() + ": 该设备不存在";
                    return 1;
                }
                else
                {
                    pdlCtrl = pdlControllers[nIdx - 1];
                    return 0;
                }

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 根据类型获取光源盒对象
        /// </summary>
        /// <param name="type">光源盒类型，与配置的指令配置文件名称一致,如果为空，则返回第一个初始化的光源盒</param>
        /// <param name="desSwitch">获取的光源盒对象</param>
        /// <param name="errMsg">具体错误信息</param>
        /// <returns>0--正确  1--出错</returns>
        public int GetSwitchByType(string type,ref IOpticalSwitch desSwitch,ref string errMsg)
        {
            try
            {
                if (opticalSwitchs.Count == 0)
                {
                    desSwitch = null;
                    errMsg += "光源盒 " + type + ": 该光源盒不存在";
                    return 1;
                }
                else
                {
                    foreach (IOpticalSwitch optical in opticalSwitchs)
                    {
                        if(type==null||type.Length==0|| optical.SwitchName == type)
                        {
                            desSwitch = optical;
                            return 0;
                        }
                    }
                }
                desSwitch = null;
                errMsg += "光源盒 " + type + ": 该光源盒不存在";
                return 1;
            }
            catch(Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 根据类型获取光源盒对象
        /// </summary>
        /// <param name="idx">设备Index，从1开始</param>
        /// <param name="desSwitch">获取的光源盒对象</param>
        /// <param name="errMsg">具体错误信息</param>
        /// <returns>0--正确  1--出错</returns>
        public int GetSwitchByIndex(int idx, ref IOpticalSwitch desSwitch, ref string errMsg)
        {
            try
            {
                if (opticalSwitchs.Count == 0)
                {
                    desSwitch = null;
                    errMsg += "光源盒 " + idx + ": 该光源盒不存在";
                    return 1;
                }
                else
                {
                    desSwitch = opticalSwitchs[0];
                    return 0;
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 获取静电计或者万用表对象
        /// </summary>
        /// <param name="index">设备Index，从1开始</param>
        /// <param name="desCurrent">静电计或者万用表对象</param>
        /// <param name="errMsg">出错具体信息</param>
        /// <returns>0--正确  1--出错</returns>
        public int GetCurrentByIndex(int index,ref ICurrent desCurrent,ref string errMsg)
        {
            try
            {
                if(currents.Count==0||index==0 || currents.Count < index)
                {
                    desCurrent = null;
                    errMsg += "静电计或者万用表 " + index.ToString() + ": 该设备不存在";
                    return 1;
                }
                else
                {
                    desCurrent = currents[index - 1];
                    return 0;
                }
               
            }
            catch(Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }


        /// <summary>
        /// 根据波长和光源类型进行查找
        /// </summary>
        /// <param name="index">设备Index，从1开始</param>
        /// <param name="desOptical">光源对象</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功  1--出错</returns>
        public int GetOpticalSourceByWaveAndType(int index, ref IOpticalSource desOptical, ref string errMsg)
        {
            try
            {
                if (opticalSources.Count == 0 || index == 0 || opticalSources.Count < index)
                {
                    desOptical = null;
                    errMsg += "光源设备 " + index.ToString() + ": 该设备不存在";
                    return 1;
                }
                else
                {
                    desOptical = opticalSources[index - 1];
                    return 0;
                }

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        public int GetAutomationInIndex(int index, ref IAutomation automation, ref string errMsg)
        {
            try
            {
                if (automations.Count == 0 || index == 0 || automations.Count < index)
                {
                    automation = null;
                    errMsg += "自动化服务器 " + index.ToString() + ": 该设备不存在";
                    return 1;
                }
                else
                {
                    automation = automations[index - 1];
                    return 0;
                }

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
