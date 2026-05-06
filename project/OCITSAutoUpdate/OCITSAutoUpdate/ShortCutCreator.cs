using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IWshRuntimeLibrary;

///<summary>
///文件名：ShortCutCreator
///作用：创建快捷方式类
///作者：高鹏娟
///编写日期：2018-05-29
///修改记录
///R1：
///		修改作者：
///		修改日期：
/// 	修改内容：
///</summary>
namespace OCITSAutoUpdate
{
    /// <summary>
    /// 创建快捷方式的类
    /// </summary>
    /// <remarks></remarks>
    public class ShortCutCreator
    {
        //需要引入IWshRuntimeLibrary，搜索Windows Script Host Object Model
        /// <summary>
        /// 创建快捷方式
        /// </summary>
        /// <param name="directory">快捷方式所处的文件夹</param>
        /// <param name="shortcutName">快捷方式名称</param>
        /// <param name="targetPath">目标路径</param>
        /// <param name="description">描述</param>
        /// <param name="iconLocation">图标路径，格式为"可执行文件或DLL路径, 图标编号"，
        /// 例如System.Environment.SystemDirectory + "\\" + "shell32.dll, 165"</param>
        /// <remarks></remarks>
        public static void CreateShortcut(string directory, string shortcutName, string targetPath, string description = null, string iconLocation = null)
        {
            try
            {
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                string shortcutPath = Path.Combine(directory, string.Format("{0}.lnk", shortcutName));
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);//创建快捷方式对象
                shortcut.TargetPath = targetPath;//指定目标路径
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);//设置起始位置
                shortcut.WindowStyle = 1;//设置运行方式，默认为常规窗口
                //shortcut.Description = description;
                shortcut.Description = shortcutName;//设置备注
                shortcut.IconLocation = string.IsNullOrWhiteSpace(iconLocation) ? targetPath : iconLocation;//设置图标路径
                shortcut.Save();//保存快捷方式
            }
            catch (Exception ex)
            {
                string errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                Common.WriteLog(errMsg);
                return;
            }
        }

        /// <summary>
        /// 创建桌面快捷方式
        /// </summary>
        /// <param name="shortcutName">快捷方式名称</param>
        /// <param name="targetPath">目标路径</param>
        /// <param name="description">描述</param>
        /// <param name="iconLocation">图标路径，格式为"可执行文件或DLL路径, 图标编号"</param>
        /// <remarks></remarks>
        public static void CreateShortcutOnDesktop(string shortcutName, string targetPath, string description = null, string iconLocation = null)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);//获取桌面文件夹路径
                string shortcutPath = desktop + "\\" + shortcutName + ".lnk";
                foreach (string file in Directory.GetFiles(desktop))
                {
                    if (file.Substring(file.Length - 4, 4).ToUpper() == ".LNK")
                    {
                        WshShell shell = new WshShell();
                        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(file);//创建快捷方式对象
                        if (string.Compare(shortcut.TargetPath.Trim().ToUpper(),targetPath.Trim().ToUpper ())==0)
                        {
                            System.IO.File.Delete(file);
                        }
                    }
                }

                CreateShortcut(desktop, shortcutName, targetPath, description, iconLocation);
            }
            catch (Exception ex)
            {
                string errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                Common.WriteLog(errMsg);
                return;
            }
        }
    }
}
