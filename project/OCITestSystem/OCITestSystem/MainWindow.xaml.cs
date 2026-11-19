using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Threading;
using System.ComponentModel;
using ProtocolAggregator;
using System.Xml;
using MenuPluginInterface;
using MolexUtility.Device;
using MolexUtility.Protocol;
using MolexUtility;
using System.IO;
using System.Reflection;
using MolexUtility.UIList;
using Path = System.IO.Path;

///<summary>
///文件名：MainWindow.xaml.cs
///作用：主程序，动态加载菜单、子模块、设备初始化
///作者：阮锦芳
///编写日期：2018-02-26
///修改记录
///</summary>


namespace OCITestSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private CompositionContainer container;

        /// <summary>
        /// 定义了Export(UserControl)的界面子模块集
        /// </summary>
        [ImportMany(AllowRecomposition = true)]
        private IEnumerable<UserControl> cards;

        private List<UserControl> dynamicCards = new List<UserControl>();

        /// <summary>
        /// 与其他模块通信的事件集 
        /// </summary>
        [Import(typeof(IEventAggregator))]
        public IEventAggregator EventAggregator { get; set; }

        /// <summary>
        /// 定义了Export(IMenuPlugin)的菜单项集
        /// </summary>
        [ImportMany]
        private IEnumerable<IMenuPlugin> menuPlugins;

        /// <summary>
        /// 设备处理模块
        /// </summary>
        [Import]
        private IDeviceHandle deviceHandle;

        private string stationType = "";

        private string softwareID = "";

        private string softwareVersion = "";

        private string softwareName = "";

        private string useUDL = "0";

        private MainInitInfo mainInfo = new MainInitInfo();

        /// <summary>
        /// 所有工位类型信息
        /// </summary>
        private List<StationShowConfig> allStations = new List<StationShowConfig>();

        public MainWindow(string type)
        {
            InitializeComponent();
            this.ResizeMode = System.Windows.ResizeMode.CanMinimize;
            this.Left = 0;
            this.Top = 0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            ParserCmdArgs(type);
            //产线;工位类型;ID;应用程序路径;模板类型;工序;UserID
            //按照这个解析
            /*string[] splits = type.Split(';');
            if(splits.Length==7)
            {
                mainInfo.ProductLine = splits[0];
                mainInfo.StationType = splits[1];
                mainInfo.StationID = splits[2];
                //mainInfo.ExePath = splits[3];
                mainInfo.TemplateType = splits[3];
                mainInfo.TestProcess = splits[4];
                mainInfo.UserID = splits[5];
                mainInfo.Goldsample= splits[6];
                stationType = mainInfo.StationType;
                barUserID.Content = "工号：" + mainInfo.UserID;
                barTemplate.Content = "模板类型："+mainInfo.TemplateType;
                barTestProcess.Content = "工序：" + mainInfo.TestProcess;
            }*/
            string errMsg = "";
            //增加工位类型文件解析
            StationXMLParser.GetAllStations(GetExeDir() + "\\set\\stations.xml", ref allStations, ref errMsg);
            if(allStations.Count>0&& allStations[0].Stations.Count>0)
            {
                Environment.CurrentDirectory = GetExeDir();
                SingleStationConfig activeStation = GetSelectedStationConfig();
                if (activeStation == null)
                    activeStation = allStations[0].Stations[0];
                stationType = activeStation.Name;
                mainInfo.AutomationType = Convert.ToInt32(activeStation.Automation);
                string fileName = GetExeDir() + "\\Module\\Module_" + stationType + ".xml";
                string mainDllPath = GetExeDir() +"\\"+ activeStation.MainDllPath;
                mainInfo.Goldsample = activeStation.Goldsample;
                ApplyTestProcessFromStationsXml();
                LayoutXMLParser.ParseSoftIDAndVersion(fileName, ref softwareID, ref softwareVersion, ref softwareName,ref useUDL);
                if (stationType.Length > 0)
                {
                    FileInfo dllInfo = new FileInfo(mainDllPath);
                    //txtTitle.Text = "OCITS 光器件集成测试系统" + "--" + stationType + "(" + softwareID + "_" + softwareVersion + ")";
                    txtTitle.Text = softwareName + "_" + softwareVersion;
                }
            }
            else
            {
                MessageBox.Show("工位配置出错，请检查配置文件stations.xml");
            }
        }

        /// <summary>
        /// 与插件通信
        /// </summary>
        private void InitRegerster()
        {
            //receive Xml format protocal
            EventAggregator.GetEvent<EventXml>().Subscribe
                (
                    msg =>
                    {
                        ParserMsg(msg);
                    }
                );
        }
        private void ParserMsg(XmlStr msg)
        {
            MsgBaseInfo info = new MsgBaseInfo();
            MsgXmlParser.GetMsgBase(msg.Content, ref info);
            if (info.MsgTarget == "MainWindow")
            {
                if (info.MsgType == "Template")
                {
                    if (info.Operate == "ShowTemplatePath")
                    {
                        string tmpltPath = MsgXmlParser.GetNodeInner(msg.Content, "Path");
                        if (tmpltPath != "")
                        {
                            barTmpltPath.Content = "模板："+ tmpltPath;
                        }
                    }
                }
                else if (info.MsgType == "BtnClick")
                {
                    if (info.Operate == "InitDevice")
                    {
                        string result = MsgXmlParser.GetNodeInner(msg.Content, "Result");
                        if (result == "0")
                        {
                            curStatus.Content = "设备正常初始化";
                        }
                        else
                        {
                            string errMsg = MsgXmlParser.GetNodeInner(msg.Content, "NoteMsg");
                            MessageBox.Show(errMsg);
                            curStatus.Content = "设备初始化出错！";
                        }
                    }
                }
            }
        }

        private SingleStationConfig GetSelectedStationConfig()
        {
            if (allStations == null)
                return null;
            foreach (StationShowConfig line in allStations)
            {
                foreach (SingleStationConfig station in line.Stations)
                {
                    if (station.IsSelected)
                        return station;
                }
            }
            return null;
        }

        /// <summary>
        /// 从 set\stations.xml 中已选工位的 TestProcess 覆盖 MIMS 传入工序（ITL_FTS 应配置为 Interleaver-ITL-终测）。
        /// </summary>
        private void ApplyTestProcessFromStationsXml()
        {
            if (allStations == null || allStations.Count == 0)
                return;
            foreach (StationShowConfig line in allStations)
            {
                foreach (SingleStationConfig station in line.Stations)
                {
                    if (!station.IsSelected || string.IsNullOrWhiteSpace(station.TestProcess))
                        continue;
                    if (string.IsNullOrEmpty(stationType) ||
                        string.Equals(station.Name, stationType, StringComparison.OrdinalIgnoreCase))
                    {
                        mainInfo.TestProcess = station.TestProcess.Trim();
                        return;
                    }
                }
            }
        }

        private void ParserCmdArgs(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                return;
            if (!args.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                TryParseSemicolonStationArgs(args);
                return;
            }
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(args);
                XmlNode rootNode = doc.SelectSingleNode("MIMS");
                XmlNode appInfoNode = rootNode.SelectSingleNode("AppInfo");
                XmlNodeList childNodes = appInfoNode.ChildNodes;
                foreach (XmlNode child in childNodes)
                {
                    if (child.Name.ToString().ToUpper() == "USER")
                    {
                        mainInfo.UserID = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "PN")
                    {

                    }
                    else if (child.Name.ToString().ToUpper() == "SN")
                    {

                    }
                    else if (child.Name.ToString().ToUpper() == "PROCESS")
                    {
                        mainInfo.TestProcess = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "LOGINMODE")
                    {
                        mainInfo.LoginMode = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "SOFTWAREID")
                    {
                        mainInfo.SoftwareID = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "MESMODE")
                    {
                        mainInfo.MESMode = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "CHECKUSER")
                    {
                        mainInfo.CheckUser = child.InnerText;
                    }
                    else if (child.Name.ToString().ToUpper() == "CHECKPWD")
                    {
                        mainInfo.CheckPSW = child.InnerText;
                    }
                }
            }
            catch(Exception ex)
            {
                TryParseSemicolonStationArgs(args);
            }
        }

        /// <summary>
        /// 登录界面传入：产线;工位类型;ID;模板类型;工序;UserID;Goldsample
        /// </summary>
        private void TryParseSemicolonStationArgs(string args)
        {
            string[] splits = args.Split(';');
            if (splits.Length < 7)
                return;
            mainInfo.ProductLine = splits[0];
            mainInfo.StationType = splits[1];
            mainInfo.StationID = splits[2];
            mainInfo.TemplateType = splits[3];
            mainInfo.TestProcess = splits[4];
            mainInfo.UserID = splits[5];
            mainInfo.Goldsample = splits[6];
            stationType = mainInfo.StationType;
        }

        /// <summary>
        /// 始终参与 MEF 组合的 library 插件（与 Module_*.xml 中的 UI 模块分开配置）。
        /// </summary>
        private static readonly string[] ModuleCorePluginAssemblies =
        {
            "DeviceControl.dll",
            "ConfigModel.dll",
            "InterleaverAlgorithm.dll",
        };

        /// <summary>
        /// 与宿主共用的依赖程序集：禁止再 Load 出第二份，否则 MEF 契约 TypeIdentity 分裂
        /// （典型症状：IDeviceHandle / IInterleaverAlgorithm 同时「未找到匹配导出」）。
        /// </summary>
        private static readonly HashSet<string> SharedDependencyAssemblyFiles =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "MolexUtility.dll",
                "ProtocolAggregator.dll",
                "MenuPluginInterface.dll",
            };

        private static bool assemblyResolveRegistered;
        private static string probeExeDir = "";
        private static string probeModuleDir = "";
        private static string probeModuleCacheDir = "";
        private static readonly object AssemblyLoadGate = new object();
        private static readonly Dictionary<string, Assembly> AssembliesBySimpleName =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 写入 compose 专用日志（不依赖 Environment.CurrentDirectory；网络盘 Log 写失败时回退到 %TEMP%）。
        /// </summary>
        private void WriteComposeLog(string message)
        {
            string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} {1}\r\n", DateTime.Now, message ?? "");
            string[] logDirs =
            {
                Path.Combine(GetExeDir(), "Log"),
                Path.Combine(Path.GetTempPath(), "OCITestSystem", "Log")
            };
            foreach (string logDir in logDirs)
            {
                try
                {
                    if (string.IsNullOrEmpty(logDir))
                        continue;
                    Directory.CreateDirectory(logDir);
                    File.AppendAllText(Path.Combine(logDir, "compose.log"), line, Encoding.UTF8);
                    return;
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 按简单名查找 AppDomain 中已加载程序集（忽略动态/反射上下文异常）。
        /// </summary>
        private static Assembly FindLoadedAssembly(string simpleName)
        {
            if (string.IsNullOrEmpty(simpleName))
                return null;

            Assembly cached;
            lock (AssemblyLoadGate)
            {
                if (AssembliesBySimpleName.TryGetValue(simpleName, out cached) && cached != null)
                    return cached;
            }

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (string.Equals(asm.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                    {
                        RememberLoadedAssembly(simpleName, asm);
                        return asm;
                    }
                }
                catch
                {
                }
            }
            return null;
        }

        private static void RememberLoadedAssembly(string simpleName, Assembly asm)
        {
            if (string.IsNullOrEmpty(simpleName) || asm == null)
                return;
            lock (AssemblyLoadGate)
            {
                AssembliesBySimpleName[simpleName] = asm;
            }
        }

        /// <summary>
        /// 将 module\ 下 DLL 同步到本机临时目录，避免网络盘 LoadFrom 触发 0x80131515。
        /// </summary>
        private static string SyncModuleDllCache(string moduleDir)
        {
            string cacheDir = Path.Combine(Path.GetTempPath(), "OCITestSystem", "module_cache");
            Directory.CreateDirectory(cacheDir);
            foreach (string src in Directory.GetFiles(moduleDir, "*.dll"))
            {
                string dest = Path.Combine(cacheDir, Path.GetFileName(src));
                try
                {
                    File.Copy(src, dest, true);
                }
                catch
                {
                    // 单文件拷贝失败不阻断；后续可回退直接读 module\
                }
            }
            return cacheDir;
        }

        /// <summary>
        /// 加载程序集：同名只保留一份。优先 LoadFrom（带 Location，利于依赖绑定）；
        /// 失败再 Load(byte[])。绝不能对 MolexUtility 等依赖每次新建身份。
        /// </summary>
        private static Assembly LoadAssemblyFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException("程序集文件不存在", path);

            string simpleName = Path.GetFileNameWithoutExtension(path);
            Assembly existing = FindLoadedAssembly(simpleName);
            if (existing != null)
                return existing;

            lock (AssemblyLoadGate)
            {
                if (AssembliesBySimpleName.TryGetValue(simpleName, out existing) && existing != null)
                    return existing;

                Assembly asm = null;
                Exception loadFromError = null;
                try
                {
                    // 本地缓存路径用 LoadFrom，保持单一身份且带 Location
                    asm = Assembly.LoadFrom(path);
                }
                catch (Exception ex)
                {
                    loadFromError = ex;
                }

                if (asm == null)
                {
                    try
                    {
                        byte[] raw = File.ReadAllBytes(path);
                        asm = Assembly.Load(raw);
                    }
                    catch (Exception ex)
                    {
                        throw new FileLoadException(
                            "无法加载程序集: " + path +
                            (loadFromError != null ? "；LoadFrom: " + loadFromError.Message : ""),
                            ex);
                    }
                }

                AssembliesBySimpleName[simpleName] = asm;
                return asm;
            }
        }

        private Assembly LoadPluginAssembly(string moduleDir, string dllFileName)
        {
            // 共享依赖：强制使用 exe 根已加载/可加载的那一份，避免 module 副本造成 MEF 分裂
            if (SharedDependencyAssemblyFiles.Contains(dllFileName))
            {
                string simple = Path.GetFileNameWithoutExtension(dllFileName);
                Assembly shared = FindLoadedAssembly(simple);
                if (shared != null)
                    return shared;

                string exeDep = Path.Combine(probeExeDir, dllFileName);
                if (File.Exists(exeDep))
                    return LoadAssemblyFromFile(exeDep);
            }

            string cachePath = Path.Combine(probeModuleCacheDir, dllFileName);
            if (File.Exists(cachePath))
                return LoadAssemblyFromFile(cachePath);

            string modulePath = Path.Combine(moduleDir, dllFileName);
            if (File.Exists(modulePath))
                return LoadAssemblyFromFile(modulePath);

            throw new FileNotFoundException("插件 DLL 不存在", dllFileName);
        }

        /// <summary>
        /// 从 exe 根目录与 module\ 解析依赖（如 ADODB.dll），仅作依赖加载，不加入 MEF Catalog。
        /// </summary>
        private static void EnsureModuleAssemblyProbe(string exeDir, string moduleDir, string moduleCacheDir)
        {
            probeExeDir = exeDir ?? "";
            probeModuleDir = moduleDir ?? "";
            probeModuleCacheDir = moduleCacheDir ?? "";
            if (assemblyResolveRegistered)
                return;
            AppDomain.CurrentDomain.AssemblyResolve += ModuleAssemblyResolve;
            assemblyResolveRegistered = true;
        }

        private static Assembly ModuleAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string simpleName = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(simpleName))
                    return null;

                // 关键：复用已加载程序集，杜绝 MolexUtility 等被 Load 出第二份
                Assembly existing = FindLoadedAssembly(simpleName);
                if (existing != null)
                    return existing;

                // 共享依赖优先 exe 根目录
                bool shared = SharedDependencyAssemblyFiles.Contains(simpleName + ".dll");
                string[] dirs = shared
                    ? new[] { probeExeDir, probeModuleCacheDir, probeModuleDir }
                    : new[] { probeExeDir, probeModuleCacheDir, probeModuleDir };

                foreach (string dir in dirs)
                {
                    if (string.IsNullOrEmpty(dir))
                        continue;
                    string path = Path.Combine(dir, simpleName + ".dll");
                    if (!File.Exists(path))
                        continue;
                    return LoadAssemblyFromFile(path);
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// 预登记宿主已引用的共享程序集，保证后续插件 Import/Export 使用同一 TypeIdentity。
        /// </summary>
        private void PreloadSharedDependencyAssemblies(string exeDir)
        {
            try
            {
                RememberLoadedAssembly("ProtocolAggregator", typeof(EventAggregator).Assembly);
                RememberLoadedAssembly("MolexUtility", typeof(CommonFunction).Assembly);
                RememberLoadedAssembly("MenuPluginInterface", typeof(IMenuPlugin).Assembly);
            }
            catch (Exception ex)
            {
                WriteComposeLog("[Compose] PreloadShared(type) warn: " + ex.Message);
            }

            foreach (string dllName in SharedDependencyAssemblyFiles)
            {
                string simple = Path.GetFileNameWithoutExtension(dllName);
                if (FindLoadedAssembly(simple) != null)
                    continue;
                string path = Path.Combine(exeDir, dllName);
                if (!File.Exists(path))
                    continue;
                try
                {
                    LoadAssemblyFromFile(path);
                    WriteComposeLog("[Compose] preload shared: " + dllName);
                }
                catch (Exception ex)
                {
                    WriteComposeLog("[Compose] preload shared fail " + dllName + ": " + ex.Message);
                }
            }
        }

        /// <summary>
        /// 收集需加入 MEF 的插件 DLL 文件名（不含路径）。
        /// </summary>
        private IEnumerable<string> CollectModulePluginDllNames(string moduleDir)
        {
            var dllNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string core in ModuleCorePluginAssemblies)
                dllNames.Add(core);

            string layoutPath = Path.Combine(moduleDir, "Module_" + stationType + ".xml");
            if (File.Exists(layoutPath))
            {
                var rowDefs = new List<GridLength>();
                var colDefs = new List<GridLength>();
                var panels = new List<PanelConfige>();
                LayoutXMLParser.ParseConfig(layoutPath, ref rowDefs, ref colDefs, ref panels);
                foreach (PanelConfige panel in panels)
                {
                    if (string.IsNullOrWhiteSpace(panel.ModuleName))
                        continue;
                    dllNames.Add(panel.ModuleName.Trim() + ".dll");
                }
            }
            else
            {
                WriteComposeLog("[Compose] layout xml missing: " + layoutPath);
            }

            foreach (string dllName in dllNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                // 共享依赖只作绑定，不作为独立插件 Catalog（ProtocolAggregator 已由宿主 AssemblyCatalog 加入）
                if (SharedDependencyAssemblyFiles.Contains(dllName))
                    continue;
                yield return dllName;
            }
        }

        /// <summary>
        /// 仅组合 library 插件 DLL；依赖项通过 AssemblyResolve 解析，且同名程序集全局唯一。
        /// </summary>
        private void Compose()
        {
            WriteComposeLog("[Compose] begin exeDir=" + GetExeDir() + " stationType=" + stationType);
            try
            {
                string exeDir = GetExeDir();
                string moduleDir = Path.Combine(exeDir, "module");

                if (!Directory.Exists(moduleDir))
                {
                    WriteComposeLog("[Compose] module dir missing: " + moduleDir);
                    MessageBox.Show("module 目录不存在：" + moduleDir);
                    return;
                }

                string moduleCacheDir = SyncModuleDllCache(moduleDir);
                WriteComposeLog("[Compose] module cache: " + moduleCacheDir);

                EnsureModuleAssemblyProbe(exeDir, moduleDir, moduleCacheDir);
                PreloadSharedDependencyAssemblies(exeDir);

                var catalog = new AggregateCatalog();
                catalog.Catalogs.Add(new AssemblyCatalog(typeof(EventAggregator).Assembly));
                WriteComposeLog("[Compose] catalog add ProtocolAggregator (exe root, single)");

                foreach (string dllName in CollectModulePluginDllNames(moduleDir))
                {
                    string cachePath = Path.Combine(moduleCacheDir, dllName);
                    string modulePath = Path.Combine(moduleDir, dllName);
                    if (!File.Exists(cachePath) && !File.Exists(modulePath))
                    {
                        WriteComposeLog("[Compose] plugin missing: " + dllName);
                        continue;
                    }

                    WriteComposeLog("[Compose] catalog add: " + dllName);
                    Assembly pluginAsm = LoadPluginAssembly(moduleDir, dllName);
                    catalog.Catalogs.Add(new AssemblyCatalog(pluginAsm));
                }

                // 诊断：确认关键契约导出是否可见（便于网络盘联调）
                try
                {
                    using (var probe = new CompositionContainer(catalog))
                    {
                        int deviceExports = probe.GetExports<IDeviceHandle>().Count();
                        int algoExports = probe.GetExports<MolexUtility.Algorithm.IInterleaverAlgorithm>().Count();
                        WriteComposeLog(string.Format(
                            "[Compose] export probe IDeviceHandle={0} IInterleaverAlgorithm={1} MolexUtility={2}",
                            deviceExports,
                            algoExports,
                            typeof(CommonFunction).Assembly.FullName));
                    }
                }
                catch (Exception ex)
                {
                    WriteComposeLog("[Compose] export probe warn: " + ex.Message);
                }

                container = new CompositionContainer(catalog);
                container.ComposeParts(this);
                WriteComposeLog("[Compose] success");
            }
            catch (CompositionException ex)
            {
                string detail = FormatCompositionErrors(ex);
                WriteComposeLog("[Compose] CompositionException: " + detail);
                CommonFunction.WriteLog("[Compose] " + detail);
                MessageBox.Show(detail);
            }
            catch (Exception ex)
            {
                string detail = FormatExceptionDetail(ex, 0);
                WriteComposeLog("[Compose] Exception: " + detail);
                CommonFunction.WriteLog("[Compose] " + detail);
                MessageBox.Show(detail.Length > 4000 ? detail.Substring(0, 4000) + "\r\n...(详见 Log\\compose.log)" : detail);
            }
        }

        private static string FormatCompositionErrors(CompositionException ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);
            if (ex.Errors != null)
            {
                foreach (CompositionError err in ex.Errors)
                {
                    sb.AppendLine(err.Description);
                    if (err.Exception != null)
                        sb.Append(FormatExceptionDetail(err.Exception, 1));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 展开异常树（含 ReflectionTypeLoadException.LoaderExceptions），用于定位缺失依赖。
        /// </summary>
        private static string FormatExceptionDetail(Exception ex, int indent)
        {
            var sb = new StringBuilder();
            AppendExceptionDetail(sb, ex, indent, 0);
            return sb.ToString();
        }

        private static void AppendExceptionDetail(StringBuilder sb, Exception ex, int indent, int depth)
        {
            if (ex == null || depth > 6)
                return;
            string pad = new string(' ', indent * 2);
            sb.AppendLine(pad + ex.GetType().Name + ": " + ex.Message);

            ReflectionTypeLoadException rtle = ex as ReflectionTypeLoadException;
            if (rtle != null && rtle.LoaderExceptions != null)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Exception le in rtle.LoaderExceptions)
                {
                    if (le == null || string.IsNullOrEmpty(le.Message) || !seen.Add(le.Message))
                        continue;
                    sb.AppendLine(pad + "  [LoaderException] " + le.Message);
                }
            }

            CompositionException ce = ex as CompositionException;
            if (ce != null && ce.Errors != null)
            {
                foreach (CompositionError err in ce.Errors)
                {
                    if (err.Exception == null || err.Exception == ex)
                        continue;
                    AppendExceptionDetail(sb, err.Exception, indent + 1, depth + 1);
                }
            }

            AppendExceptionDetail(sb, ex.InnerException, indent, depth + 1);
        }

        private string GetExeDir()
        {
            string curPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string[] dirs = curPath.Split('\\');
            if (dirs.Length == 0)
                return "";
            //去除应用程序名，得到路径
            int count = dirs[dirs.Length - 1].Length + 1;
            string path = curPath.Remove(curPath.Length - count, count);

            return path;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Compose();
                initMenu();
                InitRegerster();
                //this.Owner = System.Windows.Application.Current.MainWindow;

                string fileName = GetExeDir() + "\\module\\Module_" + stationType+".xml";
                
                List<GridLength> rowDefines = new List<GridLength>();
                List<GridLength> columnDefines = new List<GridLength>();
                List<PanelConfige> childs = new List<PanelConfige>();
                //从配置文件读取布局信息
                //MessageBox.Show(fileName);
                LayoutXMLParser.ParseConfig(fileName, ref rowDefines, ref columnDefines, ref childs);
                
                //分行信息
                RowDefinitionCollection rowDefs = rootGrid.RowDefinitions;
                foreach (GridLength heigh in rowDefines)
                {
                    RowDefinition rowDef = new RowDefinition();
                    rowDef.Height = heigh;
                    rowDefs.Add(rowDef);
                }
                //MessageBox.Show("1");
                //分列信息
                ColumnDefinitionCollection columnDefs = rootGrid.ColumnDefinitions;
                foreach (GridLength width in columnDefines)
                {
                    ColumnDefinition columnDef = new ColumnDefinition();
                    columnDef.Width = width;
                    columnDefs.Add(columnDef);
                }
                //MessageBox.Show("2");
                //各模块分布信息
                foreach (PanelConfige child in childs)
                {
                    //MessageBox.Show("3");
                    DockPanel panel = new DockPanel();

                    if (child.Row != -1)
                    {
                        Grid.SetRow(panel, child.Row);
                    }
                    if (child.Column != -1)
                        Grid.SetColumn(panel, child.Column);
                    if (child.ColumnSpan != -1)
                        Grid.SetColumnSpan(panel, child.ColumnSpan);
                    if (child.RowSpan != -1)
                        Grid.SetRowSpan(panel, child.RowSpan);                      
                    panel.Margin = new Thickness(2);
                    if (child.ModuleName.Length > 0)
                    {
                        IEnumerable<Lazy<UserControl, IDictionary<string, object>>> modules = null;
                        modules = container.GetExports<UserControl, IDictionary<string, object>>().Where(x => (string)x.Metadata["name"] == child.ModuleName);
                        Lazy<UserControl, IDictionary<string, object>> tmp = modules.ElementAtOrDefault(0);
                        if (child.ModuleIndex > 0)
                        {
                            var typeName = tmp.Value.GetType();
                            UserControl control = (UserControl)Activator.CreateInstance(typeName);
                            control.Name = child.Name;
                            dynamicCards.Add(control);
                            panel.Children.Add(control);

                        }
                        else
                        {
                            panel.Children.Add(tmp.Value);
                            tmp.Value.Name= child.Name;
                        }
                    }
                    //panel根据module，增加自己显示内容
                    rootGrid.Children.Add(panel);
                }

                //
                //initMenu();
                App.DoEvents();
                //等待1s后开始初始化设备
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += WaitInitDevice;
                bw.RunWorkerCompleted += InitDevice;
                bw.RunWorkerAsync();

                //查看是否有文件夹temple，reference、rawdata、data文件夹，如无，则创建。
                string curPath = System.Environment.CurrentDirectory;
                string tempPath = curPath + "\\temple";
                if(false==Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                string refPath = curPath + "\\reference";
                if (false == Directory.Exists(refPath))
                {
                    Directory.CreateDirectory(refPath);
                }

                string rawPath = curPath + "\\rawdata";
                if (false == Directory.Exists(rawPath))
                {
                    Directory.CreateDirectory(rawPath);
                }

                string dataPath = curPath + "\\data";
                if (false == Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                string lightDataPath = curPath + "\\lightdata";
                if (false == Directory.Exists(lightDataPath))
                {
                    Directory.CreateDirectory(lightDataPath);
                }
                this.MaxHeight = SystemParameters.WorkArea.Height;
            }
            catch (Exception ex)
            {
                string detail = FormatExceptionDetail(ex, 0);
                WriteComposeLog("[Window_Loaded] Exception: " + detail);
                CommonFunction.WriteLog("[Window_Loaded] " + detail);
                MessageBox.Show(detail.Length > 4000 ? detail.Substring(0, 4000) + "\r\n...(详见 Log\\compose.log)" : detail);
            }
        }

        /// <summary>
        /// 初始化设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitDevice(object sender, RunWorkerCompletedEventArgs e)
        {
            string errMsg = "";
            if (useUDL == "0")
            {
                curStatus.Content = "正在初始化设备。。。";             
                if (deviceHandle.InitDeviceByConfig(ref errMsg) != 0)
                {
                    if (errMsg.Length > 0)
                    {
                        MessageBox.Show(errMsg);                       
                    }
                    curStatus.Content = "设备初始化出错！";
                    mainInfo.DeviceInitRes = false;
                }
            }
            barUserID.Content ="工号："+ mainInfo.UserID;
            string modeShow = "";
            if (mainInfo.LoginMode.ToUpper().Contains("DEBUG"))
            {
                barLoginType.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                modeShow = "调试模式";
                barTemplate.Content = "工程路径：TP/TT/NA/TD/CF";
                barTemplate.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
            else if (mainInfo.LoginMode.ToUpper().Contains("RD"))
            {
                barLoginType.Foreground = new SolidColorBrush(Color.FromRgb(255, 155, 0));
                modeShow = "研发模式";
                barTemplate.Content = "工程路径：TP/TT/NA/TD/CF";
                barTemplate.Foreground = new SolidColorBrush(Color.FromRgb(255, 155, 0));
            }
            else //if (mainInfo.LoginMode.ToUpper().Contains("MFG"))
            {
                barLoginType.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                modeShow = "生产模式";
                barTemplate.Content = "工程路径：NA";
                barTemplate.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }

            barLoginType.Content = "模式：" + modeShow + "/MES "+ mainInfo.MESMode;

            
            //更新测试信息
            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<EventMainInit>().Publish(mainInfo);
            }
            if (errMsg.Length == 0)
            {
                curStatus.Content = "设备正常初始化";
                mainInfo.DeviceInitRes = true;
            }

            
        }

        private void WaitInitDevice(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
        }

        /// <summary>
        /// 初始化菜单
        /// </summary>
        private void initMenu()
        {
            MenuItem item = new MenuItem();
            item.Header = "软件说明";
            menu.Items.Add(item);
            MenuItem item2 = new MenuItem();
            item2.Header = "帮助";
            menu.Items.Add(item2);

            foreach (IMenuPlugin plugin in menuPlugins)
            {
                MenuItem subItem = new MenuItem();
                subItem.Header = plugin.MenuHeader.SubHeader;
                //ToolStripMenuItem subItem = new ToolStripMenuItem(plugin.Text);
                subItem.Click += (s, arg) => { plugin.Show(mainInfo); };
                MenuItem curItem = FindHostItem(plugin.MenuHeader.HostHeader);
                curItem.Items.Add(subItem);
            }
        }

        /// <summary>
        /// 查找最上级菜单，如果存在返回对象，如果不存在，则创建
        /// </summary>
        /// <param name="header">查找菜单名称</param>
        /// <returns>菜单对象</returns>
        private MenuItem FindHostItem(string header)
        {
            ItemCollection items = menu.Items;
            MenuItem item = null;
            for (int i=0;i<items.Count;i++)
            {
                item = (MenuItem)items[i];
                if (item.Header.ToString() == header)
                    break;
                item = null;
            }
            //未找到，则创建
            if(item==null)
            {
                item = new MenuItem();
                item.Header = header;
                menu.Items.Insert(0, item);
            }
            return item;
        }

        
        /// <summary>
        /// 最小化按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mini_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; //设置窗口最小化
            App.DoEvents();
        }

        /// <summary>
        /// 最大化按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal; //设置窗口还原
            }
            else
            {
                this.WindowState = WindowState.Maximized; //设置窗口最大化
            }
            App.DoEvents();
        }

        /// <summary>
        /// 关闭按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (deviceHandle != null)
            {
                string errMsg="";
                deviceHandle.CloseAllDevice(ref errMsg);
            }
            this.Close();
            System.Environment.Exit(0);
            //关闭所有窗体
            Application.Current.Shutdown();
        }

       /// <summary>
       /// 鼠标点在标题栏，拖动响应
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void Title_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
                App.DoEvents();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {            
            /*KeyDownInfo keyInfo = new KeyDownInfo();
            keyInfo.Key = e.Key;
            EventAggregator.GetEvent<EventListKeyDown>().Publish(keyInfo);
            e.Handled = false;*/
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            KeyDownInfo keyInfo = new KeyDownInfo();
            keyInfo.Key = e.Key;
            EventAggregator.GetEvent<EventListKeyDown>().Publish(keyInfo);
            e.Handled = false;
        }
    }
}
