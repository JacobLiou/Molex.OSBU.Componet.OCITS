using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Runtime.InteropServices;
///<summary>
///文件名：IniParser
///作用：文件解析类，section key value类ini文件
///作者：阮锦芳
///编写日期：2018-01-22
///修改记录
///R1：
///		修改作者：阮锦芳
///		修改日期：2019-2-23
///		修改内容：读文件增加先将内如读取，用“=”号分割，保存到字典中，
///		         后续需要直接从字典中解析，大大缩短文件解析时间
///</summary>
namespace MolexUtility
{
    public class IniParser
    {
        /// <summary>
        /// 文件名称
        /// </summary>
        private string _file_path;

        /// <summary>
        /// 文件流
        /// </summary>
        private StreamReader readStream=null;

        /// <summary>
        /// 保存当前读到的section
        /// </summary>
        private string curSection = "";

        /// <summary>
        /// 是否先读取文件内容，如果是的话，将内容读取到fileAllInfos保存
        /// </summary>
        private bool isReaded = false;

        /// <summary>
        /// 保存文件中section key value，大大缩短文件解析时间
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> fileAllInfos = new Dictionary<string, Dictionary<string, string>>();

        public IniParser()
        {
            _file_path = Directory.GetCurrentDirectory() + "\\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".ini";
            //readStream = new StreamReader(_file_path);
        }

