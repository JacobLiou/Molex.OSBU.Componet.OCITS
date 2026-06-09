using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MSXML2;   //使用msxml v6.0
using System.IO;
using System.Text.RegularExpressions;


namespace MolexUtility
{
    public class CommonFunction
    {
        private static string xmlSetPath = "\\set\\XMLSet.ini";
        private static string xmlSetSection = "XML Set";
        private static string xmlSetKey = "Address";
        private static string saveWebservicSection = "Webservice Set";
        private static string saveProductFamilyKey = "ProductFamilyPath";
        //private static string refDataFile = "\\temple\\RefData.csv";

        private static string templateConfig = "\\set\\template.ini";
        private static string testProcessSection = "TestProcess";
        private static string processSection = "TestProcess";
        private static string processUserKey = "UserProcess";
        private static string processAMTSKey = "AMTSProcess";
        private static string currentProcessKey = "CurProcess";
        private static string templateTypeSection = "TemplateType";
        private static string currentTemplateKey = "CurType";



        public static string GetXmlSetPath()
        {
            return xmlSetPath;
        }
        public static string GetXmlSetSection()
        {
            return xmlSetSection;
        }

        public static string GetXmlSetKey()
        {
            return xmlSetKey;
        }

        public static string GetBasePathKey()
        {
            return saveProductFamilyKey;
        }
        public static string GetSaveWebservicSection()
        {
            return saveWebservicSection;
        }
        public static string GetTemplateConfig()
        {
            return templateConfig;
        }

        public static string GetTestProcessSection()
        {
            return testProcessSection;
        }

        public static string GetProcessUserKey()
        {
            return processUserKey;
        }

        public static string GetProcessAMTSKey()
        {
            return processAMTSKey;
        }

        public static string GetCurrentProcessKey()
        {
            return currentProcessKey;
        }

        public static string GetTemplateTypeSection()
        {
            return templateTypeSection;
        }

        public static string GetCurrentTemplateKey()
        {
            return currentTemplateKey;
        }

        public static string GetProcessSection()
        {
            return processSection;
        }
        /// <summary>
        /// 无纸化未设置参数默认值为-9999.9999
        /// </summary>
        private static double defaultValue = -9999.9999;

        /// <summary>
        /// 获取参数defaultValue
        /// </summary>
        /// <returns>defaultValue值</returns>
        public static double GetDefaultValue()
        {
            return defaultValue;
        }

        /// <summary>
        /// defaultValue第四位小数四舍五入后值
        /// </summary>
        /// <returns>defaultValue第四位小数四舍五入后值</returns>
        public static double GetFormatDefaultValue()
        {
            string strDefault = defaultValue.ToString("#0.000");
            return Convert.ToDouble(strDefault);
        }

        public static bool IsDefault(double value)
        {
            if ((value.CompareTo(Math.Abs(GetDefaultValue())) == 0) || (value.CompareTo(Math.Abs(GetFormatDefaultValue())) == 0))
                return true;
            return false;
        }

