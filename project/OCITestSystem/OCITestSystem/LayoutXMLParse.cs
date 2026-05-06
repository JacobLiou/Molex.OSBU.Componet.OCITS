using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows;

///<summary>
///文件名：LayoutXMLParse.cs
///作用：主界面布局配置文件解析模块
///作者：阮锦芳
///编写日期：2018-02-26
///修改记录
///</summary>
namespace OCITestSystem
{
    
    public class LayoutXMLParser
    {
        /// <summary>
        /// 解析xml配置文件
        /// </summary>
        /// <param name="path">配置文件路径</param>
        /// <param name="rowDefs">布局行信息</param>
        /// <param name="columnDefs">布局列信息</param>
        /// <param name="panelChilds">各自模块信息</param>
        public static void ParseConfig(string path, ref List<GridLength> rowDefs, ref List<GridLength> columnDefs, ref List<PanelConfige> panelChilds)
        {
            FileInfo configFileInfo = new FileInfo(path);
            if (configFileInfo.Exists == false)
                return;
            string xmlString = File.ReadAllText(path);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);
            //doc.Load(xmlString);
            XmlNode rootNode = doc.SelectSingleNode("Grid");
            //分行信息解析
            XmlNodeList rowList = rootNode.SelectNodes("Grid.RowDefinitions");
            foreach (XmlNode node in rowList)
            {
                XmlNodeList subNodeList = node.SelectNodes("RowDefinition");
                foreach (XmlNode detailNode in subNodeList)
                {
                    GridLength layout;
                    XmlNode subNode = detailNode.Attributes["Height"];
                    string height = "";
                    if (subNode != null)
                        height = subNode.InnerText;
                    ParseToGridUnitType(height, out layout);
                    rowDefs.Add(layout);
                }
            }

            //分列信息解析
            XmlNodeList columnList = rootNode.SelectNodes("Grid.ColumnDefinitions");
            foreach (XmlNode node in columnList)
            {
                XmlNodeList subNodeList = node.SelectNodes("ColumnDefinition");
                foreach (XmlNode detailNode in subNodeList)
                {
                    GridLength layout;
                    XmlNode subNode = detailNode.Attributes["Width"];
                    string height = "";
                    if (subNode != null)
                        height = subNode.InnerText;
                    ParseToGridUnitType(height, out layout);
                    columnDefs.Add(layout);
                }
            }

            //模块布局信息解析
            XmlNodeList panelList = rootNode.SelectNodes("DockPanel");
            foreach (XmlNode node in panelList)
            {
                PanelConfige panel = new PanelConfige();
                XmlNode subNode = node.Attributes["Grid.Row"];
                if (subNode != null)
                    panel.Row = Convert.ToInt32(subNode.InnerText);
                subNode = node.Attributes["Grid.Column"];
                if (subNode != null)
                    panel.Column = Convert.ToInt32(subNode.InnerText);
                subNode = node.Attributes["Grid.ColumnSpan"];
                if (subNode != null)
                    panel.ColumnSpan = Convert.ToInt32(subNode.InnerText);
                subNode = node.Attributes["Grid.RowSpan"];
                if (subNode != null)
                    panel.RowSpan = Convert.ToInt32(subNode.InnerText);
                if (panel.Row == -1 && panel.RowSpan > 1)
                    panel.Row = 0;
                if (panel.Column == -1 && panel.ColumnSpan > 1)
                    panel.Column = 0;
                
                subNode = node.Attributes["Module"];
                if (subNode != null)
                    panel.ModuleName = subNode.InnerText;

                subNode = node.Attributes["Name"];
                if (subNode != null)
                    panel.Name = subNode.InnerText;

                subNode = node.Attributes["Index"];
                if (subNode != null)
                    panel.ModuleIndex = Convert.ToInt32(subNode.InnerText);
                panelChilds.Add(panel);
            }
        }

        /// <summary>
        /// 将字符转为GridUnitType
        /// </summary>
        /// <param name="source">需要转换的字符</param>
        /// <param name="length">转换的GridUnitType结果</param>
        public static void ParseToGridUnitType(string source, out GridLength length)
        {

            bool isStar = false;
            foreach (char ch in source)
            {
                if (!char.IsNumber(ch))
                    isStar = true;
            }
            if (source.Length == 1 && isStar)
            {
                length = new GridLength(1, GridUnitType.Auto);
            }
            else if (source.Length > 0 && isStar)
            {
                string value = source.Remove(source.Length - 1);
                length = new GridLength(Convert.ToInt32(value), GridUnitType.Star);
            }
            else if (source.Length > 0 && isStar == false)
            {
                length = new GridLength(Convert.ToInt32(source), GridUnitType.Pixel);
            }
            else
                length = new GridLength(1, GridUnitType.Auto);

        }

        /// <summary>
        /// 解析软件号和版本，用于界面显示
        /// </summary>
        /// <param name="path">配置文件路径</param>
        /// <param name="softID">软件号</param>
        /// <param name="version">软件版本s</param>
        public static void ParseSoftIDAndVersion(string path, ref string softID,ref string version, ref string softName,ref string useUDL)
        {
            FileInfo configFileInfo = new FileInfo(path);
            if (configFileInfo.Exists == false)
                return;
            string xmlString = File.ReadAllText(path);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);
            //doc.Load(xmlString);
            XmlNode rootNode = doc.SelectSingleNode("Grid");
            
            //分列信息解析
            XmlNodeList columnList = rootNode.SelectNodes("Software");
            //如果有多个Software存在，则解析第一个，原则上只有一个
            foreach (XmlNode node in columnList)
            {
                XmlNode subNode = node.Attributes["ID"];
                if (subNode != null)
                    softID = subNode.InnerText;
                subNode = node.Attributes["Version"];
                if (subNode != null)
                    version = subNode.InnerText;
                subNode = node.Attributes["UDL"];
                if (subNode != null)
                    useUDL = subNode.InnerText;
                subNode = node.Attributes["Name"];
                if (subNode != null)
                    softName = subNode.InnerText;
                break;
            }
        }
    }
}
