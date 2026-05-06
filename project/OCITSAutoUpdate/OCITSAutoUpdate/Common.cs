using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

///<summary>
///文件名：Common
///作用：公用类
///作者：高鹏娟
///编写日期：2018-05-28
///修改记录
///R1：
///		修改作者：
///		修改日期：
/// 	修改内容：
///</summary>
namespace OCITSAutoUpdate
{
    public class Common
    {
        private static object objLock = new object();
        public static void WriteLog(string content)
        {
            try
            {
                lock (objLock)
                {
                    string file = "";
                    file = Environment.CurrentDirectory + "\\log.txt";
                    FileStream fs=null;
                    StreamWriter sw=null;
                    if (!File.Exists(file))
                    {
                        fs = new FileStream(file, FileMode.CreateNew);
                        sw = new StreamWriter(fs, Encoding.Default);
                    }
                    else
                        sw = new StreamWriter(file, true, Encoding.Default);
                    sw.WriteLine(content);
                    sw.Close();
                    if (fs != null)
                    {
                        fs.Close();
                        fs = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("保存失败，请重新保存!\r\n" + ex.Message);
            }
        }

        /// <summary>
        /// 拷贝文件夹(删除旧的)
        /// </summary>
        /// <param name="from">源文件夹</param>
        /// <param name="to">目标文件夹</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0==成功，1==出错，2==源文件不存在</returns>
        public static int CopyFolder(string from, string to, ref string errMsg, bool isDeleteOld, bool isFilterSet)
        {
            try
            {
                if (Directory.Exists(from))
                {
                    if (isDeleteOld)
                    {
                        if (Directory.Exists(to))
                            DelectDir(to, ref errMsg);
                        Directory.CreateDirectory(to);
                    }
                    else
                    {
                        if (!Directory.Exists(to))
                            Directory.CreateDirectory(to);
                    }
                    // 子文件夹
                    foreach (string sub in Directory.GetDirectories(from))
                    {
                        string[] subArr = sub.Split('\\');
                        if (subArr[subArr.Length - 1].ToUpper() != "DATA" && subArr[subArr.Length - 1].ToUpper() != "TEMPLE"
                            && subArr[subArr.Length - 1].ToUpper() != "RAWDATA" && subArr[subArr.Length - 1].ToUpper() != "REFERENCE")
                        {
                            if (isFilterSet)
                            {
                                if (subArr[subArr.Length - 1].ToUpper() != "SET")
                                    CopyFolder(sub, to + "\\" + System.IO.Path.GetFileName(sub), ref errMsg, false, isFilterSet);
                            }
                            else
                                CopyFolder(sub, to + "\\" + System.IO.Path.GetFileName(sub), ref errMsg, false, isFilterSet);
                        }
                    }
                    // 文件
                    foreach (string file in Directory.GetFiles(from))
                        File.Copy(file, to + "\\" + System.IO.Path.GetFileName(file), true);
                }
                else
                    return 2;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="path">目标文件夹</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>true==成功，false==失败</returns>
        public static bool DelectDir(string path, ref string errMsg)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        string[] subArr = i.FullName.Split('\\');
                        if (subArr[subArr.Length - 1].ToUpper() == "DATA" || subArr[subArr.Length - 1].ToUpper() == "TEMPLE"
                            || subArr[subArr.Length - 1].ToUpper() == "RAWDATA" || subArr[subArr.Length - 1].ToUpper() == "REFERENCE")
                            continue;
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 查找exe路径
        /// </summary>
        /// <param name="softPath"></param>
        /// <param name="softName"></param>
        /// <param name="version"></param>
        /// <param name="EXEPath"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static string FindEXEPath(string folder)
        {
            try
            {
                string name = "";
                List<string> EXEPathList = new List<string>();
                foreach (string file in Directory.GetFiles(folder))
                {
                    name = System.IO.Path.GetFileName(file);
                    if (name.Substring(name.Length - 4).ToUpper() == ".EXE")
                    {
                        return file;
                    }
                    name = "";
                }
                return "";
            }
            catch (Exception ex)
            {
                string content = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                WriteLog(content);
                return "";
            }
        }

        /// <summary>
        /// 比较版本大小
        /// </summary>
        /// <param name="localFolder"></param>
        /// <param name="serverFolder"></param>
        /// <returns></returns>
        public static bool CompareFolderVersion(string localFolder, string serverFolder)
        {
            if (!Directory.Exists(localFolder))
                return true;
            if (!Directory.Exists(serverFolder))
                return false;

            foreach (string file in Directory.GetFiles(serverFolder))
            {
                string localFile = localFolder + file.Substring(serverFolder.Length);
                FileInfo localInfo = new FileInfo(localFile);
                FileInfo serverInfo = new FileInfo(file);
                DateTime localLastModified = localInfo.LastWriteTime;
                DateTime serverLastModified = serverInfo.LastWriteTime;
                if (localLastModified != serverLastModified)
                    return true;
            }

            foreach (string file in Directory.GetDirectories(serverFolder))
            {
                string[] subArr = file.Split('\\');
                if (subArr[subArr.Length - 1].ToUpper() == "DATA" || subArr[subArr.Length - 1].ToUpper() == "TEMPLE"
                    || subArr[subArr.Length - 1].ToUpper() == "RAWDATA" || subArr[subArr.Length - 1].ToUpper() == "REFERENCE")
                    continue;
                string localFile = localFolder + file.Substring(serverFolder.Length);
                if (CompareFolderVersion(localFile, file))
                    return true;
            }

            return false;
        }
    }
}
