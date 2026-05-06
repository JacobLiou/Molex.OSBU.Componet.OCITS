using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using MenuPluginInterface;
using MolexUtility.Protocol;


namespace ConfigModel
{
    [Export(typeof(IMenuPlugin))]
    public class ConfigPlugin:IMenuPlugin
    {
        /// <summary>
        /// 配置主界面，单例模式
        /// </summary>
        private static ConfigMain config = new ConfigMain();

        /// <summary>
        /// 返回设备配置所在的菜单
        /// </summary>
        public MenuDetail MenuHeader 
        {
            get
            {
                MenuDetail deviceConfig = new MenuDetail();
                deviceConfig.HostHeader = "设置";
                deviceConfig.SubHeader= "设备配置";
                return deviceConfig;
            }
        }

        /// <summary>
        /// 显示窗口
        /// </summary>
        public void Show(MainInitInfo info)
        {
            config.BaseInfo(info);    
            config.Show();
            config.Activate();
        }
    }
}
