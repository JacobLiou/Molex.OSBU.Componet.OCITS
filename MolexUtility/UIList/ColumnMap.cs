using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.UIList
{
    /// <summary>
    /// 显示有特殊要求时，对应的列名称和值
    /// </summary>
    public class ColumnMap
    {
        /// <summary>
        /// 列名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 列显示的内容
        /// </summary>
        public string Value { get; set; }
    }
}
