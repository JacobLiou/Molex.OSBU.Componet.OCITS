using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///<summary>
///文件名：PanelConfige.cs
///作用：每个模块的界面分布信息
///作者：阮锦芳
///编写日期：2018-02-26
///修改记录
///</summary>
namespace OCITestSystem
{
    public class PanelConfige
    {
        /// <summary>
        /// 起始行
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// 起始列
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// 占用多少行
        /// </summary>
        public int RowSpan { get; set; }

        /// <summary>
        /// 占用多少列
        /// </summary>
        public int ColumnSpan { get; set; }

        /// <summary>
        /// 需要显示的模块名称
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// 每个实例化对象名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 同一模块index必须不一样
        /// </summary>
        public int ModuleIndex { get; set; }
        public PanelConfige()
        {
            Row = -1;
            Column = -1;
            RowSpan = -1;
            ColumnSpan = -1;
            ModuleName = "";
            Name = "";
            ModuleIndex = 0;
        }
    }
}
