using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Protocol;
using System.Xml;

namespace MolexUtility
{
    public class MsgXmlParser
    {
        public static void GetMsgBase(string msg,ref MsgBaseInfo info)
        {
            if (msg == "")
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(msg);
            //doc.Load(xmlString);
            XmlNode rootNode = doc.SelectSingleNode("OCITS");

            XmlNodeList msgList = rootNode.SelectNodes("Msg");
            if (msgList.Count > 0)
            {
                XmlNode subTypeLine = msgList[0].Attributes["Type"];
                if (subTypeLine != null)
                    info.MsgType = subTypeLine.InnerText;
                XmlNode subTargetType = msgList[0].Attributes["Target"];
                if (subTargetType != null)
                    info.MsgTarget = subTargetType.InnerText;
                XmlNode subSrcType = msgList[0].Attributes["Source"];
                if (subSrcType != null)
                    info.MsgSource = subSrcType.InnerText;
            }

            XmlNodeList operList = rootNode.SelectNodes("Operate");
            if(operList.Count>0)
            {
                info.Operate = operList[0].InnerText;
            }
        }

        public static string GetNodeInner(string msg, string node)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(msg);
            //doc.Load(xmlString);
            XmlNode rootNode = doc.SelectSingleNode("OCITS");

            XmlNodeList nodeList = rootNode.SelectNodes(node);
            if (nodeList.Count > 0)
            {
                return nodeList[0].InnerText;
            }
            return "";
        }

        public static void Ack(MsgBaseInfo info, Dictionary<string,string> nodes,ref XmlStr ackMsg)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<OCITS></OCITS>");
            XmlNode rootNode = doc.SelectSingleNode("OCITS");
            XmlElement msgElement = doc.CreateElement("Msg");
            XmlAttribute attrType = doc.CreateAttribute("Type");
            attrType.InnerText = info.MsgType;
            msgElement.SetAttributeNode(attrType);

            XmlAttribute attrTarget = doc.CreateAttribute("Target");
            attrTarget.InnerText = info.MsgSource;
            msgElement.SetAttributeNode(attrTarget);

            XmlAttribute attrSrc = doc.CreateAttribute("Source");
            attrSrc.InnerText = info.MsgTarget;
            msgElement.SetAttributeNode(attrSrc);
            rootNode.AppendChild(msgElement);

            XmlElement operElement = doc.CreateElement("Operate");
            operElement.InnerText = info.Operate;
            rootNode.AppendChild(operElement);

            if (nodes != null)
            {
                string[] dicKeys = nodes.Keys.ToArray();
                foreach (string dicKey in dicKeys)
                {
                    XmlElement otherElement = doc.CreateElement(dicKey);
                    otherElement.InnerText = nodes[dicKey];
                    rootNode.AppendChild(otherElement);
                }
            }
            doc.AppendChild(rootNode);
            ackMsg.Content = doc.InnerXml;
        }

        public static void MakeMsg(MsgBaseInfo info, Dictionary<string, string> nodes, ref XmlStr ackMsg)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<OCITS></OCITS>");
            XmlNode rootNode = doc.SelectSingleNode("OCITS");
            XmlElement msgElement = doc.CreateElement("Msg");
            XmlAttribute attrType = doc.CreateAttribute("Type");
            attrType.InnerText = info.MsgType;
            msgElement.SetAttributeNode(attrType);

            XmlAttribute attrTarget = doc.CreateAttribute("Target");
            attrTarget.InnerText = info.MsgTarget;
            msgElement.SetAttributeNode(attrTarget);

            XmlAttribute attrSrc = doc.CreateAttribute("Source");
            attrSrc.InnerText = info.MsgSource;
            msgElement.SetAttributeNode(attrSrc);
            rootNode.AppendChild(msgElement);

            XmlElement operElement = doc.CreateElement("Operate");
            operElement.InnerText = info.Operate;
            rootNode.AppendChild(operElement);

            if (nodes != null)
            {
                string[] dicKeys = nodes.Keys.ToArray();
                foreach (string dicKey in dicKeys)
                {
                    XmlElement otherElement = doc.CreateElement(dicKey);
                    otherElement.InnerText = nodes[dicKey];
                    rootNode.AppendChild(otherElement);
                }
            }
            doc.AppendChild(rootNode);
            ackMsg.Content = doc.InnerXml;
        }
    }
}
