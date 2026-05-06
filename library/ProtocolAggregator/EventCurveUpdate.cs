using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Protocol;

///<summary>
///文件名：EventCurveUpdate
///作用：曲线更新事件，需要用到曲线显示的模块注册此事件，并且在需要曲线更新时产生该事件即可
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
    /// 更新曲线事件，跟新具体信息在CurveUpdateDetail类里
    /// </summary>
    public class EventCurveUpdate: CompositePresentationEvent<CurveUpdateDetail>
    {
    }
}
