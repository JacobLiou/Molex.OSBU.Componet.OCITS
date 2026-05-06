using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using MolexUtility;
using System.Xml;
using System.IO;
namespace ConfigModel
{
    public class ConfigXmlParser
    {
        public static void ParseConfig(string path,out List<string> deviceNameList,out List<List<DeviceConfig>> allDevice)
        {
            deviceNameList = null;
            allDevice = null;
            FileInfo configFileInfo = new FileInfo(path);
            if (configFileInfo.Exists == false)
                return;
            string xmlString = File.ReadAllText(path);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);
            //doc.Load(xmlString);
            XmlNode rootNode = doc.SelectSingleNode("Config");
            XmlNodeList nodeList = rootNode.SelectNodes("DeviceConfig");
            deviceNameList = new List<string>(nodeList.Count);
          
            allDevice = new List<List<DeviceConfig>>(nodeList.Count);
            foreach (XmlNode node in nodeList)
            {
                deviceNameList.Add(node.Attributes["name"].InnerText);              
                XmlNodeList subNodeList = node.SelectNodes("Device");
                List<DeviceConfig> subDevice = new List<DeviceConfig>(subNodeList.Count);
                foreach (XmlNode detailNode in subNodeList)
                {
                    DeviceConfig deviceInfo = new DeviceConfig();
                    XmlNode subNode = detailNode.Attributes["name"];
                    if (subNode != null)
                        deviceInfo.ShowName = subNode.InnerText;
                    subNode = detailNode.SelectSingleNode("channel");
                    if (subNode != null)
                        deviceInfo.ChannelCount = subNode.InnerText;
                    subNode = detailNode.SelectSingleNode("Type");
                    if (subNode != null)
                        deviceInfo.ControlName = subNode.InnerText;
                    subNode = detailNode.SelectSingleNode("check");
                    if (subNode != null)
                        deviceInfo.CheckCmd = subNode.InnerText;
                    for (int i=0;i<deviceInfo.ControlMaxCount;i++)
                    {
                        subNode = detailNode.SelectSingleNode("control" + i.ToString());
                        if (subNode != null)
                        {
                            deviceInfo.Control[i] = subNode.InnerText;
                            XmlNode lastNode = subNode.Attributes["name"];
                            if(lastNode!=null)
                                deviceInfo.ControlKey[i] = lastNode.InnerText;
                        }
                    }
                    subDevice.Add(deviceInfo);
                }
                allDevice.Add(subDevice);
            }
            
        }

        public static void SaveConfig(string path, List<string> deviceName,List<List<DeviceConfig>> allConfig)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode node = doc.CreateXmlDeclaration("1.0", "gb2312",null);
            doc.AppendChild(node);
            XmlNode rootNode = doc.CreateElement("Config");
            doc.AppendChild(rootNode);
            for(int i=0;i< allConfig.Count;i++)
            {
                if(allConfig[i].Count!=0)
                {
                    XmlNode subNode = doc.CreateElement("DeviceConfig");
                    XmlAttribute attriNode = doc.CreateAttribute("name");
                    attriNode.InnerText = deviceName[i];
                    subNode.Attributes.Append(attriNode);
                   
                    rootNode.AppendChild(subNode);
                    AddNode(doc, subNode, allConfig[i]);
                }
            }
            string saveXml = doc.InnerXml; 
            File.WriteAllText(path, saveXml,Encoding.UTF8);
            //doc.Save(path);
        }

        private static void AddNode(XmlDocument xmlDoc,XmlNode parentNode, List<DeviceConfig> configList)
        {
            foreach(DeviceConfig config in configList)
            {
                XmlNode node = xmlDoc.CreateElement("Device");
                parentNode.AppendChild(node);
                if (config.ControlName != "")
                {
                    CreateNode(xmlDoc, node, "Type", config.ControlName,"");
                }
                if(config.ChannelCount!="")
                {
                    CreateNode(xmlDoc, node, "channel", config.ChannelCount,"");
                }
                for (int i = 0; i < config.ControlMaxCount; i++)
                {
                    if (config.Control[i] != "")
                    {
                        string nodeName = "control" + i.ToString();
                        CreateNode(xmlDoc, node, nodeName, config.Control[i],config.ControlKey[i]);
                    }
                }         
             }
        }

        private static void CreateNode(XmlDocument xmlDoc, XmlNode parentNode,string name,string value,string valueKey)
        {
            XmlNode subNode = xmlDoc.CreateNode(XmlNodeType.Element, name, null);
            subNode.InnerText = value;
            if(valueKey!=null&& valueKey.Length>0)
            {
                
                XmlAttribute attriNode = xmlDoc.CreateAttribute("name");
                attriNode.InnerText = valueKey;
                subNode.Attributes.Append(attriNode);
            }           
            parentNode.AppendChild(subNode);
        }
    }

    public class DeviceConfig
    {       
        public string ShowName { get; set; }
        public string ChannelCount { get; set; }
        public string ControlName { get; set; }
        public int ControlMaxCount;
        public string CheckCmd { get; set; }
        public string AckData { get; set; }
        public string[] Control { get; set; }

        public string[] ControlKey { get; set; }
        public DeviceConfig()
        {
            ControlMaxCount = 100;// Properties.Settings.Default.ControlMaxCount;
            Control = new string[ControlMaxCount];
            ControlKey=new string[ControlMaxCount];
            ShowName = "";
            ChannelCount = "";
            ControlName = "";
            for (int i = 0; i < ControlMaxCount; i++)
            {
                Control[i] = "";
                ControlKey[i] = "";
            }
            CheckCmd = "";
            AckData = "";
        }

        public DeviceConfig Clone()
        {
            DeviceConfig config = new DeviceConfig();
            CopyTo(ref config);
            return config;
        }
        public void CopyTo(ref DeviceConfig config)
        {
            config.ShowName = ShowName;           
            config.ChannelCount = ChannelCount;
            config.ControlName = ControlName;
            for (int i = 0; i < config.ControlMaxCount; i++)
            {
                config.Control[i] = Control[i];
                config.ControlKey[i] = ControlKey[i];
            }
            config.CheckCmd = CheckCmd;
            config.AckData = AckData;
        }
    }

    
}
