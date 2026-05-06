using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OCITestSystem
{
    public class StationShowConfig
    {
        public string ProdoctLine;
        public List<SingleStationConfig> Stations;
        public StationShowConfig()
        {
            ProdoctLine = "";
            Stations = new List<SingleStationConfig>();
        }
    }

    public class SingleStationConfig:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 工位名称，用来调用molude_工位名称.xml文件，决定使用那些模块
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 图片名称，暂时未用
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 模板类型，1830、EVOA等，与MESTemplateType类型里面的MESSaveDataKeywords属性一致，即保存数据时模板类型
        /// </summary>
        public string TemplateType { get; set; }

        /// <summary>
        /// 测试工序，例如preadjust、adjust等，与MESTestProcess中Additional一致
        /// </summary>
        public string TestProcess { get; set; }

        
        public string Goldsample { get; set; }

        public string MainDllPath { get; set; }

        public string Automation { get; set; }

        private bool isSelected;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
            }
        }
        public SingleStationConfig()
        {
            Name = "";
            Icon = "";
            TemplateType = "";
            TestProcess = "";
            Goldsample = "";
            IsSelected = false;
            MainDllPath = "";
            Automation = "0";
        }
    }
}
