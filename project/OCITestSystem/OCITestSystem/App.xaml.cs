using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;

namespace OCITestSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrames), frame);
            try
            {
                Dispatcher.PushFrame(frame);
            }
            catch (InvalidOperationException)
            {
            }
        }
        private static object ExitFrames(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string[] args = e.Args;
            //rjf test
           /* if (args.Length == 0)
            {
                MessageBox.Show("Mims传入信息为空！");
            }
            else
            {
                //MFG/Debug/RD

                string testArg = "";
                foreach (string str in args)
                {
                    if (str.Contains("Type"))
                    {
                        testArg += " ";
                        testArg += str;
                    }
                    else if (str.Contains("\"ProgInfo\""))
                    {
                        testArg += str;
                    }
                    else if (str.Contains("ProgInfo"))
                    {
                        testArg += str.Replace("ProgInfo", "\"ProgInfo\"");
                    }
                    else
                    {
                        testArg += str;
                    }
                }*/
                //rjf test
                string testArg = "<MIMS>\r\n<AppInfo Type = \"ProgInfo\">\r\n<User>24351</User>\r\n<PN>1831760177</PN>\r\n<SN>CFOIC0001</SN>\r\n<Process>Interleaver-ITL-终测CD</Process>\r\n<LoginMode>RDModule</LoginMode>\r\n<SoftwareID>NA</SoftwareID>\r\n<MesMode>MESOnline</MesMode>\r\n<CheckUser>9353</CheckUser>\r\n<CheckPwd>AF3F3335190E8D197A020F41360EBA5B</CheckPwd>\r\n</AppInfo>\r\n</MIMS>\r\n";
                //MessageBox.Show(testArg);
                MainWindow mainWindow = new OCITestSystem.MainWindow(testArg);
                mainWindow.Show();
            //}
        }
    }
}