        /// <summary>
        /// 根据节点名称获取内容
        /// </summary>
        /// <param name="strAddress">地址</param>
        /// <param name="strElementName">元素名称</param>
        /// <param name="strNodeName">需要获取的节点名称</param>
        /// <param name="strNodeContent">获取到的节点内容</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>是否出错，如果出错，从errMsg处获取出错信息</returns>
        public static bool GetNodeContentByName(string strAddress, string strElementName, string[] strNodeName, out string[] strNodeContent, out string errMsg)
        {
            strNodeContent = new string[strNodeName.Length];
            errMsg = "";
            try
            {
                //为了防止浏览器缓存，每次输入的地址不一致，才会重新加载
                strAddress += "&?param=" + DateTime.Now;
                XMLHTTP60 xmlHttp = new XMLHTTP60();
                xmlHttp.open("get", strAddress, false, null, null);
                xmlHttp.send(null);
                string strRes = xmlHttp.responseText;
                XmlDocument xmlParser = new XmlDocument();
                xmlParser.LoadXml(strRes);
                XmlNodeList pNode;
                //"AutoTemplate"
                pNode = xmlParser.GetElementsByTagName(strElementName);
                if (pNode.Count > 0)
                {
                    XmlElement xe = (XmlElement)pNode.Item(0);
                    XmlNodeList pNode2 = xe.ChildNodes;
                    foreach (XmlNode xn in pNode2)
                    {
                        for (int i = 0; i < strNodeName.Length; i++)
                        {
                            if (xn.LocalName == "Error")
                            {
                                errMsg = xn.InnerText;
                                if(errMsg.Length>0)
                                    return false;
                            }
                            else if (xn.LocalName == strNodeName[i])
                                strNodeContent[i] = xn.InnerText;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = "GetNodeContentByName 出错："+ex.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Receives string and returns the string with its letters reversed.
        /// </summary>
        public static string ReverseString(string s)
        {
            char[] arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        public static bool IsNumber(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            const string pattern = "^[0-9]*$";
            Regex rx = new Regex(pattern);
            return rx.IsMatch(s);
        }

        public static bool IsNumber(char s)
        {
            string str = string.Format("{0}", s);
            if (string.IsNullOrWhiteSpace(str)) return false;
            const string pattern = "^[0-9]*$";
            Regex rx = new Regex(pattern);
            
            return rx.IsMatch(str);
        }

        /// <summary>
        /// 获取最大值
        /// </summary>
        /// <param name="dArr">原始数据</param>
        /// <param name="dMax">数据最大值</param>
        /// <param name="dMin">数据最小值</param>
        public static void GetMaxMin(double[] dArr, out double dMax, out double dMin)
        {
            double dOutMax = -Math.Abs(GetDefaultValue());
            double dOutMin = Math.Abs(GetDefaultValue());
            dMax = dOutMax;
            dMin = dOutMin;
            if (dArr == null || dArr.Length == 0)
            {
                return;
            }
            foreach (double dvalue in dArr)
            {
                if (dMax < dvalue)
                    dMax = dvalue;
                if (dMin > dvalue)
                    dMin = dvalue;
            }
        }

        public static void WriteFile(string path ,string strWriteContent)
        {  
            FileInfo fi = new FileInfo(path);
            var di = fi.Directory;
            if (!di.Exists)
                di.Create();
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs,Encoding.Unicode);
            //开始写入
            sw.Write(strWriteContent);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
            
        }


        public static void WriteFileASCII(string path, string strWriteContent)
        {
            FileInfo fi = new FileInfo(path);
            var di = fi.Directory;
            if (!di.Exists)
                di.Create();
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.ASCII);
            //开始写入
            sw.Write(strWriteContent);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();

        }

        /// <summary>
        /// 将数据按内存的方式强制转换为double类型
        /// </summary>
        /// <param name="bArr">需要转换的数据</param>
        /// <returns>转换后的double值</returns>
        public static double Memorytofloat(byte[] bArr)
        {
            if (bArr.Length != 4)
                return 0;
            string binaryStr = "";
            foreach (byte b in bArr)
            {
                string str = Convert.ToString(b, 2);
                binaryStr += str.PadLeft(8, '0');
            }
            //float 32位保存，1-符号位 2-9 共8位2的指数位（e-127） 剩余23位小数点位
            int sign = Convert.ToInt32(binaryStr.Remove(1, binaryStr.Length - 1));

            //解析指数位的值
            string eStr = binaryStr.Remove(0, 1);
            eStr = eStr.Remove(8, eStr.Length - 8);
            int e = Convert.ToInt32(eStr, 2);
            e -= 127;

            string maintain = "1";
            maintain += binaryStr.Remove(0, 9);

            //2进制的e次，即左右移位

            //小数中的整数
            string strInter = "";
            //小数
            string strDecimal = ""; 
            if (e >= 0)
            {
                strInter = maintain.Remove(e + 1, maintain.Length - e - 1);
                strDecimal = maintain.Remove(0, e + 1);
            }
            else if (e == -1)
            {
                strInter = "0";
                strDecimal = maintain;
            }
            else
            {
                strInter = "0";
                for (int i = 1; i < -e; i++)
                    strDecimal += "0";
                strDecimal += maintain;
            }

            //将二进制小数转换为10进制算法
            char[] ch1 = new char[strInter.Length];
            char[] ch2 = new char[strDecimal.Length];
            strInter.CopyTo(0, ch1, 0, strInter.Length);
            strDecimal.CopyTo(0, ch2, 0, strDecimal.Length);

            double dRes = 0;
            for (int i = 0; i < ch1.Length; i++)
            {
                dRes += Convert.ToInt32(ch1[i].ToString()) * Math.Pow(2, ch1.Length - i - 1);
            }

            for (int i = 0; i < ch2.Length; i++)
            {
                dRes += Convert.ToInt32(ch2[i].ToString()) * Math.Pow(2, -i - 1);
            }
            dRes = dRes * Math.Pow(-1, sign);

            return dRes;
        }

        /// <summary>
        /// 获取无纸化所有用户信息
        /// </summary>
        /// <param name="serverAddress">服务器地址</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>获取的所有用户信息，里层的List[0]:账号，List[1]密码</returns>
        public static List<List<string>> GetUserData(string serverAddress,ref string errMsg)
        {
            try
            {      
                //下载用户信息  
                string strAddress = serverAddress;
                strAddress = strAddress + "sys_getuser.aspx";
                XMLHTTP60 xmlHttp = new XMLHTTP60();
                xmlHttp.open("get", strAddress, false, null, null);
                xmlHttp.send(null);
                string strRes = xmlHttp.responseText;
                XmlDocument xmlParser = new XmlDocument();
                xmlParser.LoadXml(strRes);
                XmlNodeList pNode;
                pNode = xmlParser.GetElementsByTagName("UserData");
                if(pNode.Item(0)==null)
                {
                    errMsg = "获取用户信息为空";
                    return null;
                }
                //解析用户信息，将数据放入List
                string unAnalyzeDatas= pNode.Item(0).InnerText;
                unAnalyzeDatas=unAnalyzeDatas.Replace("\r\n", "\n");
                List<List<string>> userAccouts = new List<List<string>>();
                string[] users = unAnalyzeDatas.Split('\n');
                foreach(string user in users)
                {
                    string[] details = user.Split(',');
                    if (details.Length < 2)
                        continue;
                    List<string> singleUser = new List<string>();
                    singleUser.Add(details[0]);
                    singleUser.Add(details[1]);
                    userAccouts.Add(singleUser);
                }
                return userAccouts;

            }
            catch (Exception ex)
            {
                errMsg="获取用户信息出错，" + ex.InnerException.Message;
                return null;
            }
        }


        private static object objLock = new object();

        private static string GetExeDir()
        {
            try
            {
                string curPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                return Path.GetDirectoryName(curPath) ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 日志目录：优先 exe\Log，其次 CurrentDirectory\Log，最后 %TEMP%\OCITestSystem\Log。
        /// </summary>
        private static IEnumerable<string> GetLogDirectoryCandidates()
        {
            yield return Path.Combine(GetExeDir(), "Log");
            yield return Path.Combine(Environment.CurrentDirectory, "Log");
            yield return Path.Combine(Path.GetTempPath(), "OCITestSystem", "Log");
        }

        /// <summary>
        /// 记录log
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>0--成功，1--失败</returns>
        public static int WriteLog(string content,string logPath="")
        {
            try
            {
                DateTime currDate = DateTime.Now;
                string temp = "时间：" + DateTime.Now.ToString() + "\tTickCount=" + Environment.TickCount + "\r\n" + content + "\r\n";
                lock (objLock)
                {
                    if (!string.IsNullOrEmpty(logPath))
                    {
                        File.AppendAllText(logPath, temp, Encoding.UTF8);
                        return 0;
                    }

                    string fileName = currDate.Year.ToString() + "-" + currDate.Month.ToString().PadLeft(2, '0') + "-" + currDate.Day.ToString().PadLeft(2, '0') + "log.txt";
                    foreach (string logDir in GetLogDirectoryCandidates())
                    {
                        if (string.IsNullOrEmpty(logDir))
                            continue;
                        try
                        {
                            Directory.CreateDirectory(logDir);
                            File.AppendAllText(Path.Combine(logDir, fileName), temp, Encoding.UTF8);
                            return 0;
                        }
                        catch
                        {
                        }
                    }
                }
                return 1;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        /// <summary>
        /// 删除旧log文件
        /// </summary>
        /// <param name="currDate">当前日期</param>
        private static void DeleteOldLogFile(DateTime currDate)
        {
            lock (objLock)
            {
                string filePath = Environment.CurrentDirectory + "\\Log";
                if (Directory.Exists(filePath))
                {
                    foreach (string file in Directory.GetFiles(filePath))
                    {
                        DateTime fileDate = Convert.ToDateTime(Path.GetFileNameWithoutExtension(file).Substring(0, 10));
                        TimeSpan timeSpan = currDate - fileDate;
                        if (timeSpan > new TimeSpan(168, 0, 0))
                        {
                            if (File.Exists(file))
                                File.Delete(file);
                        }
                    }
                }
            }
        }

        /*public static T ConverToEnum<T>(string attibute)
        {
            T enumValue;
            return enumValue;
        }*/
    }
}
