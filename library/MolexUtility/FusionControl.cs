using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using USLTASLibrary;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Runtime.InteropServices;

namespace MolexUtility
{
    [Serializable]
    public class FusionControl
    {
        private static readonly object Crc32LoadLock = new object();
        private static bool crc32LibLoaded;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("CRC32Lib.dll", EntryPoint = "GetCRC32", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 GetCRC32(byte[] content, long len);

        static FusionControl()
        {
            EnsureCrc32LibLoaded();
        }

        /// <summary>
        /// 原生 CRC32Lib.dll 默认只从 exe 目录加载；产线常放在 module\ 或 common\，需显式 LoadLibrary。
        /// </summary>
        private static void EnsureCrc32LibLoaded()
        {
            if (crc32LibLoaded)
                return;

            lock (Crc32LoadLock)
            {
                if (crc32LibLoaded)
                    return;

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidates = new[]
                {
                    Path.Combine(baseDir, "CRC32Lib.dll"),
                    Path.Combine(baseDir, "module", "CRC32Lib.dll"),
                    Path.Combine(baseDir, "common", "CRC32Lib.dll"),
                };

                foreach (string path in candidates)
                {
                    if (!File.Exists(path))
                        continue;
                    if (LoadLibrary(path) != IntPtr.Zero)
                    {
                        crc32LibLoaded = true;
                        return;
                    }
                }

                throw new DllNotFoundException(
                    "无法加载 CRC32Lib.dll。请将 CRC32Lib.dll 放在程序目录、" +
                    "module\\ 或 common\\ 下（当前基目录: " + baseDir + "）。");
            }
        }

        public string ProductSN="";
        /// <summary>
        /// 防止线程访问冲突，用于互斥
        /// </summary>
        private object lockObj = new object();
        public List<MESTestInfo> AllTestInfo = new List<MESTestInfo>();
        public DocrevRecordInfo docrevInfo = new DocrevRecordInfo();
        public RecipeRecordInfo recipeInfo = new RecipeRecordInfo();
        public MFGRecordInfo MFGInfo = new MFGRecordInfo();
        public List<CFGRecordInfo> CFGInfo = new List<CFGRecordInfo>();
        public List<MISCRecordInfo> MISCInfo = new List<MISCRecordInfo>();
        public MESProductInfo productInfo = new MESProductInfo();
        private List<ScanData> scanReference = new List<ScanData>();
        private List<ScanData> scanRawData = new List<ScanData>();
        private List<ScanData> gffOriginal = new List<ScanData>();
        public List<FusionEnvironmentInfo> EnvironmentInfo = new List<FusionEnvironmentInfo>();
        public List<FusionObjectInfo> ObjectInfos = new List<FusionObjectInfo>();
        public List<FusionPortInfo> PortInfo = new List<FusionPortInfo>();
        public List<FusionConditionInfo> ConditionInfo = new List<FusionConditionInfo>();

        private DateTime loadTemplateTime = System.DateTime.Today.ToLocalTime();

        private string userRec = "";

        private string stationRec = "";

        private string templateConten = "";

        public double[] TmptArray()
        {
            List<double> enTmpt = new List<double>();
            foreach(FusionEnvironmentInfo env in EnvironmentInfo)
            {
                if(env.Name== "TEMP")
                {
                    if (!env.Value.Contains("~"))
                    {
                        enTmpt.Add(Convert.ToDouble(env.Value));
                    }
                }
            }
            return enTmpt.ToArray();
        }

        public static bool SetToSpecMode(string user, string pwd, string mesMode, ref string errMsg)
        {
            try
            {
                USLTASLibraryInterface tas = new USLTASLibraryInterface();
                if(!tas.SetSystemToSpecialMode(user, pwd, mesMode, ref errMsg))
                {
                    errMsg = "设置离线模式 出错：" + errMsg;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg = "设置离线模式 出错(Exception)：" + ex.Message;
                return false;
            }
        }

        //USLTASLibraryInterface tas = new USLTASLibraryInterface();

        public string GetSNDir(string proFamilyPath,ref string errMsg)
        {
            
            USLTASLibraryInterface tas = new USLTASLibraryInterface();
            string snPath = "";
            if(tas.CreateSNDataRootDir(proFamilyPath, ProductSN, ref snPath, ref errMsg))
            {
                return snPath;
            }
            else
            {
                return "";
            }
            //return ".//data";
        }

        private static void LogOpenTemplateStep(string step, string sn)
        {
            try
            {
                string logDir = Path.Combine(Environment.CurrentDirectory, "data");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} SN={1} {2}\r\n",
                    DateTime.Now, sn ?? "", step ?? "");
                File.AppendAllText(Path.Combine(logDir, "open_template.log"), line, Encoding.UTF8);
            }
            catch
            {
            }
        }

        public string OpenTemplate(string SN, string MESProcess, string User, string Freecheck, bool bShowData, string computer,List<string> sptProcess, out string strTemplateName,out string errMsg)
        {
            errMsg = "";
            strTemplateName = "";
            try
            {
                LogOpenTemplateStep("OpenTemplate begin", SN);
                ProductSN = SN;
                if (SN == "")
                {
                    errMsg = "请输入产品号！";
                    return "";
                }
                userRec = User;
                //rjf test
                USLTASLibraryInterface tas = new USLTASLibraryInterface();
                LogOpenTemplateStep("SetEmployeeAccount", SN);
                if(!tas.SetEmployeeAccount(User,ref errMsg))
                {
                    errMsg = string.Format("TAS库设置员工账号出错：{0}", errMsg);
                    return "";
                }
                string stationName = "";

                if (stationRec == "")
                {
                    //rjf test
                    //computer = "ITPC180117";
                    LogOpenTemplateStep("GetStationName " + computer, SN);
                    if (!tas.GetStationName(computer, ref stationRec, ref stationName, ref errMsg))
                    {
                        errMsg = "TAS获取工位信息出错";
                        return "";
                    }
                }

                string proPN = "";
                string proSpec = "";
                string proWO = "";
                string proCurProcess = "";
                string proCurStatus = "";
                LogOpenTemplateStep("GetProductKeyInfo", SN);
                if(!tas.GetProductKeyInfo(SN,ref proPN,ref proSpec,ref proWO,ref proCurProcess,ref proCurStatus,ref errMsg))
                {
                    errMsg = "获取产品关键信息出错"+ errMsg;
                    return "";
                }

                if(proSpec=="")
                {
                    errMsg = "获取产品关键信息出错,Spec为空";
                    return "";
                }
                
                if (proCurProcess != MESProcess)
                {
                    errMsg = string.Format("产品当前工序:{0}，不在工序：{1}，工序不对应！", proCurProcess, MESProcess);
                    return "";
                }

                //如果有传入内部工序，则用内部工序判断，否则用OPC工序
                LogOpenTemplateStep("GetTestProcessCode", SN);
                string omsProcess = tas.GetTestProcessCode(proPN, proCurProcess, ref errMsg);
                bool isSuport = false;
                if(sptProcess.Count>0)
                {                    
                    foreach (string proc in sptProcess)
                    {
                        if (omsProcess.ToUpper() == proc.ToUpper())
                        {
                            isSuport = true;
                        }
                    }
                }
                else
                {
                    if(proCurProcess== MESProcess)
                    {
                        isSuport = true;
                    }
                }
                                
                if(isSuport==false)
                {
                    errMsg = string.Format("不支持该工序测试：{0}/{1}", proCurProcess, omsProcess);
                    return "";
                }

                string skipMoveInPath = Path.Combine(Environment.CurrentDirectory, "set", "OpenTemplateSkipMoveIn.txt");
                if (!File.Exists(skipMoveInPath))
                {
                    LogOpenTemplateStep("TriggerProcessMoveIn", SN);
                    if (!tas.TriggerProcessMoveIn(SN, MESProcess, stationRec, ref errMsg))
                    {
                        errMsg = "TAS Move In出错:"+ errMsg;
                        return "";
                    }
                }
                else
                {
                    LogOpenTemplateStep("Skip MoveIn (set\\OpenTemplateSkipMoveIn.txt)", SN);
                }
                LogOpenTemplateStep("GetProdTestTemplate", SN);
                templateConten = tas.GetProdTestTemplate(SN, MESProcess, User, Freecheck, bShowData, ref strTemplateName, ref errMsg);
                
                
                if (templateConten == "")
                {
                     errMsg="TAS 获取模板出错：" + errMsg;
                    return "";
                }
                //rjf test
                //从文件里读取模板
                /*string result = "";
                StreamReader readsr = new StreamReader("C:\\Users\\jruan01\\OneDrive - kochind.com\\Documents\\source code fusion\\OCITS_fusion\\1831760166@Interleaver-ITL-终测CD@8048-03@WONA.xml");
                strTemplateName = "1831760166@Interleaver-ITL-终测CD@8048-03@WONA.xml";
                templateConten = readsr.ReadToEnd();
                readsr.Close();*/
                CommonFunction.WriteFile(Environment.CurrentDirectory + "\\temple\\temp.xml", templateConten);

                loadTemplateTime = System.DateTime.Now;
                AllTestInfo.Clear();
                docrevInfo = new DocrevRecordInfo();
                recipeInfo = new RecipeRecordInfo();
                MFGInfo = new MFGRecordInfo();
                CFGInfo.Clear();
                MISCInfo.Clear();
                productInfo = new MESProductInfo();

                LogOpenTemplateStep("ParserTemplate", SN);
                if (!ParserTemplate(templateConten, out errMsg))
                {
                    return "";
                }
                LogOpenTemplateStep("OpenTemplate success", SN);
                return templateConten;
            }
            catch (Exception ex)
            {
                errMsg = "打开模板 出错(Exception)：" + ex.Message;
                LogOpenTemplateStep("OpenTemplate exception: " + ex.Message, SN);
                return "";
            }
        }

