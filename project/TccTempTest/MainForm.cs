using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DeviceControl;
using MolexUtility.Device;

namespace TccTempTest
{
    public partial class MainForm : Form
    {
        private DeviceHandle deviceHandle;
        private IUDLTCC tcc;
        private bool busy;
        private string logFilePath;
        private volatile string lastInitStep = "";
        private volatile bool initInProgress;

        public MainForm()
        {
            InitializeComponent();
            string exeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            if (!string.Equals(Environment.CurrentDirectory, exeDir, StringComparison.OrdinalIgnoreCase))
                Environment.CurrentDirectory = exeDir;
            logFilePath = Path.Combine(exeDir, "TccTempTest.log");

            UdlUiSync.Capture();
            this.Text = "TCC 温度测试 (DeviceControl) — " + Program.GetCurrentWindowsIdentityDisplay();
            AppendLog("工作目录: " + Environment.CurrentDirectory);
            AppendLog("Windows 账号: " + Program.GetCurrentWindowsIdentityDisplay()
                + (Program.IsRequiredWindowsUser() ? " [PRED-TESTING OK]" : " [非 PRED-TESTING]"));
            AppendLog("诊断日志文件: " + logFilePath);
            AppendLog("模式: InitTccOnly（只建 Engine+TCC，跳过 FSTP/OSW 与 Deviceconfig 其它设备）");
            AppendLog("可选: set\\UDLConfig_TCC.xml（仅 TCC 的 UDL 配置，优先于完整 UDLConfig.xml）");
            AppendLog("卡死时看最后 [STEP]/TccTempTest.log 的 HEARTBEAT");
            RunPreflight(exeDir);
        }

        private void RunPreflight(string exeDir)
        {
            AppendLog("--- 启动自检 ---");
            CheckPath(Path.Combine(exeDir, "set", "UDLConfig_TCC.xml"), false);
            CheckPath(Path.Combine(exeDir, "set", "UDLConfig.xml"), true);
            CheckPath(Path.Combine(exeDir, "set", "Deviceconfig.xml"), true);
            CheckPath(Path.Combine(exeDir, "module", "DeviceControl.dll"), true);
            CheckPath(Path.Combine(exeDir, "MolexUtility.dll"), false);
            string parentCommon = null;
            try
            {
                DirectoryInfo parent = Directory.GetParent(exeDir);
                if (parent != null)
                    parentCommon = Path.Combine(parent.FullName, "common", "MolexUtility.dll");
            }
            catch
            {
            }
            if (!string.IsNullOrEmpty(parentCommon))
                CheckPath(parentCommon, false);
            AppendLog("--- 自检结束 ---");
        }

        private void CheckPath(string path, bool required)
        {
            bool ok = File.Exists(path);
            AppendLog(string.Format("{0}: {1}  [{2}]",
                ok ? "OK" : (required ? "缺" : "无"),
                path,
                ok ? "存在" : (required ? "必需" : "可选")));
        }

