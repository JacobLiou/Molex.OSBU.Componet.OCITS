using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Protocol
{
    public class MainInitInfo
    {
        /// <summary>
        /// 产线名称
        /// </summary>
        public string ProductLine { get; set; }

        /// <summary>
        /// 工位类型
        /// </summary>
        public string StationType { get; set; }

        /// <summary>
        /// 工位ID
        /// </summary>
        public string StationID { get; set; }

        /// <summary>
        /// exe路径
        /// </summary>
        public string ExePath { get; set; }

        /// <summary>
        /// 模板类型，1830、EVOA等，与MESTemplateType类型里面的MESSaveDataKeywords属性一致，即保存数据时模板类型
        /// </summary>
        public string TemplateType { get; set; }

        /// <summary>
        /// 测试工序，例如preadjust、adjust等，与MESTestProcess中Additional一致
        /// </summary>
        public string TestProcess { get; set; }

        /// <summary>
        /// 用户登录ID
        /// </summary>
        public string UserID { get; set; }

        public string Goldsample { get; set; }

        public string LoginMode { get; set; }

        public string MESMode { get; set; }

        public string SoftwareID { get; set; }

        public string CheckUser { get; set; }

        public string CheckPSW { get; set; }

        public bool DeviceInitRes { get; set; }
        /// <summary>
        /// 0--不连接自动化  1--自动化为服务器
        /// </summary>
        public int AutomationType { get; set; }

        public MainInitInfo()
        {
            ProductLine = "";
            StationType = "";
            StationID = "";
            ExePath = "";
            TemplateType = "";
            TestProcess = "";
            UserID = "";
            Goldsample = "";
            AutomationType = 0;
            SoftwareID = "";
            MESMode = "";
            LoginMode = "";
            CheckUser = "";
            CheckPSW = "";
            DeviceInitRes = true;
        }
    }
}
