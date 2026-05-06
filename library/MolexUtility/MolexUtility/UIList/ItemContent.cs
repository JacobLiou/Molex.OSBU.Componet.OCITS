using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.UIList
{
    public class ItemContent
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
        public ItemContent()
        {
            TestInfo = null;
            UpdateItemMap = null;
            NextSelectMap = null;
        }
    }
}
