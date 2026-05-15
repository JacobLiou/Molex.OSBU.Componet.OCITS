using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MolexUtility
{
    [Serializable]
    public class MESControl
    {
        /// <summary>
        /// 防止线程访问冲突，用于互斥
        /// </summary>
        private object lockObj = new object();

        /// <summary>
        /// 保存ini文件里Global Setting的信息
        /// </summary>
        private MESGlobalSetting globalSetting = new MESGlobalSetting();

        /// <summary>
        /// 所有测试项
        /// </summary>
        private List<MESTestInfo> allTestInfo = new List<MESTestInfo>();

        /// <summary>
        /// 产品相关信息
        /// </summary>
        private MESProductInfo productInfo = new MESProductInfo();

        private List<ScanData> scanReference = new List<ScanData>();
        private List<ScanData> scanRawData = new List<ScanData>();
        private List<ScanData> gffOriginal = new List<ScanData>();

        /// <summary>
        /// 测试信息列表显示
        /// </summary>
        //private UIParamShow testParamShow = null;

        /// <summary>
        /// 工位代号
        /// </summary>
        private string workStationID = "";

        /// <summary>
        /// 用户ID
        /// </summary>
        private string userID = "";

        /// <summary>
        /// 模板类型
        /// </summary>
        private MESTemplateType templateType;

        /// <summary>
        /// 测试工序
        /// </summary>
        private MESTestProcess testProcess;

        /// <summary>
        /// 测试类型
        /// </summary>
        private MESTestType testType;

        /// <summary>
        /// 打开模板的时间
        /// </summary>
        private string openTemplateTime = "";

        public string ProductSN { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="show">用于测试项显示的对象，可以为空</param>
        /*public MESControl(UIParamShow show=null)
        {
            testParamShow = show;
        }*/

        /// <summary>
        /// 打开产品的模板，并解析出测试相关信息
        /// </summary>
        /// <param name="serverAddress">无纸化服务器地址</param>
        /// <param name="template">模板类型</param>
        /// <param name="sn">产品SN号</param>
        /// <param name="process">测试工序</param>
        /// <param name="type">测试类型</param>
        /// <param name="operaterID">操作员工号</param>
        /// <param name="goldSample">工位goldsample</param>
        /// <param name="isLoad">是否要加载解析模板</param>
        /// <param name="isShowData">是否要加载已经测试过数据</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>是否出错</returns>
        public bool OpenTemplate(
            string serverAddress,
            MESTemplateType template,
            string sn,
            MESTestProcess process,
            MESTestType type,
            string operaterID,
            string goldSample,
            bool isLoad,
            bool isShowData,
            ref string errMsg
            )
        {
            try
            {
                if(sn.Length==0)
                {
                    errMsg = "请输入产品号！";
                    return false;
                }
                
                string templateAdrress = serverAddress + template.GetMESTemplateKeywords() + sn + process.GetMESTemplateKeywords() + type.GetMESTemplateKeywords();
                templateAdrress += "&workstation=" + goldSample;
                templateAdrress += "&user=" + operaterID;
                
                if (isShowData)
                    templateAdrress += "&showdata=1";
                string[] strNodeName = new string[20];
                strNodeName[0] = "TempletID";
                strNodeName[1] = "Version";
                strNodeName[2] = "PN";
                strNodeName[3] = "PC";
                strNodeName[4] = "PT";
                strNodeName[5] = "SpecNo";
                strNodeName[6] = "Spec";
                strNodeName[7] = "SO";
                strNodeName[8] = "PROCESS_TYPE";
                strNodeName[9] = "PROCESS_TYPE_UP";
                strNodeName[10] = "DeviceNo";
                strNodeName[11] = "SetupModel";
                strNodeName[12] = "FinishModel";
                strNodeName[13] = "Precheck";
                strNodeName[14] = "GS_Status";
                strNodeName[15] = "GS_Date";
                strNodeName[16] = "Hint";
                strNodeName[17] = "TempletHint";
                strNodeName[18] = "FreeCheckType";
                strNodeName[19] = "TempleData";
                string[] strNodeContent;
                if (!CommonFunction.GetNodeContentByName(templateAdrress, "AutoTemplate", strNodeName, out strNodeContent, out errMsg))
                    return false;
                productInfo.Clear();
                productInfo.SN = sn;
                workStationID = goldSample;
                userID = operaterID;
                templateType = template;
                testProcess = process;
                testType = type;

                productInfo.TemplateID = strNodeContent[0];
                productInfo.Version = strNodeContent[1];
                productInfo.ProductPN = strNodeContent[2];
                productInfo.PC = strNodeContent[3];
                productInfo.PT = strNodeContent[4];
                productInfo.SpecNO = strNodeContent[5];
                productInfo.Spec = strNodeContent[6];
                productInfo.SO = strNodeContent[7];
                productInfo.ProcessType = strNodeContent[8];
                productInfo.ProcessTypeUp = strNodeContent[9];
                productInfo.DeviceNO = strNodeContent[10];
                productInfo.SetupModel = strNodeContent[11];
                productInfo.FinishModel = strNodeContent[12];
                productInfo.Precheck = strNodeContent[13];
                productInfo.GSStatus = strNodeContent[14];
                productInfo.GSDate = strNodeContent[15];
                productInfo.Hint = strNodeContent[16];
                productInfo.TempletHint = strNodeContent[17];
                productInfo.FreeCheckType = strNodeContent[18];
                if (isLoad)
                {
                    allTestInfo.Clear();
                    globalSetting.Clear();
                    string strPath = System.Environment.CurrentDirectory;
                    strPath += "\\temple\\tempdata.ini";
                    CommonFunction.WriteFile(strPath, strNodeContent[19]);
                    if (!ParserTemplateFile(strPath, ref errMsg))
                        return false;
                    openTemplateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    /*if (testParamShow != null)
                    {
                        //testParamShow.InitView(GetAllTestInfo());
                    }*/
                    ProductSN = sn;
                }

            }
            catch (Exception ex)
            {
                errMsg = "OpenTemplate 出错：" + ex.Message;
                return false;
            }
            return true;
        }

        
        /// <summary>
        /// ini文件解析（后续增加所有类型解析）
        /// </summary>
        /// <param name="strFilePath">文件路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>是否成功完成操作</returns>
        public bool ParserTemplateFile(string strFilePath, ref string errMsg)
        {
            try
            {
                IniParser templateParser = new IniParser(strFilePath,true);
                //globalsetting 用单独函数解析
                if (!ParserGlobalSetting(ref templateParser, ref errMsg))
                    return false;

                string strSection = "";
                string strKey = "";
                
                for (int i = 0; i < globalSetting.TmptCount; i++)
                {
                    string strTmptSection = "Tmpt" + i.ToString();
                    strKey = "Channel Config";
                    string strAllPort = templateParser.readStringData(strTmptSection, strKey);
                    string[] PortArray = strAllPort.Split(';');
                    
                    foreach (string strPort in PortArray)
                    {
                        string strStartWL = "";
                        string strStopWL = "";
                        string strStepWL = "";
                        string strUserPortName = "";
                        string strITU = "";
                        int nDBCount = 0;
                        int nITUCount = 0;
                        int nWLCount = 0;
                        int nEXCount = 0;
                        if (strPort.Length == 0)
                            continue;
                        string strParserPort = strPort.Replace(":", " -> ");
                        strSection = strTmptSection + " Port " + strParserPort + " Setting";
                        strUserPortName = templateParser.readStringData(strSection, "Port Caption");
                        strStepWL = templateParser.readStringData(strSection, "Step Size", "0");
                        strStartWL = templateParser.readStringData(strSection, "Start WL", "0");
                        strStopWL = templateParser.readStringData(strSection, "Stop WL", "0");
                        strITU = templateParser.readStringData(strSection, "ITU", "0");
                        nDBCount = templateParser.readIntData(strSection, "dB Count");
                        nITUCount = templateParser.readIntData(strSection, "ITU Count");
                        nWLCount = templateParser.readIntData(strSection, "WL Count");
                        nEXCount = templateParser.readIntData(strSection, "EX Count");
                        
                        //解析DB参数
                        List<MESTestInfo> PortAllInfo = new List<MESTestInfo>();
                        for (int j = 0; j < nDBCount; j++)
                        {
                            List<MESTestInfo> ParamInfo;
                            if (!ParserParam(ref templateParser, strTmptSection, strParserPort, "dB", j, out ParamInfo, ref errMsg))
                                return false;
                            foreach (MESTestInfo info in ParamInfo)
                            {
                                info.ParamType = MESParamRule.DB;
                                PortAllInfo.Add(info);
                            }
                        }
                        //解析ITU参数 
                        for (int j = 0; j < nITUCount; j++)
                        {
                            List<MESTestInfo> ParamInfo;
                            if (!ParserParam(ref templateParser, strTmptSection, strParserPort, "ITU", j, out ParamInfo, ref errMsg))
                                return false;
                            foreach (MESTestInfo info in ParamInfo)
                            {
                                info.ParamType = MESParamRule.ITU;
                                PortAllInfo.Add(info);
                            }
                        }
                        //差ex
                        for (int j = 0; j < nEXCount; j++)
                        {
                            List<MESTestInfo> ParamInfo;
                            if (!ParserEXParam(ref templateParser, strTmptSection, strParserPort, j, out ParamInfo, ref errMsg))
                                return false;
                            foreach (MESTestInfo info in ParamInfo)
                            {
                                info.ParamType = MESParamRule.EX;
                                PortAllInfo.Add(info);
                            }

                        }
                        //解析WL参数 
                        for (int j = 0; j < nWLCount; j++)
                        {
                            List<MESTestInfo> ParamInfo;
                            if (!ParserParam(ref templateParser, strTmptSection, strParserPort, "WL", j, out ParamInfo, ref errMsg))
                                return false;
                            foreach (MESTestInfo info in ParamInfo)
                            {
                                info.ParamType = MESParamRule.WL;
                                PortAllInfo.Add(info);
                            }
                        }
                        
                        foreach (MESTestInfo info in PortAllInfo)
                        {
                            info.PortNameForAMTS = strPort.Replace(":", "->");
                            info.PortNameForUser = strUserPortName;
                            info.StartWL = Convert.ToDouble(strStartWL);
                            info.StopWL = Convert.ToDouble(strStopWL);
                            info.Step = Convert.ToDouble(strStepWL);
                            info.ITU = Convert.ToDouble(strITU);
                            info.Temperature = globalSetting.TmptArray[i];
                            //无纸化保存时温度
                            info.SaveTemperature = i.ToString();
                            info.TmptChangeTimes = globalSetting.TmptTimeArray[i];
                            allTestInfo.Add(info);
                        }
                    }
                }
                templateParser.CloseFile();
            }
            catch (Exception ex)
            {
                errMsg = "ParserTemplateFile 出错：" + ex.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 解析模板ini文件里Global Setting里的信息
        /// </summary>
        /// <param name="tpParser">文件解析实例化对象</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>是否成功完成操作</returns>
        private bool ParserGlobalSetting(ref IniParser tpParser, ref string errMsg)
        {
            try
            {
                string strSection = "Global Setting";
                string strKey = "";
                globalSetting.TmptCount = tpParser.readIntData(strSection, "Tmpt Count", 0);
                for (int i = 0; i < globalSetting.TmptCount; i++)
                {
                    strKey = string.Format("Tmpt{0}", i);
                    globalSetting.TmptArray[i] = Convert.ToDouble(tpParser.readStringData(strSection, strKey));
                    strKey = string.Format("Time{0}", i);
                    globalSetting.TmptTimeArray[i] = Convert.ToDouble(tpParser.readStringData(strSection, strKey));
                }
                //剩余解析
            }
            catch (Exception ex)
            {
                errMsg = "ParserGlobalSetting 出错：" + ex.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 解析每个参数的详细信息
        /// </summary>
        /// <param name="tpParser">文件解析实例化对象</param>
        /// <param name="strTmpt">温度</param>
        /// <param name="strPort">端口</param>
        /// <param name="strType">参数类型</param>
        /// <param name="nIndex">第几个参数</param>
        /// <param name="paramInfo">解析所得参数信息</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>成功true，失败false</returns>
        private bool ParserParam(ref IniParser tpParser, string strTmpt, string strPort, string strType, int nIndex, out List<MESTestInfo> paramInfo, ref string errMsg)
        {
            paramInfo = null;
            try
            {
                List<MESTestInfo> listParamInfo = new List<MESTestInfo>();
                string strSection = "";
                strSection = string.Format("{0} Port {1} {2}{3:00} Settings", strTmpt, strPort, strType, nIndex);
                string strCode = tpParser.readStringData(strSection, "Setting Code EX");
                int nCodeIndex = 0;
                foreach (char ch in strCode)
                {
                    nCodeIndex++;
                    if (ch == '0')
                        continue;
                    MESTestInfo getParamInfo = new MESTestInfo();

                    getParamInfo.TestParam = (MESParam)nCodeIndex;
                    getParamInfo.ParamColumnName = getParamInfo.TestParam.GetMESTemplateKeywords();
                    string strKey = "";
                    strKey = getParamInfo.TestParam.GetMESTemplateKeywords();
                    //getParamInfo.Criterion = Convert.ToDouble(tpParser.readStringData(strSection, strKey, "0"));
                    getParamInfo.Criterion = tpParser.readStringData(strSection, strKey, "0");
                    strKey = string.Format("{0}1", getParamInfo.TestParam.GetMESTemplateKeywords());
                    //getParamInfo.Criterion1 = Convert.ToDouble(tpParser.readStringData(strSection, strKey, "0"));
                    getParamInfo.Criterion1 = tpParser.readStringData(strSection, strKey, "0");
                    strKey = string.Format("Free {0}", getParamInfo.TestParam.GetMESTemplateKeywords());
                    getParamInfo.FreeLowestCriterion = tpParser.readStringData(strSection, strKey, "0");
                    strKey = string.Format("Free {0}1", getParamInfo.TestParam.GetMESTemplateKeywords());
                    getParamInfo.FreeHighestCriterion = tpParser.readStringData(strSection, strKey, "0");
                    strKey = string.Format("{0} Value", getParamInfo.TestParam.GetMESTemplateKeywords());
                    getParamInfo.TestedValue = Convert.ToDouble(tpParser.readStringData(strSection, strKey, "0"));
                    //读出测试过数据，并显示状态
                    if (getParamInfo.TestedValue.CompareTo(CommonFunction.GetDefaultValue()) != 0
                        && getParamInfo.TestedValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    {
                        getParamInfo.CurValue = getParamInfo.TestedValue;
                        getParamInfo.Tested = true;
                        CheckPassOrFail(getParamInfo);

                    }
                    getParamInfo.BandType = Convert.ToUInt64(tpParser.readIntData(strSection, "Band Type", 0));
                    getParamInfo.WLLeft = Convert.ToDouble(tpParser.readStringData(strSection, "WL Left", "0"));
                    getParamInfo.WLRight = Convert.ToDouble(tpParser.readStringData(strSection, "WL Right", "0"));
                    getParamInfo.SettingValue = Convert.ToDouble(tpParser.readStringData(strSection, "Setting Value", "0"));
                    listParamInfo.Add(getParamInfo);
                }

                paramInfo = listParamInfo;
            }
            catch (Exception ex)
            {
                errMsg = "ParserParam 出错：" + ex.Message;
                return false;
            }
            return true;
        }

        private bool ParserEXParam(ref IniParser tpParser,string strTmpt,string strPort,int nIndex,out List<MESTestInfo> paramInfo,ref string errMsg)
        {
            paramInfo = null;
            try
            {
                List<MESTestInfo> listParamInfo = new List<MESTestInfo>();
                string strSection = "";
                strSection = string.Format("{0} Port {1} EX{2:00} Settings", strTmpt, strPort, nIndex);
                
                MESTestInfo getParamInfo = new MESTestInfo();

                //getParamInfo.TestParam = (MESParam)nCodeIndex;                  
                List<string> attrKeys = new List<string>();                
                attrKeys.Add("Band Type");
                attrKeys.Add("Setting Value");
                attrKeys.Add("Key Name");

                string[] attrs;
                tpParser.readStringData(strSection, attrKeys.ToArray(), out attrs);
                                
                getParamInfo.BandType = Convert.ToUInt64(attrs[0]);
                getParamInfo.WLLeft = Convert.ToDouble(attrs[1]);
                getParamInfo.ExParamName = attrs[2];
                getParamInfo.ParamColumnName = getParamInfo.ExParamName;

                List<string> keys = new List<string>();              
                string strKey = getParamInfo.ExParamName;
                keys.Add(strKey);
                keys.Add(string.Format("{0}1", getParamInfo.ExParamName));
                keys.Add(string.Format("Free {0}", getParamInfo.ExParamName));
                keys.Add(string.Format("Free {0}1", getParamInfo.ExParamName));
                keys.Add(string.Format("{0} Value", getParamInfo.ExParamName));
                string[] results;
                tpParser.readStringData(strSection, keys.ToArray(), out results);
                //getParamInfo.Criterion = Convert.ToDouble(results[0]);
                
                //getParamInfo.Criterion1 = Convert.ToDouble(results[1]);
                getParamInfo.Criterion = results[0];
                getParamInfo.Criterion1 = results[1];
                
                getParamInfo.FreeLowestCriterion = results[2];
                
                getParamInfo.FreeHighestCriterion =results[3];
                
                getParamInfo.TestedValue = Convert.ToDouble(results[4]);

                //读出测试过数据，并显示状态
                if (getParamInfo.TestedValue.CompareTo(CommonFunction.GetDefaultValue()) != 0
                    && getParamInfo.TestedValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                {
                    getParamInfo.CurValue = getParamInfo.TestedValue;
                    getParamInfo.Tested = true;
                    CheckPassOrFail(getParamInfo);

                }
                
                //getParamInfo.TestParam = MESParam.DefineEx;
                listParamInfo.Add(getParamInfo);
              
                paramInfo = listParamInfo;
            }
            catch (Exception ex)
            {
                errMsg = "ParserParam 出错：" + ex.Message;
                return false;
            }
            return true;
        }
        /// <summary>
        /// 获取模板中global的设置信息，返回的是clone对象
        /// </summary>
        /// <returns></returns>
        public MESGlobalSetting GetGlobalSetting()
        {
            lock (lockObj)
            {
                return globalSetting.Clone();
            }
        }

        /// <summary>
        /// 根据index来获取测试信息
        /// </summary>
        /// <param name="nIndex">第几行测试信息</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>测试信息</returns>
        public MESTestInfo GetTestInfoByIndex(int nIndex, ref string errMsg)
        {
            lock (lockObj)
            {
                if (nIndex < 0 || allTestInfo.Count == 0)
                {
                    errMsg = "当前选中行无测试信息！";
                    return null;
                }
                if(allTestInfo[nIndex].TestParam==MESParam.Default)
                {
                    errMsg = "当前选中行无测试信息！";
                    return null;
                }
                return allTestInfo[nIndex].Clone();
            }
        }

        /// <summary>
        /// 获取产品相关信息
        /// </summary>
        /// <returns>产品相关信息</returns>
        public MESProductInfo GetProductInfo()
        {
            lock (lockObj)
            {
                return productInfo.Clone();
            }
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void ClearAllData()
        {
            lock (lockObj)
            {
                productInfo.Clear();
                allTestInfo.Clear();
                globalSetting.Clear();
            }
        }

        /// <summary>
        /// 获取所有测试信息
        /// </summary>
        /// <returns>返回所有测试信息clone对象</returns>
        /*public MESTestInfo[] GetAllTestInfo()
        {
            lock (lockObj)
            {
                MESTestInfo[] testInfoArray = new MESTestInfo[allTestInfo.Count];
                int i = 0;
                foreach (MESTestInfo info in allTestInfo)
                {
                    testInfoArray[i] = info.Clone();
                    i++;
                }
                return testInfoArray;
            }
        }*/

        public MESControl Clone()
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return formatter.Deserialize(stream) as MESControl;
            //return this.MemberwiseClone() as MESControl;

        }

        /// <summary>
        /// 获取所有测试信息
        /// </summary>
        /// <returns>返回所有测试信息clone对象</returns>
        public List<MESTestInfo> GetAllTestInfo()
        {
            lock (lockObj)
            {
                List<MESTestInfo> testInfoArray = new List<MESTestInfo>();
                
                foreach (MESTestInfo info in allTestInfo)
                {
                    testInfoArray.Add(info.Clone());
                }
                return testInfoArray;
            }
        }

        /// <summary>
        /// 删除测试行
        /// </summary>
        /// <param name="deleteIndexs">需要啊删除的行index</param>
        public void DeleteParams(List<int> deleteIndexs)
        {
            lock (lockObj)
            {
                for(int i= deleteIndexs.Count-1;i>=0;i--)
                {
                    if (allTestInfo.Count > deleteIndexs[i])
                    {
                        allTestInfo.RemoveAt(deleteIndexs[i]);
                    }
                }
                           
            }
        }

        /// <summary>
        /// 更新显示的列名称，去掉separator后面的内容
        /// </summary>
        /// <param name="separator">分隔符</param>
        public void ColumnDeleteAfterSep(params char[] separator)
        {
            lock (lockObj)
            {
                foreach(MESTestInfo info in allTestInfo)
                {
                    string column = info.ParamColumnName;
                    string[] splits = column.Split(separator);
                    if(splits.Length>0)
                        info.ParamColumnName = splits[0];
                }               
            }
        }

        /// <summary>
        /// 列显示名称，将特定字符用其他字符代替
        /// </summary>
        /// <param name="sourceStr">需要被代替的字符</param>
        /// <param name="destStr">代替字符</param>
        public void ColumnReplaceStr(string sourceStr,string destStr)
        {
            lock (lockObj)
            {
                foreach (MESTestInfo info in allTestInfo)
                {
                    string column = info.ParamColumnName;                    
                    info.ParamColumnName = column.Replace(sourceStr, destStr);
                }
            }
        }
        /// <summary>
        /// 测试项里面插入一行空行，有些测试软件显示时需要做分割
        /// </summary>
        /// <param name="index">在index后插入空行</param>
        /// <returns>是否插入成功</returns>
        public bool InsertEmptyTestInfo(int index)
        {
            if (allTestInfo.Count > index)
            {
                MESTestInfo newTestInfo = new MESTestInfo();
                allTestInfo.Insert(index, newTestInfo);

                /*if (testParamShow != null)
                {
                    testParamShow.InsertRow(index, newTestInfo);
                }*/
                return true;
            }
            return false;
        }

        /// <summary>
        /// 是否有测试数据
        /// </summary>
        /// <returns>true-有，false-没有</returns>
        public bool GetHasTested()
        {
            lock (lockObj)
            {
                if (allTestInfo.Count == 0)
                    return false;
                foreach (MESTestInfo info in allTestInfo)
                {
                    if (info.Tested)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 更新测试结果
        /// </summary>
        /// <param name="nIndex">更新nIndex行测试结果</param>
        /// <param name="dRes">结果</param>
        /// <returns>返回当前更新项</returns>
        public MESTestInfo UpdateTestData(int nIndex, double dRes,ref bool isPass)
        {
            isPass = true;
            MESTestInfo testInfo = null;
            lock (lockObj)
            {
                if (allTestInfo.Count <= nIndex)
                {
                    return null;
                }
                allTestInfo[nIndex].CurValue = dRes;
                allTestInfo[nIndex].Tested = true;
                
                allTestInfo[nIndex].Pass = CheckPassOrFail(allTestInfo[nIndex]);
                
                testInfo = allTestInfo[nIndex].Clone();
            }

            //更新到界面
            /*if (testParamShow != null)
            {
                testParamShow.UpdateDataView(nIndex, testInfo);
            }*/
            isPass = testInfo.Pass;
            return testInfo;
        }

        /// <summary>
        /// 更新归零数据
        /// </summary>
        /// <param name="nIndex">更新nIndex行的数据</param>
        /// <param name="dILRef">归零数据</param>
        public MESTestInfo UpdateILRefData(int nIndex, double dILRef)
        {
            MESTestInfo testInfo = null;
            lock (lockObj)
            {
                if (allTestInfo.Count > nIndex)
                {
                    allTestInfo[nIndex].ILRef = dILRef;
                    testInfo = allTestInfo[nIndex].Clone();
                }
            }
            return testInfo;
            //更新到界面
            /*if (testParamShow != null)
            {
                testParamShow.UpdateRefView(nIndex, testInfo);
            }*/
        }

        public MESTestInfo UpdateScanRefStatus(int nIndex,bool isRef)
        {
            MESTestInfo testInfo = null;
            lock (lockObj)
            {
                if (allTestInfo.Count > nIndex)
                {
                    allTestInfo[nIndex].IsScanRef = isRef;
                    testInfo = allTestInfo[nIndex].Clone();
                }
            }
            return testInfo;
        }

        /// <summary>
        /// 更新RL归零数据
        /// </summary>
        /// <param name="nIndex">更新nIndex行RL归零数据</param>
        /// <param name="dRLRef">归零数据</param>
        public MESTestInfo UpdateRLRefData(int nIndex, double dRLRef)
        {
            MESTestInfo testInfo = null;
            lock (lockObj)
            {
                if (allTestInfo.Count > nIndex)
                {
                    if (allTestInfo[nIndex].TestParam == MESParam.RL)
                        allTestInfo[nIndex].RLRef = dRLRef;
                    testInfo = allTestInfo[nIndex].Clone();
                }
            }
            return testInfo;
            //更新到界面
            /*if (testParamShow != null)
            {
                testParamShow.UpdateRefView(nIndex, testInfo);
            }*/
        }

        /// <summary>
        /// 更新扫描归零数据
        /// </summary>
        /// <param name="reference">归零数据</param>
        public void UpdateScanReference(ScanData reference)
        {
        }

        /// <summary>
        /// 更新测试扫描数据
        /// </summary>
        /// <param name="rawData">测试扫描数据</param>
        public void UpdateScanRawData(ScanData rawData)
        {
        }

        /// <summary>
        /// 更新gff原始数据
        /// </summary>
        /// <param name="gffData">gff原始数据</param>
        public void UpdateGFFOriginalData(ScanData gffData)
        {
        }

        /// <summary>
        /// 是否全部都归零
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <returns>是否全部归零</returns>
        public bool GetAllRef(ref string errMsg)
        {
            //errMsg="归零数据不完整！"
            List<MESTestInfo> allInfo = null;
            lock (lockObj)
            {
                allInfo = GetAllTestInfo();
            }
            for (int i = 0; i < allInfo.Count; i++)
            {
                if (allInfo[i].ILRef == CommonFunction.GetDefaultValue()
                    || allInfo[i].ILRef == CommonFunction.GetFormatDefaultValue())
                {
                    errMsg = "IL归零数据不完整！";
                    return false;
                }
                if (allInfo[i].TestParam == MESParam.RL)
                {
                    if (allInfo[i].RLRef == CommonFunction.GetDefaultValue()
                        || allInfo[i].RLRef == CommonFunction.GetFormatDefaultValue())
                    {
                        errMsg = "RL归零数据不完整！";
                        return false;
                    }
                }
            }
                
            
            //更新到界面
           /* if (testParamShow != null)
            {
                for (int i = 0; i < allInfo.Count; i++)
                {
                    testParamShow.UpdateRefView(i, allInfo[i]);
                }
            }*/

            return true;
        }

        /// <summary>
        /// 是否全部测试完成
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <returns>是否全部测试完</returns>
        public bool GetAllTested(out int nIndex, ref string errMsg)
        {
            //errMsg="有未测试项！"
            lock (lockObj)
            {
                int nUnTestedIdx = -1;
                for (int i = 0; i < allTestInfo.Count; i++)
                {
                    if (!allTestInfo[i].Tested)
                    {
                        nUnTestedIdx = i;
                        nIndex = nUnTestedIdx;
                        errMsg = string.Format("第{0}行有未测试项！", nIndex);
                        return false;
                    }
                }
                nIndex = nUnTestedIdx;
                return true;
            }
        }

        /// <summary>
        /// 是否测试过的全部合格
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <returns>是否全部合格</returns>
        public bool GetAllTestedPassed(ref string errMsg)
        {
            //errMsg="测试数据有不合格项！"
            lock (lockObj)
            {
                foreach (MESTestInfo info in allTestInfo)
                {
                    if (info.Tested && (!info.Pass))
                    {
                        errMsg = "测试数据有不合格项！";
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// 写归零数据
        /// </summary>
        /// <param name="strFilePath">归零数据文件路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>操作是否成功</returns>
        public bool RecordRefData(string strFilePath, ref string errMsg)
        {
            try
            {
                List<MESTestInfo> infoArr;
                lock (lockObj)
                {
                    infoArr = GetAllTestInfo();
                }
                string strWrite = "";
                strWrite += productInfo.TemplateID + "\n";
                foreach (MESTestInfo info in infoArr)
                {
                    strWrite += string.Format("{0:0.000},{1:0.000},{2},{3},{4:0.000},{5:0.000}\n", info.WLLeft, info.WLRight, info.PortNameForUser,info.TestParam.GetMESTemplateKeywords(), info.ILRef, info.RLRef);
                }
                CommonFunction.WriteFile(strFilePath, strWrite);
                return true;
            }
            catch (Exception ex)
            {
                errMsg = "RecordRefData 出错：" + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 读取归零数据
        /// </summary>
        /// <param name="strFilePath">归零数据文件路径</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>操作是否成功</returns>
        public bool ReadRefData(string strFilePath, ref string errMsg)
        {
            try
            {
                if (!File.Exists(strFilePath))
                {
                    errMsg = "归零文件不存在！";
                    return false;
                }
                StreamReader sr = new StreamReader(strFilePath, Encoding.Default);
                string line;
                List<string> refList = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    refList.Add(line.ToString());
                }
                sr.Close();
                sr = null;
                if (refList.Count == 0)
                    return false;
                //模板不对应
                if (productInfo.TemplateID != refList[0])
                {
                    errMsg = "当前模板与前一模板不一致，请重新归零！";
                    return false;
                }
                //归零数据不完整
                if (allTestInfo.Count != (refList.Count - 1))
                {
                    errMsg = "归零数据不完整！";
                    return false;
                }
                for (int i = 0; i < refList.Count - 1; i++)
                {
                    string[] strRef = refList[i + 1].Split(',');
                    if (strRef.Length < 5)
                        return false;
                    if (allTestInfo[i].WLLeft.CompareTo(Convert.ToDouble(strRef[0])) != 0)
                        return false;
                    if (allTestInfo[i].WLRight.CompareTo(Convert.ToDouble(strRef[1])) != 0)
                        return false;
                    if (allTestInfo[i].PortNameForUser != strRef[2])
                        return false;
                    if(allTestInfo[i].TestParam.GetMESTemplateKeywords()== strRef[3])
                    allTestInfo[i].ILRef = Convert.ToDouble(strRef[4]);
                    allTestInfo[i].RLRef = Convert.ToDouble(strRef[5]);
                }
                return true;
            }
            catch(Exception ex)
            {
                errMsg = "ReadRefData 出错：" + ex.Message;
                return false;
            }
            
        }

        /// <summary>
        /// 将数据保存到无纸化
        /// </summary>
        /// <param name="strSN">产品SN号</param>
        /// <param name="strUrl">webservice路径</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="bLighted">是否照过光</param>
        /// <param name="rawdataType">rawdata类型，如果不需要保存rawdata，则为默认的default</param>
        /// <returns>错误码:1--SN号有误，2--程序异常，3--上传出错</returns>
        public int SaveDataToAMTSByPort(string strSN, string strUrl, ref string errMsg, bool bLighted = false, MESRawdataType rawdataType = MESRawdataType.Default, List<AMTSRawdata> allRawdatas = null)
        {
            string strErr = "";
            errMsg = strErr;
            if (productInfo.SN != strSN)
            {
                errMsg = "SN号有误，请检查！";
                return 1;
            }
            if (allTestInfo.Count == 0)
                return 0;
            string strTmpt = "";
            string strPreTmpt = "";
            string strPrePort = "";
            string strSave = "";
            MESParamRule preRule = MESParamRule.Default;
            try
            {
                for (int i = 0; i < allTestInfo.Count; i++)
                {
                    MESTestInfo info = allTestInfo[i];
                    //照光后--低温  照光前--常温
                    if ((testProcess == MESTestProcess.Adjust || testProcess == MESTestProcess.PreAdjust) && bLighted)
                    {
                        strTmpt = "1";
                    }
                    else
                    {
                        //0--常温  1--低温  2--高温
                        strTmpt = info.SaveTemperature;
                    }
                    if (strPreTmpt != strTmpt|| strPrePort != info.PortNameForAMTS)
                    {
                        strSave = "<AMTS>";
                        strSave += "<SN>" + productInfo.SN + "</SN>";
                        strSave += "<TT>" + testType.GetMESSaveDataKeywords() + "</TT>";
                        strSave += "<TEMPLET>" + productInfo.TemplateID + "</TEMPLET>";
                        strSave += "<VER>" + productInfo.Version + "</VER>";
                        strSave += "<PN>" + productInfo.ProductPN + "</PN>";
                        strSave += "<SPEC>" + productInfo.SpecNO + "</SPEC>";
                        strSave += "<USER>" + userID + "</USER>";
                        strSave += "<COMPUTER>" + workStationID + "</COMPUTER>";
                        strSave += "<DN>" + productInfo.DeviceNO + "</DN>";
                        if (i == 0)
                        {
                            strSave += "<START>" + openTemplateTime + "</START>";
                        }
                        strSave += "<DATE>" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</DATE>";
                        strSave += "<SOFTWARE>" + templateType.GetMESSaveDataKeywords() + testProcess.GetMESSaveDataKeywords() + "</SOFTWARE>";
                        strSave += "<TEMP VALUE=\"" + strTmpt + "\">";
                        strSave += "<PORT VALUE=\"" + info.PortNameForAMTS + "\">";
                        if (rawdataType != MESRawdataType.Default && allRawdatas != null)
                        {
                            foreach (AMTSRawdata rawdata in allRawdatas)
                            {
                                if (rawdata.PortName.ToUpper() == info.PortNameForUser.ToUpper() &&
                                    rawdata.Temperature == info.Temperature)
                                {
                                    strSave += "<" + rawdataType + ">";
                                    strSave += rawdata.Rawdata;
                                    strSave += "</" + rawdataType + ">";
                                }
                            }
                        }
                    }
                    strPrePort = info.PortNameForAMTS;
                    strPreTmpt = strTmpt;
                    //WL DB 有何差别
                    if (preRule != info.ParamType)
                    {
                        if (info.ParamType == MESParamRule.DB)
                        {
                            strSave += "<DB VALUE=\"" + info.SettingValue.ToString() + "\">";
                        }
                        else if (info.ParamType == MESParamRule.WL)
                        {
                            strSave += "<WL LEFT=\"" + info.WLLeft + "\" RIGHT=\"" + info.WLRight + "\">";
                        }
                        else if (info.ParamType == MESParamRule.EX)
                        {
                            strSave += "<EX>";
                        }
                    }
                    preRule = info.ParamType;
                    if (info.CurValue.CompareTo(CommonFunction.GetDefaultValue()) != 0 && info.CurValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    {
                        if (info.ParamType == MESParamRule.EX)
                            strSave += "<KEY NAME=\"" + info.ExParamName + "\">" + info.CurValue.ToString("#0.000") + "</KEY>";
                        else
                            strSave += "<" + info.TestParam.GetMESSaveDataKeywords() + ">" + info.CurValue.ToString("#0.000") + "</" + info.TestParam.GetMESSaveDataKeywords() + ">";
                    }
                    //下一个测试项是否是相同温度、相同端口、相同类型参数
                    if (i < allTestInfo.Count - 1)
                    {
                        MESTestInfo nextInfo = allTestInfo[i + 1];
                        if ((info.ParamType != nextInfo.ParamType)
                            || (info.PortNameForAMTS != nextInfo.PortNameForAMTS)
                            || (info.Temperature != nextInfo.Temperature))
                        {
                            if (info.ParamType == MESParamRule.DB)
                            {
                                strSave += "</DB>";
                            }
                            else if (info.ParamType == MESParamRule.WL)
                            {
                                strSave += "</WL>";
                            }
                            else if (info.ParamType == MESParamRule.EX)
                            {
                                strSave += "</EX>";
                            }
                            preRule = MESParamRule.Default;
                        }
                        if ((info.PortNameForAMTS != nextInfo.PortNameForAMTS) || (info.Temperature != nextInfo.Temperature))
                        {
                            strSave += "</PORT>";
                            strSave += "</TEMP>";
                            strSave += "</AMTS>";
                            string[] args = new string[1];
                            args[0] = strSave;
                            string dataFileName = productInfo.SN + "_" + strTmpt + ".ini";
                            CommonFunction.WriteFile(System.Environment.CurrentDirectory + "\\data\\" + dataFileName, strSave);
                            object result = WebServiceHelper.InvokeWebService(strUrl, "Upload", args);
                            errMsg = result.ToString();
                            if (errMsg.Length > 0)
                                return 3;
                        }
                    }
                    else
                    {
                        if (info.ParamType == MESParamRule.DB)
                        {
                            strSave += "</DB>";
                        }
                        else if (info.ParamType == MESParamRule.WL)
                        {
                            strSave += "</WL>";
                        }
                        else if (info.ParamType == MESParamRule.EX)
                        {
                            strSave += "</EX>";
                        }
                        strSave += "</PORT>";
                        strSave += "</TEMP>";
                        strSave += "</AMTS>";
                        string dataFileName = productInfo.SN + "_" + strTmpt + ".ini";
                        string[] args = new string[1];
                        args[0] = strSave;
                        CommonFunction.WriteFile(System.Environment.CurrentDirectory + "\\data\\" + dataFileName, strSave);
                        object result = WebServiceHelper.InvokeWebService(strUrl, "Upload", args);

                        errMsg = result.ToString();
                        if (errMsg.Length > 0)
                            return 3;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return 2;
            }
            return 0;
        }


        /// <summary>
        /// 将数据保存到无纸化
        /// </summary>
        /// <param name="strSN">产品SN号</param>
        /// <param name="strUrl">webservice路径</param>
        /// <param name="tmpt">需要保存的温度</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="bLighted">是否照过光</param>
        /// <param name="rawdataType">rawdata类型，如果不需要保存rawdata，则为默认的default</param>
        /// <returns>错误码:1--SN号有误，2--程序异常，3--上传出错</returns>
        public int SaveDataToAMTSByTmpt(string strSN, string strUrl, int tmpt,ref string errMsg, bool bLighted = false, MESRawdataType rawdataType = MESRawdataType.Default, List<AMTSRawdata> allRawdatas = null)
        {
            string strErr = "";
            errMsg = strErr;
            if (productInfo.SN != strSN)
            {
                errMsg = "SN号有误，请检查！";
                return 1;
            }
            if (allTestInfo.Count == 0)
                return 0;
            string strTmpt = "";
            string strPreTmpt = "";
            string strPrePort = "";
            string strSave = "";
            MESParamRule preRule = MESParamRule.Default;
            try
            {
                for (int i = 0; i < allTestInfo.Count; i++)
                {
                    MESTestInfo info = allTestInfo[i];
                    //照光后--低温  照光前--常温
                    if ((testProcess == MESTestProcess.Adjust || testProcess == MESTestProcess.PreAdjust) && bLighted)
                    {
                        strTmpt = "1";
                    }
                    else
                    {
                        //0--常温  1--低温  2--高温
                        strTmpt = info.SaveTemperature;

                    }
                    if (strTmpt != tmpt.ToString())
                        continue;
                    if (strPreTmpt != strTmpt)
                    {
                        strSave = "<AMTS>" + "\r\n";
                        strSave += "<SN>" + productInfo.SN + "</SN>" + "\r\n";
                        strSave += "<TT>" + testType.GetMESSaveDataKeywords() + "</TT>" + "\r\n";
                        strSave += "<TEMPLET>" + productInfo.TemplateID + "</TEMPLET>" + "\r\n";
                        strSave += "<VER>" + productInfo.Version + "</VER>" + "\r\n";
                        strSave += "<PN>" + productInfo.ProductPN + "</PN>" + "\r\n";
                        strSave += "<SPEC>" + productInfo.SpecNO + "</SPEC>" + "\r\n";
                        strSave += "<USER>" + userID + "</USER>" + "\r\n";
                        strSave += "<COMPUTER>" + workStationID + "</COMPUTER>" + "\r\n";
                        strSave += "<DN>" + productInfo.DeviceNO + "</DN>" + "\r\n";
                        if (i == 0)
                        {
                            strSave += "<START>" + openTemplateTime + "</START>" + "\r\n";
                        }
                        strSave += "<DATE>" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</DATE>" + "\r\n";
                        strSave += "<SOFTWARE>" + templateType.GetMESSaveDataKeywords() + testProcess.GetMESSaveDataKeywords() + "</SOFTWARE>" + "\r\n";
                        strSave += "<TEMP VALUE=\"" + strTmpt + "\">" + "\r\n";
                    }
                    if ((strPrePort != info.PortNameForAMTS) || (strPreTmpt != strTmpt))
                    {
                        strSave += "<PORT VALUE=\"" + info.PortNameForAMTS + "\">" + "\r\n";
                        if (rawdataType != MESRawdataType.Default && allRawdatas != null)
                        {
                            foreach (AMTSRawdata rawdata in allRawdatas)
                            {
                                if (rawdata.PortName.ToUpper() == info.PortNameForUser.ToUpper() &&
                                    rawdata.Temperature == info.Temperature)
                                {
                                    strSave += "<" + rawdataType + ">";
                                    strSave += rawdata.Rawdata;
                                    strSave += "</" + rawdataType + ">";
                                }
                            }
                        }
                    }
                    strPrePort = info.PortNameForAMTS;
                    strPreTmpt = strTmpt;
                    //WL DB 有何差别
                    if (preRule != info.ParamType)
                    {
                        if (info.ParamType == MESParamRule.DB)
                        {
                            strSave += "<DB VALUE=\"" + info.SettingValue.ToString() + "\">" + "\r\n";
                        }
                        else if (info.ParamType == MESParamRule.WL)
                        {
                            strSave += "<WL LEFT=\"" + info.WLLeft + "\" RIGHT=\"" + info.WLRight + "\">" + "\r\n";
                        }
                        else if (info.ParamType == MESParamRule.EX)
                        {
                            strSave += "<EX>" + "\r\n";
                        }
                    }
                    preRule = info.ParamType;
                    if (info.CurValue.CompareTo(CommonFunction.GetDefaultValue()) != 0 && info.CurValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    {
                        if (info.ParamType == MESParamRule.EX)
                            strSave += "<KEY NAME=\"" + info.ExParamName + "\">" + info.CurValue.ToString("#0.000") + "</KEY>" + "\r\n";
                        else
                            strSave += "<" + info.TestParam.GetMESSaveDataKeywords() + ">" + info.CurValue.ToString("#0.000") + "</" + info.TestParam.GetMESSaveDataKeywords() + ">" + "\r\n";
                    }
                    //下一个测试项是否是相同温度、相同端口、相同类型参数
                    if (i < allTestInfo.Count - 1)
                    {
                        MESTestInfo nextInfo = allTestInfo[i + 1];
                        if ((info.ParamType != nextInfo.ParamType)
                            || (info.PortNameForAMTS != nextInfo.PortNameForAMTS)
                            || (info.Temperature != nextInfo.Temperature))
                        {
                            if (info.ParamType == MESParamRule.DB)
                            {
                                strSave += "</DB>" + "\r\n";
                            }
                            else if (info.ParamType == MESParamRule.WL)
                            {
                                strSave += "</WL>" + "\r\n";
                            }
                            else if (info.ParamType == MESParamRule.EX)
                            {
                                strSave += "</EX>" + "\r\n";
                            }
                            preRule = MESParamRule.Default;
                        }
                        if ((info.PortNameForAMTS != nextInfo.PortNameForAMTS) || (info.Temperature != nextInfo.Temperature))
                        {
                            strSave += "</PORT>" + "\r\n";
                        }
                        if (info.Temperature != nextInfo.Temperature)
                        {
                            strSave += "</TEMP>" + "\r\n";
                            strSave += "</AMTS>" + "\r\n";
                            string[] args = new string[1];
                            args[0] = strSave;
                            string dataFileName = productInfo.SN + "_" + strTmpt + ".ini";
                            CommonFunction.WriteFile(System.Environment.CurrentDirectory + "\\data\\" + dataFileName, strSave);
                            object result = WebServiceHelper.InvokeWebService(strUrl, "Upload", args);
                            /*errMsg = result.ToString();
                            if (errMsg.Length > 0)
                                return 3;*/
                        }
                    }
                    else
                    {
                        if (info.ParamType == MESParamRule.DB)
                        {
                            strSave += "</DB>" + "\r\n";
                        }
                        else if (info.ParamType == MESParamRule.WL)
                        {
                            strSave += "</WL>" + "\r\n";
                        }
                        else if (info.ParamType == MESParamRule.EX)
                        {
                            strSave += "</EX>" + "\r\n";
                        }
                        strSave += "</PORT>" + "\r\n";
                        strSave += "</TEMP>" + "\r\n";
                        strSave += "</AMTS>" + "\r\n";
                        string dataFileName = productInfo.SN + "_" + strTmpt + ".ini";
                        string[] args = new string[1];
                        args[0] = strSave;
                        CommonFunction.WriteFile(System.Environment.CurrentDirectory + "\\data\\" + dataFileName, strSave);
                        object result = WebServiceHelper.InvokeWebService(strUrl, "Upload", args);


                        errMsg = result.ToString();
                        if (errMsg.Length > 0)
                            return 3;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return 2;
            }
            return 0;
        }

        /// <summary>
        /// 将数据保存到无纸化
        /// </summary>
        /// <param name="strSN">产品SN号</param>
        /// <param name="strUrl">webservice路径</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="bLighted">是否照过光</param>
        /// <param name="rawdataType">rawdata类型，如果不需要保存rawdata，则为默认的default</param>
        /// <returns>错误码:1--SN号有误，2--程序异常，3--上传出错</returns>
        public int SaveDataToAMTS(string strSN, string strUrl, ref string errMsg, bool bLighted = false, MESRawdataType rawdataType = MESRawdataType.Default, List<AMTSRawdata> allRawdatas=null)
        {
            string strErr = "";
            errMsg = strErr;
            if (productInfo.SN != strSN)
            {
                errMsg = "SN号有误，请检查！";
                return 1;
            }
            if (allTestInfo.Count == 0)
                return 0;
            string strTmpt = "";
            string strPreTmpt = "";
            string strPrePort = "";
            string strSave = "";
            MESParamRule preRule = MESParamRule.Default;
            try
            {
                for (int i = 0; i < allTestInfo.Count; i++)
                {
                    MESTestInfo info = allTestInfo[i];
                    //照光后--低温  照光前--常温
                    if ((testProcess == MESTestProcess.Adjust || testProcess == MESTestProcess.PreAdjust) && bLighted)
                    {
                        strTmpt = "1";
                    }
                    else
                    {
                        //0--常温  1--低温  2--高温
                        strTmpt = info.SaveTemperature;
                        
                    }
                    if (strPreTmpt != strTmpt)
                    {
                        strSave = "<AMTS>"+"\r\n";
                        strSave += "<SN>" + productInfo.SN + "</SN>" + "\r\n";
                        strSave += "<TT>" + testType.GetMESSaveDataKeywords() + "</TT>" + "\r\n";
                        strSave += "<TEMPLET>" + productInfo.TemplateID + "</TEMPLET>" + "\r\n";
                        strSave += "<VER>" + productInfo.Version + "</VER>" + "\r\n";
                        strSave += "<PN>" + productInfo.ProductPN + "</PN>" + "\r\n";
                        strSave += "<SPEC>" + productInfo.SpecNO + "</SPEC>" + "\r\n";
                        strSave += "<USER>" + userID + "</USER>" + "\r\n";
                        strSave += "<COMPUTER>" + workStationID + "</COMPUTER>" + "\r\n";
                        strSave += "<DN>" + productInfo.DeviceNO + "</DN>" + "\r\n";
                        if (i == 0)
                        {
                            strSave += "<START>" + openTemplateTime + "</START>" + "\r\n";
                        }
                        strSave += "<DATE>" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</DATE>" + "\r\n";
                        strSave += "<SOFTWARE>" + templateType.GetMESSaveDataKeywords() + testProcess.GetMESSaveDataKeywords() + "</SOFTWARE>" + "\r\n";
                        strSave += "<TEMP VALUE=\"" + strTmpt + "\">" + "\r\n";
                    }
                    if ((strPrePort != info.PortNameForAMTS) || (strPreTmpt != strTmpt))
                    {
                        strSave += "<PORT VALUE=\"" + info.PortNameForAMTS + "\">" + "\r\n";
                        if(rawdataType!= MESRawdataType.Default&&allRawdatas!=null)
                        {
                            foreach(AMTSRawdata rawdata in allRawdatas)
                            {
                                if(rawdata.PortName.ToUpper()==info.PortNameForUser.ToUpper()&&
                                    rawdata.Temperature==info.Temperature)
                                {
                                    strSave += "<" + rawdataType.GetMESSaveDataKeywords() + ">";
                                    strSave += rawdata.Rawdata;
                                    strSave += "</" + rawdataType.GetMESSaveDataKeywords() + ">";
                                }
                            }
                        }
                    }
                    strPrePort = info.PortNameForAMTS;
                    strPreTmpt = strTmpt;
                    //WL DB 有何差别
                    if (preRule != info.ParamType)
                    {
                        if (info.ParamType == MESParamRule.DB)
                        {
                            strSave += "<DB VALUE=\"" + info.SettingValue.ToString() + "\">" + "\r\n";
                        }
                        else if (info.ParamType == MESParamRule.WL)
                        {
                            strSave += "<WL LEFT=\"" + info.WLLeft + "\" RIGHT=\"" + info.WLRight + "\">" + "\r\n";
                        }
                        else if(info.ParamType==MESParamRule.EX)
                        {
                            strSave += "<EX>" + "\r\n";
                        }
                    }
                    preRule = info.ParamType;
                    if (info.CurValue.CompareTo(CommonFunction.GetDefaultValue()) != 0 && info.CurValue.CompareTo(CommonFunction.GetFormatDefaultValue()) != 0)
                    {
                        if (info.ParamType == MESParamRule.EX)
                            strSave += "<KEY NAME=\"" + info.ExParamName + "\">" + info.CurValue.ToString("#0.000") + "</KEY>" + "\r\n";
                        else
                            strSave += "<" + info.TestParam.GetMESSaveDataKeywords() + ">" + info.CurValue.ToString("#0.000") + "</" + info.TestParam.GetMESSaveDataKeywords() + ">" + "\r\n";
                    }
                    //下一个测试项是否是相同温度、相同端口、相同类型参数
                    if (i < allTestInfo.Count - 1)
                    {
                        MESTestInfo nextInfo = allTestInfo[i + 1];
                        if ((info.ParamType != nextInfo.ParamType)
                            || (info.PortNameForAMTS != nextInfo.PortNameForAMTS)
                            || (info.Temperature != nextInfo.Temperature))
                        {
                            if (info.ParamType == MESParamRule.DB)
                            {
                                strSave += "</DB>" + "\r\n";
                            }
                            else if (info.ParamType == MESParamRule.WL)
                            {
                                strSave += "</WL>" + "\r\n";
                            }
                            else if (info.ParamType == MESParamRule.EX)
                            {
                                strSave += "</EX>" + "\r\n";
                            }
                            preRule = MESParamRule.Default;
                        }
                        if ((info.PortNameForAMTS != nextInfo.PortNameForAMTS) || (info.Temperature != nextInfo.Temperature))
                        {
                            strSave += "</PORT>" + "\r\n";
                        }
                        if (info.Temperature != nextInfo.Temperature)
                        {
                            strSave += "</TEMP>" + "\r\n";
                            strSave += "</AMTS>" + "\r\n";
                            string[] args = new string[1];
                            args[0] = strSave;
                            string dataFileName = productInfo.SN + "_" + strTmpt + ".ini";
                            CommonFunction.WriteFile(System.Environment.CurrentDirectory + "\\data\\" + dataFileName, strSave);
                            object result = WebServiceHelper.InvokeWebService(strUrl, "Upload", args);
                            /*errMsg = result.ToString();
                            if (errMsg.Length > 0)
                                return 3;*/
                        }
                    }
                    else
                    {
                        if (info.ParamType == MESParamRule.DB)
                        {
                            strSave += "</DB>" + "\r\n";
                        }
                        else if (info.ParamType == MESParamRule.WL)
                        {
                            strSave += "</WL>" + "\r\n";
                        }
                        else if (info.ParamType == MESParamRule.EX)
                        {
                            strSave += "</EX>" + "\r\n";
                        }
                        strSave += "</PORT>" + "\r\n";
                        strSave += "</TEMP>" + "\r\n";
                        strSave += "</AMTS>" + "\r\n";
                        string dataFileName = productInfo.SN + "_" + strTmpt + ".ini";
                        string[] args = new string[1];
                        args[0] = strSave;
                        CommonFunction.WriteFile(System.Environment.CurrentDirectory + "\\data\\" + dataFileName, strSave);
                        object result = WebServiceHelper.InvokeWebService(strUrl, "Upload", args);


                        errMsg = result.ToString();
                        if (errMsg.Length > 0)
                            return 3;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return 2;
            }
            return 0;
        }


        /// <summary>
        /// 测试结果是否合格
        /// </summary>
        /// <param name="nIndex">需要判断行序号</param>
        /// <returns>是否合格</returns>
        private bool CheckPassOrFail(int nIndex, ref string errMsg)
        {
            lock (lockObj)
            {
                if (allTestInfo.Count > nIndex)
                {
                    return CheckPassOrFail(GetTestInfoByIndex(nIndex, ref errMsg));
                }
            }
            return true;
        }

        /// <summary>
        /// 判断测试结果是否合格
        /// </summary>
        /// <param name="info">测试相关信息</param>
        /// <returns>是否合格</returns>
        private bool CheckPassOrFail(MESTestInfo info,bool isStr=false)
        {
            bool bPass = true;
            //如果为空行，则不需要判断是否合格
            if (info.TestParam != MESParam.Default)
            {
                double dTestValue = info.CurValue;

                bPass = MeetCriterion(dTestValue, info.Criterion)
                    & MeetCriterion(dTestValue, info.Criterion1);

            }
            info.Pass = bPass;
            return bPass;
        }

        /// <summary>
        /// 无纸化模板判断是否在合格范围内
        /// </summary>
        /// <param name="result">需要评判的值</param>
        /// <param name="criterion">上下限</param>
        /// <param name="isShift">是否为shift</param>
        /// <returns></returns>
        private bool MeetCriterion(double result, string criterion, bool isShift = false)
        {
            //设置的上下限值，
            //正负号决定了是大于或者小于；
            //数值是否大于9999或者999，决定了数值是正负
            //如果为<0.0001,大于0的上下限值，则不需要评判，即为设置时的默认值
            bool isNegative = false;
            if (criterion.Substring(0,1) == "-")
                isNegative = true;
            double dCriterion = Convert.ToDouble(criterion);
            if (dCriterion < 0.001 && dCriterion > Double.Epsilon)
                return true;
            double limit = 0;
            //如果值大于9999（shift大于1000），则为负值（绝对值/1000000），
            if ((Math.Abs(dCriterion)) > 9999 || ((Math.Abs(dCriterion) >= 1000) && isShift))
            {
                limit = -Math.Abs(dCriterion) / 1000000.0;
            }
            else
                limit = Math.Abs(dCriterion);

            //正号为>=,负号为<=
            if (!isNegative)
                return result >= limit;
            else
                return result <= limit;
        }
    }
}
