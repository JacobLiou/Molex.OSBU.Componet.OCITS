using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows;

namespace OCITSAutoUpdate
{
    public class StationXMLParser
    {
        /// <summary>
        /// 解析工位类型配置文件
        /// </summary>
        /// <param name="path">配置文件路径</param>
        /// <param name="stations">所有工位信息</param>
        ///  <param name="errMsg">错误信息</param>
        public static void GetAllStations(string path,ref List<StationShowConfig> stations,ref string errMsg)
        {
            try
            {
                FileInfo configFileInfo = new FileInfo(path);
                if (configFileInfo.Exists == false)
                    return;
                string xmlString = File.ReadAllText(path, Encoding.Default);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlString);
                //doc.Load(xmlString);
                XmlNode rootNode = doc.SelectSingleNode("Stations");

                XmlNodeList recList = rootNode.SelectNodes("Record");
                string recLine = "";
                string recStation = "";
                if (recList.Count > 0)
                {
                    XmlNode subRecLine = recList[0].Attributes["line"];
                    if (subRecLine != null)
                        recLine = subRecLine.InnerText;
                    XmlNode subRecType = recList[0].Attributes["type"];
                    if (subRecType != null)
                        recStation = subRecType.InnerText;
                }
                //分行信息解析
                XmlNodeList lineList = rootNode.SelectNodes("Productline");
                foreach (XmlNode node in lineList)
                {
                    //解析生产线信息
                    StationShowConfig singleLine = new StationShowConfig();
                    XmlNode subLine = node.Attributes["name"];
                    if (subLine != null)
                        singleLine.ProdoctLine = subLine.InnerText;
                    XmlNodeList subNodeList = node.SelectNodes("Stationtype");
                    //每条生产线需要的工位类型
                    foreach (XmlNode detailNode in subNodeList)
                    {
                        XmlNode subNode = detailNode.Attributes["name"];
                        if (subNode != null)
                        {
                            SingleStationConfig config = new SingleStationConfig();
                            config.Name = subNode.InnerText;
                            subNode = detailNode.Attributes["TemplateType"];
                            if (subNode != null)
                            {
                                config.TemplateType = subNode.InnerText;
                            }

                            subNode = detailNode.Attributes["TestProcess"];
                            if (subNode != null)
                            {
                                config.TestProcess = subNode.InnerText;
                            }
                            //判断是否选中
                            if (recLine == singleLine.ProdoctLine && recStation == config.Name)
                                config.IsSelected = true;
                            singleLine.Stations.Add(config);
                        }

                    }
                    stations.Add(singleLine);
                }
            }
            catch(Exception ex)
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
            }
        }

        /// <summary>
        /// 保存选择工位
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="line">选择产线</param>
        /// <param name="station">选择工位类型</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>true-成功 false-出错</returns>
        public static bool RecordSelectedStation(string path, string line, string station, ref string errMsg)
        {
            try
            {
                FileInfo configFileInfo = new FileInfo(path);
                if (configFileInfo.Exists == false)
                    return false;
                string xmlString = File.ReadAllText(path, Encoding.Default);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlString);
                //doc.Load(xmlString);
                XmlNode rootNode = doc.SelectSingleNode("Stations");

                XmlNodeList recList = rootNode.SelectNodes("Record");
                if (recList.Count > 0)
                {
                    XmlNode subRecLine = recList[0].Attributes["line"];
                    if (subRecLine != null)
                        subRecLine.InnerText = line;
                    XmlNode subRecType = recList[0].Attributes["type"];
                    if (subRecType != null)
                        subRecType.InnerText = station;
                }

                string saveXml = doc.InnerXml;
                File.WriteAllText(path, saveXml, Encoding.Default);
                return true;    
            }
            catch(Exception ex)             
            {
                errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
        }
    }
            
}
