using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MSXML2;
using System.IO;

namespace LibTest
{
    public class CommonFunction
    {
        private static double m_dDefaultValue = -9999.9999;

        public static double GetDefaultValue()
        {
            return m_dDefaultValue;
        }

        public static double GetFormatDefaultValue()
        {
            string strDefault = m_dDefaultValue.ToString("#0.000");
            return Convert.ToDouble(strDefault);
        }

        //
        //摘要:根据节点名称获取内容
        //
        //参数：
        //  strAddress:地址
        //  strElementName:元素名称
        //  strNodeName:需要获取的节点名称
        //  strNodeContent:获取到的节点内容
        //  errMsg:出错信息
        //
        //结果：
        //  bool：是否出错，如果出错，从errMsg处获取出错信息
        public static bool GetNodeContentByName(string strAddress, string strElementName, string[] strNodeName, out string[] strNodeContent, out string errMsg)
        {
            strNodeContent = new string[strNodeName.Length];
            errMsg = "";
            try
            {
                XMLHTTP40 xmlHttp = new XMLHTTP40();
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
                errMsg = ex.Message;
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

        public static void GetMaxMin(double[] dArr, out double dMax, out double dMin)
        {
            double dOutMax = -GetDefaultValue();
            double dOutMin = Math.Abs(GetDefaultValue());
            dMax = dOutMax;
            dMin = dOutMin;
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
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            sw.Write(strWriteContent);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }

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
            //float 32位保存，1-符号位 2-9 共8位2的指数为（e-127） 剩余23位小数点位
            int s = Convert.ToInt32(binaryStr.Remove(1, binaryStr.Length - 1));
            string eStr = binaryStr.Remove(0, 1);
            eStr = eStr.Remove(8, eStr.Length - 8);
            int e = Convert.ToInt32(eStr, 2);
            e -= 127;

            string maintain = "1";
            maintain += binaryStr.Remove(0, 9);

            string strInter = "";
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
            dRes = dRes * Math.Pow(-1, s);

            return dRes;
        }
    }
}
