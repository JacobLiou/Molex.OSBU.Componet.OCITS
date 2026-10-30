using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

namespace TccTempTest
{
    static class Program
    {
        /// <summary>产线约定：TCC 探针必须以该 Windows 账号运行（UDL/串口权限）。</summary>
        public const string RequiredWindowsUser = "PRED-TESTING";

        private static string[] probeDirs;

        [STAThread]
        static void Main(string[] args)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            string parentCommon = null;
            try
            {
                DirectoryInfo parent = Directory.GetParent(exeDir);
                if (parent != null)
                    parentCommon = Path.Combine(parent.FullName, "common");
            }
            catch
            {
            }
            probeDirs = new[]
            {
                exeDir,
                Path.Combine(exeDir, "module"),
                Path.Combine(exeDir, "common"),
                parentCommon
            };
            AppDomain.CurrentDomain.AssemblyResolve += ModuleAssemblyResolve;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool skipUserCheck = HasArg(args, "--skip-user-check");
            if (!skipUserCheck && !IsRequiredWindowsUser())
            {
                if (!PromptRelaunchAsRequiredUser())
                    return;
                // 已拉起 runas 新进程，当前进程退出
                return;
            }

            Application.Run(new MainForm());
        }

        public static bool IsRequiredWindowsUser()
        {
            string user = Environment.UserName ?? "";
            if (string.Equals(user, RequiredWindowsUser, StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                string full = WindowsIdentity.GetCurrent().Name ?? "";
                // DOMAIN\PRED-TESTING 或 MACHINE\PRED-TESTING
                int slash = full.LastIndexOf('\\');
                string bare = slash >= 0 ? full.Substring(slash + 1) : full;
                if (string.Equals(bare, RequiredWindowsUser, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch
            {
            }
            return false;
        }

        public static string GetCurrentWindowsIdentityDisplay()
        {
            try
            {
                return WindowsIdentity.GetCurrent().Name ?? Environment.UserName;
            }
            catch
            {
                return Environment.UserName ?? "(unknown)";
            }
        }

        /// <summary>
        /// 非 PRED-TESTING 时提示，并可选通过 runas 以目标账号重启本 exe。
        /// </summary>
        private static bool PromptRelaunchAsRequiredUser()
        {
            string current = GetCurrentWindowsIdentityDisplay();
            string exePath = Application.ExecutablePath;
            string domainUser = string.IsNullOrEmpty(Environment.UserDomainName)
                ? RequiredWindowsUser
                : (Environment.UserDomainName + "\\" + RequiredWindowsUser);

            DialogResult choice = MessageBox.Show(
                string.Format(
                    "TccTempTest 必须以 Windows 账号 {0} 启动。\r\n\r\n" +
                    "当前账号: {1}\r\n\r\n" +
                    "是 = 用 runas 切换到 {2} 重新启动（将提示输入密码）\r\n" +
                    "否 = 退出\r\n\r\n" +
                    "开发调试可加参数: --skip-user-check",
                    RequiredWindowsUser, current, domainUser),
                "需要 PRED-TESTING 账号",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (choice != DialogResult.Yes)
                return false;

            try
            {
                // runas 会打开控制台要密码；/savecred 仅在本机曾保存过凭据时免密（按需）
                string args = string.Format("/user:{0} \"{1}\"", domainUser, exePath);
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "runas.exe",
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? exeDirOrEmpty(exePath)
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "无法启动 runas: " + ex.Message + "\r\n\r\n" +
                    "请先注销并以 " + RequiredWindowsUser + " 登录 Windows，再运行 TccTempTest.exe。\r\n" +
                    "也可双击同目录 Start-As-PRED-TESTING.bat。",
                    "TccTempTest",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private static string exeDirOrEmpty(string exePath)
        {
            try
            {
                return Path.GetDirectoryName(exePath) ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static bool HasArg(string[] args, string name)
        {
            if (args == null)
                return false;
            foreach (string a in args)
            {
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static Assembly ModuleAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string simpleName = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(simpleName))
                    return null;

                foreach (string dir in probeDirs)
                {
                    if (string.IsNullOrEmpty(dir))
                        continue;
                    string path = Path.Combine(dir, simpleName + ".dll");
                    if (!File.Exists(path))
                        continue;
                    return Assembly.LoadFrom(path);
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