        public void CloseFile()
        {
            if (!isReaded&& readStream != null)
                readStream.Close();
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="t_filename">文件名</param>
        public IniParser(string t_filename)
        {
            if (string.IsNullOrEmpty(t_filename))
            {
                _file_path = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".ini";

                return;
            }
            else
            {
                _file_path = t_filename;

            }
            
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="t_filename">文件名</param>
        public IniParser(string t_filename, bool isRead)
        {
            if (string.IsNullOrEmpty(t_filename))
            {
                _file_path = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".ini";

                return;
            }
            else
            {
                _file_path = t_filename;

            }
            
            if (isRead)
            {
                readStream = new StreamReader(_file_path);
                isReaded = true;
                string line = readStream.ReadLine();
                Dictionary<string, string> keyDic = null;
                while (line != null)
                {
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        curSection = line;
                        keyDic = new Dictionary<string, string>();
                        //如果是section，则可能进入下一个循环，下个循环对line有做判断，如果是[]section，则退出，所以需要往下读一行
                        line = readStream.ReadLine();
                    }
                    else
                    {
                        //找到对应section情况下，找keys的循环
                        while (line != null)
                        {
                            //在找到要求的section的情况下，再遇到[]，说明当前section结束
                            string[] splits = line.Split('=');
                            if (splits.Length == 2)
                            {
                                keyDic.Add(splits[0], splits[1]);
                            }
                            else if (splits.Length > 2)
                            {
                                if (splits[0] == "Key Name" || splits[0] == "Port Caption")
                                {
                                    string value = splits[1];
                                    for (int i = 2; i < splits.Length; i++)
                                    {
                                        value += "=" + splits[i];
                                    }
                                    keyDic.Add(splits[0], value);
                                }
                                else
                                {
                                    string value = splits[0];
                                    for (int i = 1; i < splits.Length - 1; i++)
                                    {
                                        value += "=" + splits[i];
                                    }
                                    keyDic.Add(value, splits[splits.Length - 1]);
                                }
                            }

                            line = readStream.ReadLine();
                            if (line == null || (line[0] == '[' && line[line.Length - 1] == ']'))
                            {
                                fileAllInfos.Add(curSection, keyDic);
                                break;
                            }

                        }
                    }
                }
                readStream.Close();
            }

        }

        /// <summary>
        /// 将数据写入文件
        /// </summary>
        /// <param name="t_section">ini文件section</param>
        /// <param name="t_key">ini文件key</param>
        /// <param name="t_data">数据</param>
        /// <returns>成功返回true，错误返回false</returns>
        public bool writeData(string t_section, string t_key, string t_data)
        {
            long i_result = Win32API.WritePrivateProfileString(t_section, t_key, t_data, _file_path);

            return (i_result != 0 ? true : false);
        }

        /// <summary>
        /// 读取文件数据
        /// </summary>
        /// <param name="t_section">ini文件section</param>
        /// <param name="t_key">ini文件key</param>
        /// <param name="t_default">默认值</param>
        /// <returns>读取的数据</returns>
        public string readStringData(string t_section, string t_key, string t_default = "")
        {
            if (isReaded)
            {
                string section = "[" + t_section + "]";
                string res = fileAllInfos[section][t_key];
                return res;
            }
            else
            {
                StringBuilder temp = new StringBuilder(2048);
                int i = Win32API.GetPrivateProfileString(t_section, t_key, t_default, temp, 2048, this._file_path);
                return temp.ToString();
            }

        }

        /// <summary>
        /// 针对大文件写的，不需要重复打开，加快解析速度
        /// </summary>
        /// <param name="t_section">ini文件section</param>
        /// <param name="keys">ini文件keys</param>
        /// <param name="results">返回结果</param>
        /// <param name="t_default">默认返回值</param>
        public void readStringData(string t_section, string[] keys, out string[] results, string t_default = "")
        {
            results = new string[keys.Length];
            if (isReaded)
            {
                string section = "[" + t_section + "]";
                //results = new string[keys.Length];
                //string line = readStream.ReadLine();
                int resCount = 0;
                foreach (string key in keys)
                {
                    results[resCount] = fileAllInfos[section][key];
                    resCount++;
                }

            }
            else
            {
                if (readStream == null)
                {
                    readStream = new StreamReader(_file_path);
                }
                //找对应section的循环
                string line = readStream.ReadLine();
                string section = "[" + t_section + "]";
                int resCount = 0;
                while (line != null)
                {
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        curSection = line;
                        //如果是section，则可能进入下一个循环，下个循环对line有做判断，如果是[]section，则退出，所以需要往下读一行
                        line = readStream.ReadLine();
                    }
                    if (curSection == section)
                    {
                        //line = readStream.ReadLine();
                        //找到对应section情况下，找keys的循环
                        while (line != null)
                        {
                            //在找到要求的section的情况下，再遇到[]，说明当前section结束
                            if (line[0] == '[' && line[line.Length - 1] == ']')
                            {
                                curSection = line;
                                return;
                            }
                            for (int i = 0; i < keys.Length; i++)
                            {
                                //key或者内容里面可能含有“=”，
                                string key = keys[i] + "=";
                                string leftKey = "";
                                if (key.Length < line.Length)
                                {
                                    leftKey = line.Substring(0, key.Length);
                                }
                                if (leftKey == key)
                                {
                                    string[] keySplits = keys[i].Split('=');
                                    string[] splits = line.Split('=');

                                    if (splits.Length > 0)
                                    {
                                        string res = splits[keySplits.Length];
                                        for (int j = keySplits.Length + 1; j < splits.Length; j++)
                                        {
                                            res += "=";
                                            res += splits[j];
                                        }
                                        results[i] = res;
                                        resCount++;
                                        if (resCount == keys.Length)
                                            return;

                                    }
                                }
                            }
                            line = readStream.ReadLine();
                        }
                    }
                    line = readStream.ReadLine();
                }
            }

        }

        /// <summary>
        /// 读取文件数据
        /// </summary>
        /// <param name="t_section">ini文件section</param>
        /// <param name="t_key">ini文件key</param>
        /// <param name="t_default">默认值</param>
        /// <returns>读取的数据</returns>
        public int readIntData(string t_section, string t_key, int t_default = 0)
        {
            if (isReaded)
            {
                string section = "[" + t_section + "]";
                string res = fileAllInfos[section][t_key];
                return Convert.ToInt32(res);
            }
            else
            {
                int i_result = Win32API.GetPrivateProfileInt(t_section, t_key, t_default, this._file_path);

                return i_result;
            }
        }


    }
}
