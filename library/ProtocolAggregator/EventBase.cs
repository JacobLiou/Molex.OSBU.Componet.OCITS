using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

///<summary>
///文件名：EventBase
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
    public class EventBase
    {
    }

    public class CompositePresentationEvent<T>:EventBase 
        where T:new()
    {
        private  List<Action<T>> handlers = new List<Action<T>>();

        /// <summary>
        /// 监听事件，当有发布事件，则调用对应处理函数
        /// </summary>
        /// <param name="callback">事件发布时，调用的处理函数</param>
        public void Subscribe(Action<T> callback)
        {
            handlers.Add(callback);
        }

        /// <summary>
        /// 事件发布
        /// </summary>
        /// <param name="parameter">传输的信息</param>
        public void Publish(T parameter)
        {
            handlers.ForEach(a => a(parameter));
        }
    }
}
