using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
///<summary>
///文件名：IOpticalSwitch
///作用：光源盒接口类，定义了外部访问光源盒的接口
///作者：阮锦芳
///编写日期：2018-01-22
///修改记录
///R1：
///		修改作者：作者中文名
///		修改日期：<模块创建日期，格式：YYYY-MM-DD>
///		修改内容：xxx
///</summary>

namespace MolexUtility.Device
{
    public interface IOpticalSwitch
    {
        /// <summary>
        /// 切换光源盒
        /// </summary>
        /// <param name="flag">切换的标志，格式暂定 产品序号:波长:端口:参数:光源类型</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错</returns>
        int SetSwitch(string flag, ref string errMsg);

        /// <summary>
        /// 光源盒名称，和配置文件名称一致，程序用来决定使用哪个开关
        /// </summary>
        string SwitchName { get; set; }
    }
}
