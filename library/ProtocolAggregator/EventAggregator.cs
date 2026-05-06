using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;


///<summary>
///文件名：EventAggregator
///作用：模块与模块之间通信的核心，采用观察者模式来实现
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
    [Export(typeof(IEventAggregator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class EventAggregator : IEventAggregator
    {
        private static List<EventBase> events = new List<EventBase>();

        public T GetEvent<T>()
        {
            if (events.OfType<T>().FirstOrDefault() == null)
            {
                var evt = Activator.CreateInstance<T>();
                events.Add(evt as EventBase);
            }
            var result = events.OfType<T>().FirstOrDefault();
            return result;
        }
    }
}