        public string LoadTestData(string SN, string MESProcess, string User, out string errMsg)
        {
            errMsg = "";
            try
            {
                ProductSN = SN;
                if (SN == "")
                {
                    errMsg = "请输入产品号！";
                    return "";
                }
                userRec = User;
                //rjf test
                USLTASLibraryInterface tas = new USLTASLibraryInterface();
                string strTemplateName = "";
                templateConten = tas.GetProdTestTemplate(SN, MESProcess, User, "", true, ref strTemplateName, ref errMsg);

                if (templateConten == "")
                {
                    errMsg = "TAS 读取数据出错：" + errMsg;
                    return "";
                }
                //rjf test
                //从文件里读取模板
                string result = "";
                /*StreamReader readsr = new StreamReader("C:\\Users\\jruan01\\OneDrive - kochind.com\\Documents\\source code fusion\\OCITS_fusion\\1831760166@Interleaver-ITL-终测CD@8048-03@WONA.xml");
                strTemplateName = "1831760166@Interleaver-ITL-终测CD@8048-03@WONA.xml";
                templateConten = readsr.ReadToEnd();
                readsr.Close();*/
                CommonFunction.WriteFile(Environment.CurrentDirectory + "\\temple\\temp_data.xml", templateConten);

                loadTemplateTime = System.DateTime.Now;
                AllTestInfo.Clear();
                docrevInfo = new DocrevRecordInfo();
                recipeInfo = new RecipeRecordInfo();
                MFGInfo = new MFGRecordInfo();
                CFGInfo.Clear();
                MISCInfo.Clear();
                productInfo = new MESProductInfo();

                if (!ParserTemplateData(templateConten, out errMsg))
                {
                    return "";
                }
                return templateConten;
            }
            catch (Exception ex)
            {
                errMsg = "打开模板 出错(Exception)：" + ex.Message;
                return "";
            }
        }

        // TMS/MES 能力均来自 USL.TAS.dll 的 USLTASLibraryInterface（非 MolexUtility 自实现）。
        // 仓库根目录 ITasCommLib.cs 仅为平台接口说明，不必编入本工程；调用方式与 TriggerWorkStationVerify 相同。
        //
        // 打开模板前工位 Golden Sample 校验（TriggerWorkStationVerify）暂不启用，避免 UI 线程同步等 TAS 导致卡死。
        // 后续接入时请使用后台线程，并明确 mainInfo.Goldsample（工位/金样配置）与产品 SN 的判定规则。

        public static bool GoldsampleCheck(string goldSampleSN,string userID,string type, ref string errMsg)
        {
            USLTASLibraryInterface tas = new USLTASLibraryInterface();
            bool bCheckPass = tas.TriggerWorkStationVerify(goldSampleSN, userID, type, ref errMsg);
            return bCheckPass;
        }

        /// <summary>
        /// TMS GDS 金样产品 SN 标记（与 C++ strSN.Find("GDSM") 一致：含 GDSM 则跳过工位校验）。
        /// </summary>
        public const string GdsGoldenSampleSnMarker = "GDSM";

