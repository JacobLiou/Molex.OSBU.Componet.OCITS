using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility;

///<summary>
///文件名：EventTemplateUpdate
///作用：测试模板更新事件，operate模块和List模块之间通信接口
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
    public class EventTemplateUpdate : CompositePresentationEvent<List<FusionControl>>
    {
    }
}
