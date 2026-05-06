using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.UIList
{
    public class ItemDemuxContent
    {
        /// <summary>
        /// 需要更新行内容
        /// </summary>
        public MESTestInfo TestInfo { get; set; }

        /// <summary>
        /// 需要更新行对应的产品以及测试项所对应的index
        /// </summary>
        public IndexMap UpdateItemMap { get; set; }

        /// <summary>
        /// 自动跳转到选中行号，如果为-1，则不跳转
        /// </summary>
        public IndexMap NextSelectMap { get; set; }

        public double Offset1 { get; set; }
        public double Offset2 { get; set; }
        public double Offset3 { get; set; }
        public ItemDemuxContent()
        {
            TestInfo = null;
            UpdateItemMap = null;
            NextSelectMap = null;
            Offset1 = CommonFunction.GetDefaultValue();
            Offset2 = CommonFunction.GetDefaultValue();
            Offset3 = CommonFunction.GetDefaultValue();
        }
    }
}