        private void btnInit_Click(object sender, EventArgs e)
        {
            if (busy)
                return;

            int guid;
            if (!int.TryParse(txtGuid.Text.Trim(), out guid))
            {
                MessageBox.Show("GUID 必须是整数。", "TccTempTest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            busy = true;
            initInProgress = true;
            lastInitStep = "btnInit";
            SetControlsEnabled(false);
            lblStatus.Text = "初始化中…";
            lblStatus.ForeColor = Color.Black;
            Cursor = Cursors.WaitCursor;

            Thread heartbeat = new Thread(InitHeartbeatLoop);
            heartbeat.IsBackground = true;
            heartbeat.Name = "TccTempTest-InitHeartbeat";
            heartbeat.Start();

            try
            {
                AppendLog("======== 开始 InitTccOnly GUID=" + guid + "（跳过 FSTP/OSW/光开关等） ========");
                Application.DoEvents();

                string errMsg = "";
                DeviceHandle handle = new DeviceHandle();
                int initRes = handle.InitTccOnly(ref errMsg, OnInitProgress);

                lastInitStep = "GetUDLTCCByGUID";
                AppendLog("GetUDLTCCByGUID(" + guid + ") …");
                Application.DoEvents();

                IUDLTCC localTcc = null;
                string getErr = "";
                int getRes = handle.GetUDLTCCByGUID(guid, ref localTcc, ref getErr);
                if (getRes != 0)
                    localTcc = null;

                deviceHandle = handle;
                tcc = localTcc;

                if (!string.IsNullOrEmpty(errMsg))
                    AppendLog(string.Format("InitTccOnly 返回 {0}: {1}", initRes, errMsg));
                else
                    AppendLog(string.Format("InitTccOnly 返回 {0}", initRes));

                if (!string.IsNullOrEmpty(getErr))
                    AppendLog("GetUDLTCCByGUID: " + getErr);

                string udlLog = Path.Combine(Environment.CurrentDirectory, "UDLlog.txt");
                if (File.Exists(udlLog))
                    AppendLog("UDLlog.txt 已生成: " + udlLog + " size=" + new FileInfo(udlLog).Length);
                else
                    AppendLog("警告: 仍无 UDLlog.txt（可能未执行到 SetDebugLogFile，或 UDL 未加载）");

                if (tcc != null)
                {
                    if (initRes != 0)
                    {
                        lblStatus.Text = "TCC 可用（部分设备失败）";
                        lblStatus.ForeColor = Color.DarkOrange;
                    }
                    else
                    {
                        lblStatus.Text = "已就绪 GUID=" + guid;
                        lblStatus.ForeColor = Color.DarkGreen;
                    }
                    SetTccControlsEnabled(true);
                    AppendLog("======== 初始化完成，可读写温度 ========");
                }
                else
                {
                    lblStatus.Text = "未找到 TCC";
                    lblStatus.ForeColor = Color.DarkRed;
                    SetTccControlsEnabled(false);
                    AppendLog("未能获取 IUDLTCC。请确认 UDLConfig.xml 存在且 Deviceconfig 含 Type=UDLTCC、control0=GUID。");
                    AppendLog("======== 初始化结束（无 TCC） ========");
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "初始化异常";
                lblStatus.ForeColor = Color.DarkRed;
                AppendLog("初始化异常: " + ex.GetType().Name + ": " + ex.Message);
                if (ex.InnerException != null)
                    AppendLog("Inner: " + ex.InnerException.Message);
                AppendLog(ex.StackTrace ?? "");
                SetTccControlsEnabled(false);
            }
            finally
            {
                initInProgress = false;
                Cursor = Cursors.Default;
                btnInit.Enabled = true;
                busy = false;
            }
        }

        private void OnInitProgress(string msg)
        {
            lastInitStep = msg ?? "";
            // 先落盘再刷 UI，卡死在 OpenEngine 时文件里仍能看到最后一步
            WriteLogFile("[STEP] " + msg);
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        AppendLogUiOnly("[STEP] " + msg);
                        Application.DoEvents();
                    }));
                }
                catch
                {
                }
                return;
            }
            AppendLogUiOnly("[STEP] " + msg);
            Application.DoEvents();
        }

        private void InitHeartbeatLoop()
        {
            int sec = 0;
            while (initInProgress)
            {
                Thread.Sleep(5000);
                if (!initInProgress)
                    break;
                sec += 5;
                string step = lastInitStep;
                WriteLogFile(string.Format("[HEARTBEAT] 已等待 {0}s，当前步骤: {1}", sec, step));
                // OpenEngine 卡住时 UI 线程忙，BeginInvoke 可能排队；文件心跳仍可靠
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (initInProgress)
                            AppendLogUiOnly(string.Format("[HEARTBEAT] 已等待 {0}s，仍停在: {1}", sec, step));
                    }));
                }
                catch
                {
                }
            }
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            RunRead(false);
        }

        private void pollTimer_Tick(object sender, EventArgs e)
        {
            RunRead(true);
        }

        private void RunRead(bool fromTimer)
        {
            if (tcc == null || busy)
                return;

            busy = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                string errMsg = "";
                double temp;
                AppendLog("GetCurrentTemp …");
                Application.DoEvents();
                int res = tcc.GetCurrentTemp(out temp, ref errMsg);
                if (res == 0)
                {
                    txtCurrentTemp.Text = temp.ToString("F3");
                    AppendLog(string.Format("读温 OK: {0:F3} °C", temp));
                }
                else
                {
                    AppendLog(string.Format("读温失败 res={0}: {1}", res, errMsg));
                    if (!fromTimer)
                        MessageBox.Show(errMsg, "读温失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AppendLog("读温异常: " + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
                busy = false;
            }
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            double setpoint;
            if (!double.TryParse(txtSetpoint.Text.Trim(), out setpoint))
            {
                MessageBox.Show("设定温度必须是数字。", "TccTempTest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            RunSet(setpoint);
        }

        private void btnLt_Click(object sender, EventArgs e)
        {
            txtSetpoint.Text = "-5";
            RunSet(-5);
        }

        private void btnRt_Click(object sender, EventArgs e)
        {
            txtSetpoint.Text = "25";
            RunSet(25);
        }

        private void btnHt_Click(object sender, EventArgs e)
        {
            txtSetpoint.Text = "55";
            RunSet(55);
        }

        private void RunSet(double setpoint)
        {
            if (tcc == null || busy)
                return;

            busy = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                string errMsg = "";
                AppendLog(string.Format("SetTempSetpoint({0}) …", setpoint));
                Application.DoEvents();
                int res = tcc.SetTempSetpoint(setpoint, ref errMsg);
                if (res == 0)
                    AppendLog(string.Format("设温 OK: {0:F1} °C", setpoint));
                else
                {
                    AppendLog(string.Format("设温失败 res={0} 目标={1:F1}: {2}", res, setpoint, errMsg));
                    MessageBox.Show(errMsg, "设温失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AppendLog("设温异常: " + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
                busy = false;
            }
        }

        private void chkAutoRead_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePollTimer();
        }

        private void nudInterval_ValueChanged(object sender, EventArgs e)
        {
            UpdatePollTimer();
        }

        private void UpdatePollTimer()
        {
            pollTimer.Stop();
            if (chkAutoRead.Checked && tcc != null)
            {
                pollTimer.Interval = (int)nudInterval.Value * 1000;
                pollTimer.Start();
                AppendLog(string.Format("定时读温已开启，间隔 {0} s", nudInterval.Value));
            }
            else if (!chkAutoRead.Checked)
            {
                AppendLog("定时读温已关闭");
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            pollTimer.Stop();
            initInProgress = false;
            if (deviceHandle == null)
                return;
            try
            {
                string errMsg = "";
                deviceHandle.CloseAllDevice(ref errMsg);
                if (!string.IsNullOrEmpty(errMsg))
                    AppendLog("CloseAllDevice: " + errMsg);
            }
            catch (Exception ex)
            {
                AppendLog("关闭设备异常: " + ex.Message);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnInit.Enabled = enabled;
            if (!enabled)
                SetTccControlsEnabled(false);
        }

        private void SetTccControlsEnabled(bool enabled)
        {
            btnRead.Enabled = enabled;
            btnSet.Enabled = enabled;
            btnLt.Enabled = enabled;
            btnRt.Enabled = enabled;
            btnHt.Enabled = enabled;
            chkAutoRead.Enabled = enabled;
            if (!enabled)
            {
                chkAutoRead.Checked = false;
                pollTimer.Stop();
            }
        }

        private void AppendLog(string message)
        {
            WriteLogFile(message);
            AppendLogUiOnly(message);
        }

        private void AppendLogUiOnly(string message)
        {
            if (IsDisposed)
                return;
            string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message + Environment.NewLine;
            txtLog.AppendText(line);
        }

        private void WriteLogFile(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(logFilePath))
                    return;
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine;
                File.AppendAllText(logFilePath, line, Encoding.UTF8);
            }
            catch
            {
            }
        }
    }
}