        /// <summary>
        /// 是否为 GDS 金样流程产品（无需 TriggerWorkStationVerify）。
        /// </summary>
        public static bool IsGdsGoldenSampleProduct(string productSn)
        {
            return !string.IsNullOrEmpty(productSn)
                && productSn.IndexOf(GdsGoldenSampleSnMarker, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 打开模板前是否需做工位 Golden Sample / 技能校验。
        /// </summary>
        public static bool ShouldVerifyWorkStationForOpenTemplate(string productSn)
        {
            return !IsGdsGoldenSampleProduct(productSn);
        }

        /// <summary>
        /// 输入 SN 是否为配置的金样产品 SN（与 stations/MIMS 中 Goldsample 一致，或含 GDSM 标记）。
        /// 供后续在打开模板前/后做工位校验时使用；当前打开模板流程不调用。
        /// </summary>
        public static bool IsGoldSampleProductSn(string productSn, string configuredGoldSample)
        {
            if (IsGdsGoldenSampleProduct(productSn))
                return true;
            if (string.IsNullOrWhiteSpace(productSn) || string.IsNullOrWhiteSpace(configuredGoldSample))
                return false;
            return productSn.Trim().Equals(configuredGoldSample.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 工位校验：非 GDSM 走 ITasCommLib.TriggerWorkStationVerify；GDSM 直接通过。
        /// 当前终测打开模板不调用；保留供后续金样/工位技能校验接入。
        /// </summary>
        public static bool TryVerifyWorkStationForOpenTemplate(string goldSampleWorkStationId, string productSn, string userID, string verifyType, ref string errMsg)
        {
            errMsg = "";
            if (!ShouldVerifyWorkStationForOpenTemplate(productSn))
            {
                CommonFunction.WriteLog(string.Format("[TMS GDS] SN={0} 跳过 TriggerWorkStationVerify。", productSn));
                return true;
            }
            return GoldsampleCheck(goldSampleWorkStationId, userID, verifyType, ref errMsg);
        }

        /// <summary>
        /// 归零完成后上传校准时间到 TMS（USL.TAS.dll → USLTASLibraryInterface.UploadTestSystemCailbrationTime）。
        /// </summary>
        public static bool UploadRefCalibrationTime(string userID, ref string errMsg)
        {
            errMsg = "";
            if (string.IsNullOrWhiteSpace(userID))
            {
                errMsg = "用户工号为空，无法上传归零时间。";
                return false;
            }

            try
            {
                USLTASLibraryInterface tas = new USLTASLibraryInterface();
                if (!tas.UploadTestSystemCailbrationTime(userID, ref errMsg))
                {
                    if (string.IsNullOrEmpty(errMsg))
                        errMsg = "UploadTestSystemCailbrationTime 返回失败。";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        public string GetOplinkProcess(string PN,string process,ref string errMsg)
        {
            USLTASLibraryInterface tas = new USLTASLibraryInterface();
            string oplinkProcess = tas.GetMESProcessCode(PN, process, ref errMsg);
            return oplinkProcess;
        }
        public bool UploadTestData(string fileName, out string errMsg)
        {
            errMsg = "";
            try
            {
                USLTASLibraryInterface tas = new USLTASLibraryInterface();
                if (!SaveDataToFile(fileName, out errMsg))
                {
                    return false;
                }
                
                string uploadRes = tas.UploadTestData(fileName);
                if (uploadRes != "")
                {
                    errMsg = "上传出错：" + uploadRes;
                    return false;
                }
                int nRes=tas.TriggerTestResultUpload(ProductSN, ref uploadRes);
                if(nRes<0)
                {
                    errMsg = "上传Trigger出错：" + uploadRes;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "上传数据UploadTestData error(Exception):" + ex.Message;
                return false;
            }
        }


        public bool ParserTemplatePath(string path, out string errMsg)//后续增加 加载测试数据，解析环境 条件等
        {
            errMsg = "";
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(path);
                XmlNode root = xmlDoc.SelectSingleNode("ATMS_RECORD");
                XmlNode meas = null;
                if (root == null)
                {
                    root = xmlDoc.SelectSingleNode("MEAS_RECORD");
                    meas = root;
                }
                else
                {
                    XmlNode prod = root.SelectSingleNode("PROD_RECORD");
                    if (prod != null)
                    {
                        if (!ProdRecordParse(prod, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "模板格式错误！找不到ATMS_RECORD/PROD_RECORD节点";
                        return false;
                    }
                    meas = root.SelectSingleNode("MEAS_RECORD");
                }

                if (meas != null)
                {
                    string measxml = meas.InnerXml;
                    XmlString(ref measxml, out errMsg);
                    if (errMsg != "")
                        return false;
                    measxml = measxml.Replace("\r", "");
                    measxml = measxml.Replace("\n", "");
                    measxml = measxml.Replace("\t", "");
                    measxml = measxml.Replace(" ", "");
                    byte[] crcContent = System.Text.Encoding.UTF8.GetBytes(measxml);
                    uint crcValue = GetCRC32(crcContent, crcContent.Length);
                    if (meas.Attributes["CRC32"].Value.ToString() != crcValue.ToString("X8"))
                    {
                        errMsg += "模板CRC校验失败.";
                        //return false;
                    }

                    XmlNode docrev = meas.SelectSingleNode("DOCREV_RECORD");
                    if (docrev != null)
                    {
                        if (!DocrevRecordParse(docrev, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/DOCREV_RECORD节点";
                        return false;
                    }

                    XmlNode recipe = meas.SelectSingleNode("RECIPE_RECORD");
                    if (recipe != null)
                    {
                        if (!RecipeRecordParse(recipe, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/RECIPE_RECORD节点";
                        return false;
                    }

                    XmlNode mfg = meas.SelectSingleNode("MFG_RECORD");
                    if (mfg != null)
                    {
                        if (!MFGRecordParse(mfg, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/MFG_RECORD节点";
                        return false;
                    }

                    XmlNode cfg = meas.SelectSingleNode("CFG_RECORD");
                    if (cfg != null)
                    {
                        if (!CFGRecordParse(cfg, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/CFG_RECORD节点";
                        return false;
                    }

                    XmlNode condNode = meas.SelectSingleNode("COND_RECORD");
                    if (condNode != null)
                    {
                        if (!CONDRecordParse(condNode, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/COND_RECORD节点";
                        return false;
                    }

                    XmlNode test = meas.SelectSingleNode("TEST_RECORD");
                    if (test != null)
                    {
                        if (!TESTRecordParse(test, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/TEST_RECORD节点";
                        return false;
                    }

                    XmlNode misc = meas.SelectSingleNode("MISC_RECORD");
                    if (misc != null)
                    {
                        if (!MISCRecordParse(misc, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/MISC_RECORD节点";
                        return false;
                    }
                }
                else
                {
                    errMsg += "\r\n模板格式错误！找不到MEAS_RECORD节点";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType + "." +
                    System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }
        public bool ParserTemplate(string content, out string errMsg)//后续增加 加载测试数据，解析环境 条件等
        {
            errMsg = "";
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(content);
                XmlNode root = xmlDoc.SelectSingleNode("ATMS_RECORD");
                XmlNode meas = null;
                if (root == null)
                {
                    root = xmlDoc.SelectSingleNode("MEAS_RECORD");
                    meas = root;
                }
                else
                {
                    XmlNode prod = root.SelectSingleNode("PROD_RECORD");
                    if (prod != null)
                    {
                        if (!ProdRecordParse(prod, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "模板格式错误！找不到ATMS_RECORD/PROD_RECORD节点";
                        return false;
                    }
                    meas = root.SelectSingleNode("MEAS_RECORD");
                }

                if (meas != null )
                {
                    string measxml = meas.InnerXml;
                    XmlString(ref measxml, out errMsg);
                    if (errMsg != "")
                        return false ;
                    measxml = measxml.Replace("\r", "");
                    measxml = measxml.Replace("\n", "");
                    measxml = measxml.Replace("\t", "");
                    measxml = measxml.Replace(" ", "");
                    byte[] crcContent = System.Text.Encoding.UTF8.GetBytes(measxml);
                    uint crcValue = GetCRC32(crcContent, crcContent.Length);
                    if (meas.Attributes["CRC32"].Value.ToString() != crcValue.ToString("X8"))
                    {
                        errMsg += "模板CRC校验失败.";
                        //return false;
                    }

                    XmlNode docrev = meas.SelectSingleNode("DOCREV_RECORD");
                    if (docrev != null)
                    {
                        if (!DocrevRecordParse(docrev, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/DOCREV_RECORD节点";
                        return false;
                    }

                    XmlNode recipe = meas.SelectSingleNode("RECIPE_RECORD");
                    if (recipe != null)
                    {
                        if (!RecipeRecordParse(recipe, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/RECIPE_RECORD节点";
                        return false;
                    }

                    XmlNode mfg = meas.SelectSingleNode("MFG_RECORD");
                    if (mfg != null)
                    {
                        if (!MFGRecordParse(mfg, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/MFG_RECORD节点";
                        return false;
                    }

                    XmlNode cfg =meas.SelectSingleNode("CFG_RECORD");
                    if (cfg != null)
                    {
                        if (!CFGRecordParse(cfg, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/CFG_RECORD节点";
                        return false;
                    }

                    XmlNode condNode = meas.SelectSingleNode("COND_RECORD");
                    if (condNode != null)
                    {
                        if (!CONDRecordParse(condNode, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/COND_RECORD节点";
                        return false;
                    }

                    XmlNode test = meas.SelectSingleNode("TEST_RECORD");
                    if (test != null)
                    {
                        if (!TESTRecordParse(test, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/TEST_RECORD节点";
                        return false;
                    }

                    XmlNode misc = meas.SelectSingleNode("MISC_RECORD");
                    if (misc != null)
                    {
                        if (!MISCRecordParse(misc, out errMsg))
                            return false;
                    }
                    else
                    {
                        errMsg += "\r\n模板格式错误！找不到MEAS_RECORD/MISC_RECORD节点";
                        return false;
                    }
                }
                else
                {
                    errMsg += "\r\n模板格式错误！找不到MEAS_RECORD节点";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType + "." +
                    System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }

        public bool ParserTemplateData(string content, out string errMsg)//后续增加 加载测试数据，解析环境 条件等
        {
            errMsg = "";
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(content);
                XmlNode root = xmlDoc.SelectSingleNode("TEST_RECORD");
                //XmlNode meas = null;
                if (root == null)
                {
                    errMsg += "模板格式错误！找不到TEST_RECORD节点";
                    return false;
                }
                else
                {
                    if (!TESTRecordParse(root, out errMsg))
                        return false;
                }
                             
                return true;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType + "." +
                    System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }
        public void XmlString(ref string str, out string errMsg)
        {
            errMsg = "";
            try
            {
                while (true)
                {
                    if (str.Contains("&lt;"))
                    {
                        str = str.Substring(0, str.IndexOf("&lt;")) + "<" + str.Substring(str.IndexOf("&lt;") + 4);
                    }
                    if (str.Contains("&gt;"))
                    {
                        str = str.Substring(0, str.IndexOf("&gt;")) + ">" + str.Substring(str.IndexOf("&gt;") + 4);
                    }
                    if (str.Contains("&amp;"))
                    {
                        str = str.Substring(0, str.IndexOf("&amp;")) + "&" + str.Substring(str.IndexOf("&amp;") + 5);
                    }
                    if (str.Contains("&quot;"))
                    {
                        str = str.Substring(0, str.IndexOf("&quot;")) + '"' + str.Substring(str.IndexOf("&quot;") + 6);
                    }
                    if (str.Contains("&apos;"))
                    {
                        str = str.Substring(0, str.IndexOf("&apos;")) + "'" + str.Substring(str.IndexOf("&apos;") + 6);
                    }

                    if ((!str.Contains("&lt;")) && (!str.Contains("&gt;")) && (!str.Contains("&amp;")) && (!str.Contains("&quot;")) && (!str.Contains("&apos;")))
                        break;
                }
            }
            catch (Exception ex)
            {
                errMsg += "XmlString error:" + ex.Message + "\r";
                return;
            }
        }

        
        private bool ProdRecordParse(XmlNode prod, out string errMsg)
        {
            errMsg = "";
            try
            {
                productInfo.ProductCategory = prod.SelectSingleNode("PROD_CATEGORY").Attributes["VALUE"].Value.ToString();
                productInfo.ProductFamily = prod.SelectSingleNode("PROD_FAMILY").Attributes["VALUE"].Value.ToString();
                productInfo.ProductType = prod.SelectSingleNode("PROD_TYPE").Attributes["VALUE"].Value.ToString();
                productInfo.ProductName = prod.SelectSingleNode("PROD_NAME").Attributes["VALUE"].Value.ToString();
                productInfo.ProductPN = prod.SelectSingleNode("PROD_PN").Attributes["VALUE"].Value.ToString();
                productInfo.SN = prod.SelectSingleNode("PROD_SN").Attributes["VALUE"].Value.ToString();
                productInfo.ProductRev = prod.SelectSingleNode("PROD_REV").Attributes["VALUE"].Value.ToString();
                productInfo.ProductPhase = prod.SelectSingleNode("PROD_PHASE").Attributes["VALUE"].Value.ToString();
                productInfo.InProcess = prod.SelectSingleNode("PROD_STA").Attributes["VALUE"].Value.ToString();
                productInfo.Hold = prod.SelectSingleNode("HOLD_STA").Attributes["VALUE"].Value.ToString();
                productInfo.Rework = prod.SelectSingleNode("REWORK_STA").Attributes["VALUE"].Value.ToString();
                productInfo.SerialNo1 = prod.SelectSingleNode("SERIALNO1").Attributes["VALUE"].Value.ToString();
                productInfo.SerialNo2 = prod.SelectSingleNode("SERIALNO2").Attributes["VALUE"].Value.ToString();
                productInfo.SerialNo3 = prod.SelectSingleNode("SERIALNO3").Attributes["VALUE"].Value.ToString();
                productInfo.WONum = prod.SelectSingleNode("WO_NUM").Attributes["VALUE"].Value.ToString();
                productInfo.WOType = prod.SelectSingleNode("WO_TYPE").Attributes["VALUE"].Value.ToString();
                productInfo.WOPlanedDate = prod.SelectSingleNode("WO_PLANED_DATE").Attributes["VALUE"].Value.ToString();
                productInfo.WOIssueDate = prod.SelectSingleNode("WO_ISSUE_DATE").Attributes["VALUE"].Value.ToString();
                productInfo.SO = prod.SelectSingleNode("SO_NUM").Attributes["VALUE"].Value.ToString();
                productInfo.LotNum = prod.SelectSingleNode("LOT_NUM").Attributes["VALUE"].Value.ToString();
                productInfo.SpecNum = prod.SelectSingleNode("SPEC_NUM").Attributes["VALUE"].Value.ToString();
                productInfo.SpecRev = prod.SelectSingleNode("SPEC_REV").Attributes["VALUE"].Value.ToString();
                productInfo.CsmName = prod.SelectSingleNode("CSM_NAME").Attributes["VALUE"].Value.ToString();
                productInfo.WorkflowName = prod.SelectSingleNode("WORKFLOW_NAME").Attributes["VALUE"].Value.ToString();
                productInfo.CurProcess = prod.SelectSingleNode("CUR_PROCESS").Attributes["VALUE"].Value.ToString();
                productInfo.PreProcess = prod.SelectSingleNode("PRE_PROCESS").Attributes["VALUE"].Value.ToString();
                productInfo.Parent = prod.SelectSingleNode("Parent").Attributes["VALUE"].Value.ToString();
                productInfo.Status = prod.SelectSingleNode("Status").Attributes["VALUE"].Value.ToString();
                productInfo.Operation = prod.SelectSingleNode("Operation").Attributes["VALUE"].Value.ToString();
                productInfo.Qty = prod.SelectSingleNode("Qty").Attributes["VALUE"].Value.ToString();
                productInfo.MaterialCategory = prod.SelectSingleNode("Material_Category").Attributes["VALUE"].Value.ToString();
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "ProdRecordParse error:" + ex.Message;
                return false;
            }
        }
        private bool DocrevRecordParse(XmlNode docrev, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (docrev.ChildNodes == null || docrev.ChildNodes.Count == 0)
                {
                    errMsg += "ATMS_RECORD/MEAS_RECORD/DOCREV_RECORD节点格式错误.模板未审批";
                    return false;
                }
                foreach (XmlNode node in docrev.ChildNodes)
                {
                    switch (node.Attributes["NAME"].Value.ToString())
                    {
                        case "Prod_Process":
                            docrevInfo.ProdProcess = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Prod_PN":
                            docrevInfo.ProdPN = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Author":
                            docrevInfo.Author = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Creation Date":
                            docrevInfo.CreationDate = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Version":
                            docrevInfo.Version = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Approved By":
                            docrevInfo.ApprovedBy = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Approval Date":
                            docrevInfo.ApprovedDate = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Recipe ID":
                            docrevInfo.RecipeID = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Inventory Recheck":
                            docrevInfo.InventoryRecheck = node.Attributes["VALUE"].Value.ToString();
                            break;
                        default:
                            break;
                    }
                    
                }
                if (docrevInfo.ProdProcess == "" || docrevInfo.ProdPN == "" || docrevInfo.Author == "" || docrevInfo.CreationDate == "" || docrevInfo.Version == "" || docrevInfo.ApprovedBy == "" || docrevInfo.ApprovedDate == "")
                {
                    errMsg += "模板未审批.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg +="DocrevRecordParse error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool RecipeRecordParse(XmlNode recipe, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (recipe.ChildNodes == null || recipe.ChildNodes.Count == 0)
                {
                    errMsg += "ATMS_RECORD/MEAS_RECORD/RECIPE_RECORD节点格式错误.模板未审批";
                    return false;
                }
                foreach (XmlNode node in recipe.ChildNodes)
                {
                    switch (node.Attributes["NAME"].Value.ToString())
                    {
                        case "Recipe_CRC":
                            recipeInfo.RecipeCRC = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Prod_SN":
                            recipeInfo.ProdSN = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Prod_Process":
                            recipeInfo.ProdProcess = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "FW_PN":
                            recipeInfo.FW_PN = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "FW_VER":
                            recipeInfo.FW_VER = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "FW_Author":
                            recipeInfo.FW_Author = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "FW_Date":
                            recipeInfo.FW_Date = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "FW_Status":
                            recipeInfo.FW_Status = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TS_PN":
                            recipeInfo.TS_PN = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TS_VER":
                            recipeInfo.TS_VER = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TS_Author":
                            recipeInfo.TS_Author = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TS_Date":
                            recipeInfo.TS_Date = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TS_Status":
                            recipeInfo.TS_Status = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "CS_PN":
                            recipeInfo.CS_PN = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "CS_VER":
                            recipeInfo.CS_VER = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "CS_Author":
                            recipeInfo.CS_Author = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "CS_Date":
                            recipeInfo.CS_Date = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "CS_Status":
                            recipeInfo.CS_Status = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TP_PN":
                            recipeInfo.TP_PN = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TP_VER":
                            recipeInfo.TP_VER = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TP_Author":
                            recipeInfo.TP_Author = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TP_Date":
                            recipeInfo.TP_Date = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TP_Status":
                            recipeInfo.TP_Status = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TT_PN":
                            recipeInfo.TT_PN = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TT_VER":
                            recipeInfo.TT_VER = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TT_Author":
                            recipeInfo.TT_Author = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TT_Date":
                            recipeInfo.TT_Date = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "TT_Status":
                            recipeInfo.TT_Status = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Workflow_Name":
                            recipeInfo.Workflow_Name = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Operation_Name":
                            recipeInfo.Operation_Name = node.Attributes["VALUE"].Value.ToString();
                            break;
                        default:
                            break;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "RecipeRecordParse error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool SaveRecipeRecord(XmlNode recipe, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (recipe.ChildNodes == null || recipe.ChildNodes.Count == 0)
                {
                    errMsg += "ATMS_RECORD/MEAS_RECORD/RECIPE_RECORD节点格式错误.模板未审批";
                    return false;
                }
                XmlNode crcNode = null;
                string strCrcXml = "";
                foreach (XmlNode node in recipe.ChildNodes)
                {
                    switch (node.Attributes["NAME"].Value.ToString())
                    {
                        case "Recipe_CRC":
                            crcNode = node;
                            //node.Attributes["VALUE"].Value= recipeInfo.RecipeCRC;
                            break;
                        case "Prod_SN":
                             node.Attributes["VALUE"].Value= recipeInfo.ProdSN;
                            break;
                        case "Prod_Process":
                             node.Attributes["VALUE"].Value= recipeInfo.ProdProcess;
                            break;
                        case "FW_PN":
                             node.Attributes["VALUE"].Value= recipeInfo.FW_PN;
                            break;
                        case "FW_VER":
                            node.Attributes["VALUE"].Value= recipeInfo.FW_VER;
                            break;
                        case "FW_Author":
                            node.Attributes["VALUE"].Value= recipeInfo.FW_Author;
                            break;
                        case "FW_Date":
                            node.Attributes["VALUE"].Value= recipeInfo.FW_Date;
                            break;
                        case "FW_Status":
                            node.Attributes["VALUE"].Value= recipeInfo.FW_Status;
                            break;
                        case "TS_PN":
                            node.Attributes["VALUE"].Value= recipeInfo.TS_PN;
                            break;
                        case "TS_VER":
                            node.Attributes["VALUE"].Value= recipeInfo.TS_VER;
                            break;
                        case "TS_Author":
                            node.Attributes["VALUE"].Value= recipeInfo.TS_Author;
                            break;
                        case "TS_Date":
                            node.Attributes["VALUE"].Value= recipeInfo.TS_Date;
                            break;
                        case "TS_Status":
                            node.Attributes["VALUE"].Value= recipeInfo.TS_Status;
                            break;
                        case "CS_PN":
                            node.Attributes["VALUE"].Value= recipeInfo.CS_PN;
                            break;
                        case "CS_VER":
                            node.Attributes["VALUE"].Value= recipeInfo.CS_VER;
                            break;
                        case "CS_Author":
                            node.Attributes["VALUE"].Value= recipeInfo.CS_Author;
                            break;
                        case "CS_Date":
                            node.Attributes["VALUE"].Value= recipeInfo.CS_Date;
                            break;
                        case "CS_Status":
                            node.Attributes["VALUE"].Value=recipeInfo.CS_Status;
                            break;
                        case "TP_PN":
                            node.Attributes["VALUE"].Value= recipeInfo.TP_PN;
                            break;
                        case "TP_VER":
                            node.Attributes["VALUE"].Value= recipeInfo.TP_VER;
                            break;
                        case "TP_Author":
                            node.Attributes["VALUE"].Value= recipeInfo.TP_Author;
                            break;
                        case "TP_Date":
                            node.Attributes["VALUE"].Value= recipeInfo.TP_Date;
                            break;
                        case "TP_Status":
                            node.Attributes["VALUE"].Value= recipeInfo.TP_Status;
                            break;
                        case "TT_PN":
                            node.Attributes["VALUE"].Value= recipeInfo.TT_PN;
                            break;
                        case "TT_VER":
                            node.Attributes["VALUE"].Value= recipeInfo.TT_VER;
                            break;
                        case "TT_Author":
                            node.Attributes["VALUE"].Value=recipeInfo.TT_Author;
                            break;
                        case "TT_Date":
                            node.Attributes["VALUE"].Value= recipeInfo.TT_Date;
                            break;
                        case "TT_Status":
                            node.Attributes["VALUE"].Value= recipeInfo.TT_Status;
                            break;
                        case "Workflow_Name":
                            node.Attributes["VALUE"].Value= recipeInfo.Workflow_Name;
                            break;
                        case "Operation_Name":
                            node.Attributes["VALUE"].Value= recipeInfo.Operation_Name;
                            break;
                        default:
                            break;
                    }
                    if(node.Attributes["NAME"].Value!="Recipe_CRC")
                    {
                        strCrcXml += node.OuterXml;
                    }
                }
                if(crcNode!=null)
                {
                    strCrcXml = strCrcXml.Replace("\r", "");
                    strCrcXml = strCrcXml.Replace("\n", "");
                    strCrcXml = strCrcXml.Replace("\t", "");
                    strCrcXml = strCrcXml.Replace(" ", "");
                    byte[] crcConInner = System.Text.Encoding.UTF8.GetBytes(strCrcXml);
                    UInt32 conRecCrcValue = GetCRC32(crcConInner, crcConInner.Length);
                    crcNode.Attributes["VALUE"].Value = (conRecCrcValue).ToString("X8");
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "SaveRecipeRecord error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool MFGRecordParse(XmlNode mfg, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (mfg.ChildNodes == null || mfg.ChildNodes.Count == 0)
                {
                    errMsg += "ATMS_RECORD/MEAS_RECORD/MFG_RECORD节点格式错误.模板未审批";
                    return false;
                }
                foreach (XmlNode node in mfg.ChildNodes)
                {
                    switch (node.Attributes["NAME"].Value.ToString())
                    {
                        case "Prod_SN":
                            MFGInfo.ProdSN = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Prod_Process":
                            MFGInfo.ProdProcess = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "WO_NUM":
                            MFGInfo.WONum = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Operator":
                            MFGInfo.Operator = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Perms_Level":
                            MFGInfo.PermsLevel = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Test_Area":
                            MFGInfo.TestArea = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Test_Station":
                            MFGInfo.TestStation = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Tester_ID":
                            MFGInfo.TesterID = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Test_Type":
                            MFGInfo.TestType = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "ATE_Code":
                            MFGInfo.ATECode = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Refer_File":
                            MFGInfo.ReferFile = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "GRR_Status":
                            MFGInfo.GRRStatus = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "GDS_Status":
                            MFGInfo.GDSStatus = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "ESD_Status":
                            MFGInfo.ESDStatus = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Move_In":
                            MFGInfo.MoveIn = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Move_Out":
                            MFGInfo.MoveOut = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Failure_Code":
                            MFGInfo.FailureCode = node.Attributes["VALUE"].Value.ToString();
                            break;
                        case "Test_Result":
                            MFGInfo.TestResult = node.Attributes["VALUE"].Value.ToString();
                            break;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "MFGRecordParse error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool SaveMFGRecord(XmlNode mfg, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (mfg.ChildNodes == null || mfg.ChildNodes.Count == 0)
                {
                    errMsg += "ATMS_RECORD/MEAS_RECORD/MFG_RECORD节点格式错误.模板未审批";
                    return false;
                }
                foreach (XmlNode childNode in mfg.ChildNodes)
                {
                    switch (childNode.Attributes["NAME"].Value.ToString())
                    {
                        case "Prod_SN":
                            {
                                childNode.Attributes["VALUE"].Value = MFGInfo.ProdSN;
                            }
                            break;
                        case "Prod_Process":
                            childNode.Attributes["VALUE"].Value= MFGInfo.ProdProcess;
                            break;
                        case "WO_NUM":
                            childNode.Attributes["VALUE"].Value= MFGInfo.WONum;
                            break;
                        case "Operator":
                            childNode.Attributes["VALUE"].Value= MFGInfo.Operator;
                            break;
                        case "Perms_Level":
                            childNode.Attributes["VALUE"].Value= MFGInfo.PermsLevel;
                            break;
                        case "Test_Area":
                            childNode.Attributes["VALUE"].Value= MFGInfo.TestArea;
                            break;
                        case "Test_Station":
                            childNode.Attributes["VALUE"].Value= MFGInfo.TestStation;
                            break;
                        case "Tester_ID":
                            childNode.Attributes["VALUE"].Value= MFGInfo.TesterID;
                            break;
                        case "Test_Type":
                            childNode.Attributes["VALUE"].Value= MFGInfo.TestType;
                            break;
                        case "ATE_Code":
                            childNode.Attributes["VALUE"].Value= MFGInfo.ATECode;
                            break;
                        case "Refer_File":
                            childNode.Attributes["VALUE"].Value= MFGInfo.ReferFile;
                            break;
                        case "GRR_Status":
                            childNode.Attributes["VALUE"].Value= MFGInfo.GRRStatus;
                            break;
                        case "GDS_Status":
                            childNode.Attributes["VALUE"].Value= MFGInfo.GDSStatus;
                            break;
                        case "ESD_Status":
                            childNode.Attributes["VALUE"].Value= MFGInfo.ESDStatus;
                            break;
                        case "Move_In":
                            childNode.Attributes["VALUE"].Value= MFGInfo.MoveIn;
                            break;
                        case "Move_Out":
                            childNode.Attributes["VALUE"].Value= MFGInfo.MoveOut;
                            break;
                        case "Failure_Code":
                            childNode.Attributes["VALUE"].Value= MFGInfo.FailureCode;
                            break;
                        case "Test_Result":
                            childNode.Attributes["VALUE"].Value= MFGInfo.TestResult;
                            break;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "MFGRecordParse error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool CFGRecordParse(XmlNode cfg, out string errMsg)
        {
            errMsg = "";
            try
            {
                foreach (XmlNode node in cfg.ChildNodes)
                {
                    foreach (XmlNode para in node.ChildNodes)
                    {
                        CFGRecordInfo info = new CFGRecordInfo();
                        info.SectionName = node.Attributes["NAME"].Value.ToString();
                        info.SectionDesc = node.Attributes["DESC"].Value.ToString();
                        info.Name = para.Attributes["NAME"].Value.ToString();
                        info.Value = para.Attributes["VALUE"].Value.ToString();
                        info.Desc = para.Attributes["DESC"].Value.ToString();
                        info.Units = para.Attributes["UNITS"].Value.ToString();
                        info.Scale = para.Attributes["SCALE"].Value.ToString();
                        CFGInfo.Add(info);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "CFGRecordParse error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool CONDRecordParse(XmlNode ele, out string errMsg)
        {
            errMsg = "";
            try
            {
                XmlNode tenvgp = ele.SelectSingleNode("TENV_GROUP");
                if (tenvgp != null)
                {
                    XmlNodeList tenvIDs = ((XmlElement)tenvgp).GetElementsByTagName("TENV_ID");

                    if (tenvIDs != null && tenvIDs.Count > 0)
                    {
                        foreach (XmlElement tenvID in tenvIDs)
                        {
                            XmlNodeList tenvs = tenvID.GetElementsByTagName("TENV");
                            if (tenvs == null || tenvs.Count <= 0)
                            {
                                errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/TENV_GROUP/TENV_ID/TENV节点";
                                return false;
                            }
                            foreach (XmlNode tenv in tenvs)
                            {
                                FusionEnvironmentInfo envInfo = new FusionEnvironmentInfo();
                                envInfo.EnvironmentID = tenvID.Attributes["VALUE"].Value;
                                envInfo.Name = tenv.Attributes["NAME"].Value;
                                envInfo.Value = tenv.Attributes["VALUE"].Value;
                                envInfo.Desc = tenv.Attributes["DESC"].Value;
                                envInfo.Units = tenv.Attributes["UNITS"].Value;
                                envInfo.Scale = tenv.Attributes["SCALE"].Value;
                                EnvironmentInfo.Add(envInfo);
                            }
                        }
                    }
                    else
                    {
                        errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/TENV_GROUP/TENV_ID节点";
                        return false;
                    }
                }
                else
                {
                    errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/TENV_GROUP节点";
                    return false;
                }

                XmlNode objgp = ele.SelectSingleNode("OBJECT_GROUP");
                if (objgp != null)
                {
                    XmlNodeList objgs = ((XmlElement)objgp).GetElementsByTagName("OBJECT");
                    if (objgs != null && objgs.Count > 0)
                    {
                        foreach (XmlElement objg in objgs)
                        {                         
                            FusionObjectInfo objInfo = new FusionObjectInfo();
                            objInfo.Name = objg.Attributes["NAME"].Value;
                            objInfo.Instance = objg.Attributes["INSTANCE"].Value;
                            objInfo.Active = objg.Attributes["ACTIVE"].Value;
                            objInfo.Desc = objg.Attributes["DESC"].Value;
                            ObjectInfos.Add(objInfo);
                        }
                    }
                    else
                    {
                        errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/OBJECT_GROUP节点";
                        return false;
                    }
                }
                else
                {
                    errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/OBJECT_GROUP节点";
                    return false;
                }


                XmlNode portGP = ele.SelectSingleNode("PORT_GROUP");
                if (portGP != null)
                {
                    XmlNodeList portNodes = ((XmlElement)portGP).GetElementsByTagName("PORT");
                    if (portNodes != null && portNodes.Count > 0)
                    {
                        foreach (XmlElement portNode in portNodes)
                        {
                            FusionPortInfo prtInfo = new FusionPortInfo();
                            prtInfo.Value = portNode.Attributes["VALUE"].Value;
                            prtInfo.Name = portNode.Attributes["NAME"].Value;
                            prtInfo.Desc = portNode.Attributes["DESC"].Value;
                            prtInfo.Active = portNode.Attributes["ACTIVE"].Value;
                            PortInfo.Add(prtInfo);
                        }
                    }
                    else
                    {
                        errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/PORT_GROUP节点";
                        return false;
                    }
                }
                else
                {
                    errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/PORT_GROUP节点";
                    return false;
                }

                XmlNode conGP = ele.SelectSingleNode("COND_GROUP");
                if (conGP != null)
                {
                    XmlNodeList conIDNodes = ((XmlElement)conGP).GetElementsByTagName("COND_ID");

                    if (conIDNodes != null && conIDNodes.Count > 0)
                    {
                        foreach (XmlElement conIDNode in conIDNodes)
                        {
                            XmlNodeList conNodes = conIDNode.GetElementsByTagName("COND");
                            if (conNodes != null && conNodes.Count > 0)
                            {
                                foreach (XmlElement conNode in conNodes)
                                {
                                    FusionConditionInfo conInfo = new FusionConditionInfo();
                                    conInfo.ConditionID = conIDNode.Attributes["VALUE"].Value;
                                    conInfo.Name = conNode.Attributes["NAME"].Value;
                                    conInfo.Value = conNode.Attributes["VALUE"].Value;
                                    conInfo.Units = conNode.Attributes["UNITS"].Value;
                                    conInfo.Scale = conNode.Attributes["SCALE"].Value;
                                    conInfo.Desc = conNode.Attributes["DESC"].Value;
                                    conInfo.ConditionDesc = conIDNode.Attributes["DESC"].Value;
                                    ConditionInfo.Add(conInfo);
                                }
                            }
                            else
                            {
                                errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/COND_GROUP/COND_ID节点";
                                return false;
                            }
                        }
                    }
                    else
                    {
                        errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/COND_GROUP节点";
                        return false;
                    }
                }
                else
                {
                    errMsg = "模板格式错误！找不到MEAS_RECORD/COND_RECORD/COND_GROUP节点";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType + "." +
                    System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool TESTRecordParse(XmlNode test, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (test.ChildNodes == null || test.ChildNodes.Count == 0)
                {
                    errMsg += "ATMS_RECORD/MEAS_RECORD/TEST_RECORD节点格式错误.";
                    return false;
                }
                foreach (XmlNode tenvID in test.ChildNodes)
                {
                    if (tenvID != null)
                    {
                        foreach (XmlNode objectID in tenvID.ChildNodes)
                        {
                            if (objectID != null)
                            {
                                foreach (XmlNode port in objectID.ChildNodes)
                                {
                                    if (port != null)
                                    {
                                        foreach (XmlNode condID in port.ChildNodes)
                                        {
                                            if (condID != null)
                                            {
                                                foreach (XmlNode para in condID.ChildNodes)
                                                {
                                                    if (para != null)
                                                    {
                                                        MESTestInfo info = new MESTestInfo();
                                                        info.EnvironmentID = tenvID.Attributes["VALUE"].Value.ToString();
                                                        info.ObjectID = objectID.Attributes["NAME"].Value.ToString();
                                                        info.PortID = port.Attributes["VALUE"].Value.ToString();
                                                        info.PortNameForAMTS= port.Attributes["VALUE"].Value.ToString();
                                                        info.PortNameForUser = port.Attributes["NAME"].Value.ToString();
                                                        info.ConditionID = condID.Attributes["VALUE"].Value.ToString();
                                                        //Environment相关的参数
                                                        for(int nEnv=0;nEnv<EnvironmentInfo.Count;nEnv++)
                                                        {
                                                            if(info.EnvironmentID==EnvironmentInfo[nEnv].EnvironmentID)
                                                            {
                                                                if(EnvironmentInfo[nEnv].Name== "TEMP")
                                                                {
                                                                    double dbTmpt = 0;
                                                                    /*if (EnvironmentInfo[nEnv].Value.Contains("~"))
                                                                    {
                                                                        string[] envs = EnvironmentInfo[nEnv].Value.Split('~');
                                                                        double.TryParse(envs[1], out dbTmpt);
                                                                    }
                                                                    else*/
                                                                    {
                                                                        double.TryParse(EnvironmentInfo[nEnv].Value, out dbTmpt);
                                                                    }
                                                                    info.Temperature = dbTmpt;
                                                                    info.TemperStr = EnvironmentInfo[nEnv].Value;
                                                                }
                                                                else if (EnvironmentInfo[nEnv].Name == "TEMP_TIME")
                                                                {
                                                                    double dbTmptTime = 0;
                                                                    double.TryParse(EnvironmentInfo[nEnv].Value, out dbTmptTime);
                                                                    info.TmptChangeTimes = dbTmptTime;
                                                                }
                                                            }
                                                        }

                                                        //和Port相关的参数
                                                        for(int nPort=0;nPort<PortInfo.Count;nPort++)
                                                        {
                                                            if(info.PortID==PortInfo[nPort].Value)
                                                            {
                                                                info.PortNameForUser = PortInfo[nPort].Name;
                                                            }
                                                        }

                                                        //条件相关
                                                        for(int nCon=0;nCon<ConditionInfo.Count;nCon++)
                                                        {
                                                            if(info.ConditionID==ConditionInfo[nCon].ConditionID)
                                                            {
                                                                if(ConditionInfo[nCon].Name=="ITU")
                                                                {
                                                                    info.ITU = Convert.ToDouble(ConditionInfo[nCon].Value);
                                                                }
                                                                else if (ConditionInfo[nCon].Name == "WL")
                                                                {
                                                                    if(ConditionInfo[nCon].Value.Contains("~"))
                                                                    {
                                                                        string[] wls = ConditionInfo[nCon].Value.Split('~');
                                                                        info.WLLeft = Convert.ToDouble(wls[0]);
                                                                        info.WLRight = Convert.ToDouble(wls[1]);
                                                                        info.StartWL = Convert.ToDouble(wls[0]);
                                                                        info.StopWL = Convert.ToDouble(wls[1]);
                                                                    }
                                                                    else if (ConditionInfo[nCon].Value.Contains("-"))
                                                                    {
                                                                        string[] wls = ConditionInfo[nCon].Value.Split('-');
                                                                        info.WLLeft = Convert.ToDouble(wls[0]);
                                                                        info.WLRight = Convert.ToDouble(wls[1]);
                                                                        info.StartWL = Convert.ToDouble(wls[0]);
                                                                        info.StopWL = Convert.ToDouble(wls[1]);
                                                                    }
                                                                    else
                                                                    {
                                                                        info.WLLeft = Convert.ToDouble(ConditionInfo[nCon].Value);
                                                                        info.WLRight = Convert.ToDouble(ConditionInfo[nCon].Value);
                                                                        info.StartWL = Convert.ToDouble(ConditionInfo[nCon].Value);
                                                                        info.StopWL = Convert.ToDouble(ConditionInfo[nCon].Value);
                                                                    }
                                                                }
                                                                else if (ConditionInfo[nCon].Name.ToUpper() == "STEP")
                                                                {
                                                                    info.Step=Convert.ToDouble(ConditionInfo[nCon].Value);
                                                                }
                                                                else if (ConditionInfo[nCon].Name.ToUpper() == "DB DOWN")
                                                                {
                                                                    info.Deepth = ConditionInfo[nCon].Value;
                                                                }
                                                                else if (ConditionInfo[nCon].Name.ToUpper() == "PASSBAND")
                                                                {
                                                                    info.Passband = ConditionInfo[nCon].Value;
                                                                }
                                                                else if (ConditionInfo[nCon].Name.ToUpper() == "VOLTAGE")
                                                                {
                                                                    info.Voltage = ConditionInfo[nCon].Value;
                                                                }
                                                                else if (ConditionInfo[nCon].Name.ToUpper() == "ATTEN")
                                                                {
                                                                    info.Atten = ConditionInfo[nCon].Value;
                                                                }
                                                                else if (ConditionInfo[nCon].Name.ToUpper() == ("Reverse_Volt").ToUpper())
                                                                {
                                                                    info.ReverseVolt = ConditionInfo[nCon].Value;
                                                                }
                                                                else if (ConditionInfo[nCon].Name.ToUpper() == ("BPSet").ToUpper())
                                                                {
                                                                    info.BPParamSet = ConditionInfo[nCon].Value;
                                                                }
                                                                else if (ConditionInfo[nCon].Name.ToUpper() == ("CurrentSet").ToUpper())
                                                                {
                                                                    info.BPCurrentSet = ConditionInfo[nCon].Value;
                                                                }
                                                            }
                                                        }
                                                        //info.TestParam = para.Attributes["NAME"].Value.ToString();
                                                        foreach (MESParam param in Enum.GetValues(typeof(MESParam)))
                                                        {
                                                            if (param.GetMESTemplateKeywords() == para.Attributes["NAME"].Value.ToString())
                                                            {
                                                                info.TestParam = param;
                                                                break;
                                                            }
                                                        }
                                                        info.Criterion1 = para.Attributes["MAX"].Value.ToString();
                                                        info.Criterion = para.Attributes["MIN"].Value.ToString();
                                                        info.FreeLowestCriterion = para.Attributes["FREE_MIN"].Value.ToString();
                                                        info.FreeHighestCriterion = para.Attributes["FREE_MAX"].Value.ToString();
                                                        double value = 0;
                                                        if (para.Attributes["VALUE"].Value.ToString() != "")
                                                        {
                                                            double.TryParse(para.Attributes["VALUE"].Value.ToString(), out value);
                                                            info.TestedValue = value;
                                                        }
                                                        
                                                        info.Units = para.Attributes["UNITS"].Value.ToString();
                                                        info.Scale = para.Attributes["SCALE"].Value.ToString();
                                                        info.Active = para.Attributes["ACTIVE"].Value.ToString();
                                                        info.Filename = para.Attributes["FILENAME"].Value.ToString();
                                                        info.TestDate = para.Attributes["TEST_DATE"].Value.ToString();
                                                        info.ParamColumnName = info.TestParam.GetMESTemplateKeywords();
                                                        AllTestInfo.Add(info);
                                                    }
                                                    else
                                                    {
                                                        errMsg += "ATMS_RECORD/MEAS_RECORD/TEST_RECORD/TENV_ID/OBJECT/PORT/COND_ID/PARA节点格式错误.";
                                                        return false;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                errMsg += "ATMS_RECORD/MEAS_RECORD/TEST_RECORD/TENV_ID/OBJECT/PORT/COND_ID节点格式错误.";
                                                return false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        errMsg += "ATMS_RECORD/MEAS_RECORD/TEST_RECORD/TENV_ID/OBJECT/PORT节点格式错误.";
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                errMsg += "ATMS_RECORD/MEAS_RECORD/TEST_RECORD/TENV_ID/OBJECT节点格式错误.";
                                return false;
                            }
                        }
                    }
                    else
                    {
                        errMsg += "ATMS_RECORD/MEAS_RECORD/TEST_RECORD/TENV_ID节点格式错误.";
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType + "." +
                    System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool MISCRecordParse(XmlNode misc, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (misc.ChildNodes == null || misc.ChildNodes.Count == 0)
                {
                    errMsg += "ATMS_RECORD/MEAS_RECORD/MISC_RECORD节点格式错误.模板未审批";
                    return false;
                }
                foreach (XmlNode node in misc.ChildNodes)
                {
                    if (node.Name == "CRC32")
                        continue;
                    foreach (XmlNode para in node.ChildNodes)
                    {
                        //foreach (XmlNode para in objectID.ChildNodes)
                        {
                            MISCRecordInfo info = new MISCRecordInfo();
                            info.ObjectValue = node.Attributes["VALUE"].Value.ToString();
                            info.ObjectDesc = node.Attributes["DESC"].Value.ToString();
                            info.Name = para.Attributes["NAME"].Value.ToString();
                            info.Value = para.Attributes["VALUE"].Value.ToString();
                            info.Desc = para.Attributes["DESC"].Value.ToString();
                            MISCInfo.Add(info);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "MISCRecordParse error:" + ex.Message + "\r";
                return false;
            }
        }

        private bool SaveMISCRecord(XmlNode misc, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (misc.ChildNodes == null || misc.ChildNodes.Count == 0)
                {
                    errMsg += "ATMS_RECORD/MEAS_RECORD/MISC_RECORD节点格式错误.模板未审批";
                    return false;
                }
                XmlNode crcNode = null;
                string strCrcXml = "";
                foreach (XmlNode node in misc.ChildNodes)
                {
                    if (node.Name == "CRC32")
                    {
                        crcNode = node;
                        continue;
                    }
                    foreach (XmlNode para in node.ChildNodes)
                    {
                        for (int i = 0; i < MISCInfo.Count; i++)
                        {
                            if (MISCInfo[i].ObjectValue == node.Attributes["VALUE"].Value &&
                                MISCInfo[i].Name == para.Attributes["NAME"].Value)
                            {
                                para.Attributes["VALUE"].Value = MISCInfo[i].Value;
                            }
                        }
                    }
                    strCrcXml += node.OuterXml;
                }
                if (crcNode != null)
                {
                    strCrcXml = strCrcXml.Replace("\r", "");
                    strCrcXml = strCrcXml.Replace("\n", "");
                    strCrcXml = strCrcXml.Replace("\t", "");
                    strCrcXml = strCrcXml.Replace(" ", "");
                    byte[] crcConInner = System.Text.Encoding.UTF8.GetBytes(strCrcXml);
                    UInt32 conRecCrcValue = GetCRC32(crcConInner, crcConInner.Length);
                    crcNode.InnerText = (conRecCrcValue).ToString("X8");
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "MISCRecordParse error:" + ex.Message + "\r";
                return false;
            }
        }

        public bool SaveTestType(string testType)
        {
            MFGInfo.TestType = testType;
            return true;
        }

        public bool SavePermsLevel(string level)
        {
            MFGInfo.PermsLevel = level;
            return true;
        }

        public bool SaveSoftwareInfo(string name,string ver,string author,string date)
        {
            for(int i=0;i<MISCInfo.Count;i++)
            {
                if(MISCInfo[i].ObjectValue== "TestSoftware"&&MISCInfo[i].Name== "SoftwareName")
                {
                    MISCInfo[i].Value = name;
                    recipeInfo.TP_PN = name;
                }
                else if (MISCInfo[i].ObjectValue == "TestSoftware" && MISCInfo[i].Name == "Version")
                {
                    MISCInfo[i].Value = ver;
                    recipeInfo.TP_VER = ver;
                }
                else if (MISCInfo[i].ObjectValue == "TestSoftware" && MISCInfo[i].Name == "ReleaseTime")
                {
                    MISCInfo[i].Value = date;
                    recipeInfo.TP_Date = date;
                }
                else if (MISCInfo[i].ObjectValue == "TestSoftware" && MISCInfo[i].Name == "Author")
                {
                    MISCInfo[i].Value = author;
                    recipeInfo.TP_Author = author;
                }
            }
            return true;
        }

        private bool SaveDataToFile(string fileName, out string errMsg)
        {
            errMsg = "";
            try
            {
                MFGInfo.Operator = userRec;
                MFGInfo.TestStation = stationRec;
                /*if(productInfo.Hold==""|| productInfo.Rework=="")
                {
                    errMsg = "获取产品状态出错！";
                    return false;
                }
                if(productInfo.Hold.ToUpper()=="TRUE"||productInfo.Rework.ToUpper()=="TRUE")
                {
                    errMsg=string.Format("该产品处于Hold状态，不允许上传结果！");
                    return false;
                }
                if (productInfo.Rework.ToUpper() == "TRUE")
                {
                    errMsg = "该产品处于Rework状态，不允许上传结果！";
                    return false;
                }*/
                if (MFGInfo.ProdSN=="")
                {
                    MFGInfo.ProdSN = productInfo.SN;
                }
                if(MFGInfo.ProdProcess=="")
                {
                    MFGInfo.ProdProcess = productInfo.CurProcess;
                }
                if(MFGInfo.WONum=="")
                {
                    MFGInfo.WONum = productInfo.WONum;
                }
                recipeInfo.ProdSN = MFGInfo.ProdSN;
                recipeInfo.ProdProcess = MFGInfo.ProdProcess;
                recipeInfo.TT_PN = docrevInfo.ProdPN;
                recipeInfo.TT_Author = docrevInfo.Author;
                recipeInfo.TT_Date = docrevInfo.ApprovedDate;
                recipeInfo.TT_VER = docrevInfo.Version;
                recipeInfo.TT_Status = "Release";
                if (recipeInfo.ProdSN==""||recipeInfo.ProdProcess==""||MFGInfo.WONum=="")
                {
                    errMsg = "保存数据失败：SN/工序/WO为空！";
                    return false;
                }
                if(MFGInfo.TestStation==""||MFGInfo.Operator=="")
                {
                    errMsg = "保存数据失败：员工账号和测试工位ID不能为空！";
                    return false;
                }
                if(recipeInfo.TP_Author==""|| recipeInfo.TP_Date==""|| recipeInfo.TP_PN==""|| recipeInfo.TP_VER=="")
                {
                    errMsg = "保存数据失败：软件信息不能为空！";
                    return false;
                }

                if(MFGInfo.TestType=="")
                {
                    errMsg = "保存数据失败：MFG_RECORD里的Test_Type不能为空！";
                    return false;
                }

                if (MFGInfo.PermsLevel == "")
                {
                    errMsg = "保存数据失败：MFG_RECORD里的Perms_Level不能为空！";
                    return false;
                }
                if (MFGInfo.MoveIn=="")
                {
                    //MFGInfo.MoveIn = string.Format("{0:yyyyMMddHHmmsss}", loadTemplateTime);
                    MFGInfo.MoveIn = string.Format("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}",
                        loadTemplateTime.Year, loadTemplateTime.Month, loadTemplateTime.Day, loadTemplateTime.Hour, loadTemplateTime.Minute, loadTemplateTime.Second);
                }
                if(MFGInfo.MoveOut=="")
                {
                    DateTime dt = System.DateTime.Now;
                    MFGInfo.MoveOut = string.Format("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", 
                        dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
                }
                MFGInfo.Operator = userRec;
                MFGInfo.TestStation = stationRec;
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(templateConten);
                XmlNode root = doc.SelectSingleNode("ATMS_RECORD");
                XmlNode meas = null;
                if (root == null)
                {
                    root = doc.SelectSingleNode("MEAS_RECORD");
                    meas = root;
                }
                else
                {
                    meas = root.SelectSingleNode("MEAS_RECORD");
                }
                if (root != null)
                {
                    if (meas != null)
                    {
                        XmlNode mfgNode = meas.SelectSingleNode("MFG_RECORD");
                        if (!SaveMFGRecord(mfgNode, out errMsg))
                            return false;
                        XmlNode recipeNode= meas.SelectSingleNode("RECIPE_RECORD");
                        if (!SaveRecipeRecord(recipeNode, out errMsg))
                            return false;
                        XmlNode miscNode= meas.SelectSingleNode("MISC_RECORD");
                        if (!SaveMISCRecord(miscNode, out errMsg))
                            return false;
                        XmlNode test = meas.SelectSingleNode("TEST_RECORD");
                        if (test != null)
                        {
                            for (int i = 0; i < AllTestInfo.Count; i++)
                            {
                                if (AllTestInfo[i].CurValue != CommonFunction.GetDefaultValue())
                                {
                                    XmlNodeList tenvNodes = test.SelectNodes("TENV_ID");
                                    foreach(XmlNode tenvNode in tenvNodes)
                                    {
                                        if(tenvNode.Attributes["VALUE"].Value==AllTestInfo[i].EnvironmentID)
                                        {
                                            bool isTenvPass = true;
                                            XmlNodeList objNodes = tenvNode.SelectNodes("OBJECT");
                                            foreach(XmlNode objNode in objNodes)
                                            {
                                                if(objNode.Attributes["NAME"].Value==AllTestInfo[i].ObjectID)
                                                {
                                                    bool isObjectPass = true;
                                                    XmlNodeList portNodes = objNode.SelectNodes("PORT");
                                                    foreach(XmlNode portNode in portNodes)
                                                    {
                                                        if(portNode.Attributes["VALUE"].Value==AllTestInfo[i].PortID)
                                                        {
                                                            bool isPortPass = true;
                                                            XmlNodeList conNodes = portNode.SelectNodes("COND_ID");
                                                            foreach (XmlNode conNode in conNodes)
                                                            {
                                                                if (conNode.Attributes["VALUE"].Value == AllTestInfo[i].ConditionID)
                                                                {
                                                                    bool isConPass = true;
                                                                    XmlNodeList paraNodes = conNode.SelectNodes("PARA");
                                                                    foreach (XmlNode paraNode in paraNodes)
                                                                    {
                                                                        if (paraNode.Attributes["NAME"].Value == AllTestInfo[i].TestParam.GetMESTemplateKeywords())
                                                                        {
                                                                            if(Math.Abs(AllTestInfo[i].CurValue-CommonFunction.GetDefaultValue())>0.1)
                                                                            {
                                                                                paraNode.Attributes["VALUE"].Value = AllTestInfo[i].CurValue.ToString();
                                                                                if (AllTestInfo[i].Pass)
                                                                                {
                                                                                    paraNode.Attributes["PASSFAIL"].Value = "P";
                                                                                    //isConPass = true;
                                                                                }
                                                                                else
                                                                                {
                                                                                    paraNode.Attributes["PASSFAIL"].Value = "F";
                                                                                    isConPass = false;
                                                                                }
                                                                                paraNode.Attributes["FILENAME"].Value = AllTestInfo[i].Filename;
                                                                                paraNode.Attributes["TEST_DATE"].Value = AllTestInfo[i].TestDate;
                                                                            }
                                                                            
                                                                        }
                                                                    }
                                                                    /*if (isConPass)
                                                                    {
                                                                        conNode.Attributes["PASSFAIL"].Value = "P";
                                                                        //isPortPass = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        isPortPass = false;
                                                                        conNode.Attributes["PASSFAIL"].Value = "F";
                                                                    }*/
                                                                }
                                                            }
                                                            /*if (isPortPass)
                                                            {
                                                                portNode.Attributes["PASSFAIL"].Value = "P";
                                                                //isObjectPass = true;
                                                            }
                                                            else
                                                            {
                                                                isObjectPass = false;
                                                                portNode.Attributes["PASSFAIL"].Value = "F";
                                                            }*/
                                                        }
                                                    }
                                                    /*if (isObjectPass)
                                                    {
                                                        //isTenvPass = true;
                                                        objNode.Attributes["PASSFAIL"].Value = "P";
                                                    }
                                                    else
                                                    {
                                                        isTenvPass = false;
                                                        objNode.Attributes["PASSFAIL"].Value = "F";
                                                    }*/
                                                }
                                            }
                                            /*if(isTenvPass)
                                                tenvNode.Attributes["PASSFAIL"].Value = "P";
                                            else
                                                tenvNode.Attributes["PASSFAIL"].Value = "F";*/
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            errMsg += "保存数据失败，找不到ATMS_RECORD/MEAS_RECORD/TEST_RECORD节点.";
                            return false;
                        }

                    }
                    else
                    {
                        errMsg += "保存数据失败，找不到ATMS_RECORD/MEAS_RECORD节点.";
                        return false;
                    }
                }
                else
                {
                    errMsg += "保存数据失败，找不到ATMS_RECORD节点.";
                    return false;
                }

                string tmpPath = Environment.CurrentDirectory + "\\savetmp.xml";
                if (File.Exists(tmpPath))
                {
                    File.Delete(tmpPath);
                }
                doc.Save(tmpPath);
                StreamReader readsr = new StreamReader(tmpPath);
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                StreamWriter writesr = new StreamWriter(fileName);
                while (readsr.Peek() > 0)
                {
                    string line = readsr.ReadLine();
                    XmlString(ref line);
                    writesr.WriteLine(line);
                }
                readsr.Close();
                writesr.Close();
                
                return true;
            }
            catch (Exception ex)
            {
                errMsg += "SaveData error:" + ex.Message;
                return false;
            }
        }

        public void XmlString(ref string str)
        {
            while (true)
            {
                if (str.Contains("&lt;"))
                {
                    str = str.Substring(0, str.IndexOf("&lt;")) + "<" + str.Substring(str.IndexOf("&lt;") + 4);
                }
                if (str.Contains("&gt;"))
                {
                    str = str.Substring(0, str.IndexOf("&gt;")) + ">" + str.Substring(str.IndexOf("&gt;") + 4);
                }
                if (str.Contains("&amp;"))
                {
                    str = str.Substring(0, str.IndexOf("&amp;")) + "&" + str.Substring(str.IndexOf("&amp;") + 5);
                }
                if (str.Contains("&quot;"))
                {
                    str = str.Substring(0, str.IndexOf("&quot;")) + '"' + str.Substring(str.IndexOf("&quot;") + 6);
                }
                if (str.Contains("&apos;"))
                {
                    str = str.Substring(0, str.IndexOf("&apos;")) + "'" + str.Substring(str.IndexOf("&apos;") + 6);
                }

                if ((!str.Contains("&lt;")) && (!str.Contains("&gt;")) && (!str.Contains("&amp;")) && (!str.Contains("&quot;")) && (!str.Contains("&apos;")))
                    break;
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
                if (nIndex < 0 || AllTestInfo.Count == 0)
                {
                    errMsg = "当前选中行无测试信息！";
                    return null;
                }
                if (AllTestInfo[nIndex].TestParam == MESParam.Default)
                {
                    errMsg = "当前选中行无测试信息！";
                    return null;
                }
                return AllTestInfo[nIndex].Clone();
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
                AllTestInfo.Clear();
            }
        }
        
        public FusionControl Clone()
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return formatter.Deserialize(stream) as FusionControl;
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

                foreach (MESTestInfo info in AllTestInfo)
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
                for (int i = deleteIndexs.Count - 1; i >= 0; i--)
                {
                    if (AllTestInfo.Count > deleteIndexs[i])
                    {
                        AllTestInfo.RemoveAt(deleteIndexs[i]);
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
                foreach (MESTestInfo info in AllTestInfo)
                {
                    string column = info.ParamColumnName;
                    string[] splits = column.Split(separator);
                    if (splits.Length > 0)
                        info.ParamColumnName = splits[0];
                }
            }
        }

        /// <summary>
        /// 列显示名称，将特定字符用其他字符代替
        /// </summary>
        /// <param name="sourceStr">需要被代替的字符</param>
        /// <param name="destStr">代替字符</param>
        public void ColumnReplaceStr(string sourceStr, string destStr)
        {
            lock (lockObj)
            {
                foreach (MESTestInfo info in AllTestInfo)
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
            if (AllTestInfo.Count > index)
            {
                MESTestInfo newTestInfo = new MESTestInfo();
                AllTestInfo.Insert(index, newTestInfo);

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
                if (AllTestInfo.Count == 0)
                    return false;
                foreach (MESTestInfo info in AllTestInfo)
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
        public MESTestInfo UpdateTestData(int nIndex, double dRes, ref bool isPass)
        {
            isPass = true;
            MESTestInfo testInfo = null;
            lock (lockObj)
            {
                if (AllTestInfo.Count <= nIndex)
                {
                    return null;
                }
                AllTestInfo[nIndex].CurValue = Math.Round(dRes,3);
                AllTestInfo[nIndex].Tested = true;

                AllTestInfo[nIndex].Pass = CheckPassOrFail(AllTestInfo[nIndex]);

                testInfo = AllTestInfo[nIndex].Clone();
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
                if (AllTestInfo.Count > nIndex)
                {
                    AllTestInfo[nIndex].ILRef = dILRef;
                    testInfo = AllTestInfo[nIndex].Clone();
                }
            }
            return testInfo;
            //更新到界面
            /*if (testParamShow != null)
            {
                testParamShow.UpdateRefView(nIndex, testInfo);
            }*/
        }

        public MESTestInfo UpdateScanRefStatus(int nIndex, bool isRef)
        {
            MESTestInfo testInfo = null;
            lock (lockObj)
            {
                if (AllTestInfo.Count > nIndex)
                {
                    AllTestInfo[nIndex].IsScanRef = isRef;
                    testInfo = AllTestInfo[nIndex].Clone();
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
                if (AllTestInfo.Count > nIndex)
                {
                    if (AllTestInfo[nIndex].TestParam == MESParam.RL)
                        AllTestInfo[nIndex].RLRef = dRLRef;
                    testInfo = AllTestInfo[nIndex].Clone();
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
                if (allInfo[i].TestParam == MESParam.WDL)
                    continue;
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
                for (int i = 0; i < AllTestInfo.Count; i++)
                {
                    if (!AllTestInfo[i].Tested)
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
                foreach (MESTestInfo info in AllTestInfo)
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
                strWrite += productInfo.ProductPN + "\n";
                foreach (MESTestInfo info in infoArr)
                {
                    strWrite += string.Format("{0:0.000},{1:0.000},{2},{3},{4:0.000},{5:0.000}\n", info.WLLeft, info.WLRight, info.PortNameForUser, info.TestParam, info.ILRef, info.RLRef);
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
                /*if (productInfo.ProductPN != refList[0])
                {
                    errMsg = "当前模板与前一模板不一致，请重新归零！";
                    return false;
                }*/
                //归零数据不完整
                if (AllTestInfo.Count != (refList.Count - 1))
                {
                    errMsg = "归零数据不完整！";
                    return false;
                }
                for (int i = 0; i < refList.Count - 1; i++)
                {
                    string[] strRef = refList[i + 1].Split(',');
                    if (strRef.Length < 5)
                        return false;
                    if (AllTestInfo[i].WLLeft.CompareTo(Convert.ToDouble(strRef[0])) != 0)
                    {
                        errMsg = "当前模板与前一模板不一致，请重新归零！";
                        return false;
                    }
                    if (AllTestInfo[i].WLRight.CompareTo(Convert.ToDouble(strRef[1])) != 0)
                    {
                        errMsg = "当前模板与前一模板不一致，请重新归零！";
                        return false;
                    }
                    if (AllTestInfo[i].PortNameForUser != strRef[2])
                    {
                        errMsg = "当前模板与前一模板不一致，请重新归零！";
                        return false;
                    }
                    if (AllTestInfo[i].TestParam.GetMESTemplateKeywords() != strRef[3])
                    {
                        errMsg = "当前模板与前一模板不一致，请重新归零！";
                        return false;
                    }
                    AllTestInfo[i].ILRef = Convert.ToDouble(strRef[4]);
                    AllTestInfo[i].RLRef = Convert.ToDouble(strRef[5]);
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg = "ReadRefData 出错：" + ex.Message;
                return false;
            }

        }
        
        //public int SaveDataToAMTSByTmpt(string strSN, string strUrl, int tmpt, ref string errMsg, bool bLighted = false, MESRawdataType rawdataType = MESRawdataType.Default, List<AMTSRawdata> allRawdatas = null)
        //{
           
        //}
        
        /// <summary>
        /// 测试结果是否合格
        /// </summary>
        /// <param name="nIndex">需要判断行序号</param>
        /// <returns>是否合格</returns>
        private bool CheckPassOrFail(int nIndex, ref string errMsg)
        {
            lock (lockObj)
            {
                if (AllTestInfo.Count > nIndex)
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
        private bool CheckPassOrFail(MESTestInfo info, bool isStr = false)
        {
            bool bPass = true;
            //如果为空行，则不需要判断是否合格
            //if (info.TestParam != MESParam.Default)
            {
                double dTestValue = info.CurValue;

                if ((dTestValue > Convert.ToDouble(info.Criterion1) || dTestValue < Convert.ToDouble(info.Criterion))
                    && Math.Abs(dTestValue - CommonFunction.GetDefaultValue()) > 0.1)
                {
                    bPass = false;
                }
                /*bPass = MeetCriterion(dTestValue, info.Criterion)
                    & MeetCriterion(dTestValue, info.Criterion1);*/

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
            if (criterion.Substring(0, 1) == "-")
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
