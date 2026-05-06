using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.UIList;

///<summary>
///文件名：EventListSelectChanged
///作用：列表选中行改变事件，operate模块和List模块之间通信接口
///作者：阮锦芳
///编写日期：2018-04-19
///修改记录
///R1：
///		修改作者：作者中文名
///		修改日期：<模块创建日期，格式：YYYY-MM-DD>
///		修改内容：xxx
///</summary>

namespace ProtocolAggregator
{
    /// <summary>
    /// 更新List选中行事件，跟新具体信息在IndexMap类里
    /// </summary>
    public class EventListSelectChanged : CompositePresentationEvent<IndexMap>
    {
    }
}
