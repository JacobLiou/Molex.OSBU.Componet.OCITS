using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility;

namespace MolexUtility.UIList
{
    /// <summary>
    /// list显示所包含的所有信息
    /// </summary>
    public class ShowContent
    {
        /// <summary>
        /// 产品测试信息
        /// </summary>
        public List<MESTestInfo> TestInfo { get; set; }

        /// <summary>
        /// 除了测试项以外还需要增加在列表的显示内容
        /// </summary>
        public List<ColumnMap> Addition { get; set; }
        public ShowContent()
        {
            Addition = new List<ColumnMap>();
        }
    }
}
