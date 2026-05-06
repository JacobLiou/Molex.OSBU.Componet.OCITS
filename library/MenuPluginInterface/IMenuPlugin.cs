using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Protocol;

namespace MenuPluginInterface
{
    /// <summary>
    /// 菜单栏增加菜单项插件接口
    /// </summary>
    public interface IMenuPlugin
    {
        /// <summary>
        /// 插件菜单信息
        /// </summary>
        MenuDetail MenuHeader { get; }

        /// <summary>
        /// 显示插件，插件窗口关闭时不能释放资源，需要重载closing事件，在事件中将窗口隐藏。在主程序关闭时统一关闭。
        /// </summary>
        void Show(MainInitInfo info);
    }

    public class MenuDetail
    {
        /// <summary>
        /// 第一级设置标题
        /// </summary>
        public string HostHeader { get; set; }

        /// <summary>
        /// 第二级设置标题
        /// </summary>
        public string SubHeader { get; set; }
        public MenuDetail()
        {
            HostHeader = "";
            SubHeader = "";
        }
    }
}
