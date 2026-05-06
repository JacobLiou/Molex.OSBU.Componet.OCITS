using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


///<summary>
///文件名：IEventAggregator
///作用：用于模块间通信的事件接口
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
    public interface IEventAggregator
    {
        T GetEvent<T>();
    }

    
}
