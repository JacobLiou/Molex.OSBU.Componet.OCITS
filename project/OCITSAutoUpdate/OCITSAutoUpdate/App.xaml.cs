using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Diagnostics;

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

namespace OCITSAutoUpdate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        string localPath = @"C:\Users\Public\software\OCITS";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string[] args = e.Args;
            if (args.Length == 0)
            {
                Login login = new Login();
                login.Show();
            }
            else
            {
                string[] argsArr = args[0].Split(';');

                string path = localPath + "\\" + argsArr[0] + "_" + argsArr[1] + "_" + argsArr[2] + "\\OCITSAutoUpdate_1";
                string localDir = localPath + "\\" + argsArr[0] + "_" + argsArr[1] + "_" + argsArr[2] + "\\OCITSAutoUpdate";
                string errMsg = "";
                int sum = 200;
                int count = 0;
                while (count < sum)
                {
                    Common.CopyFolder(path, localDir, ref errMsg, true, true);
                    if (errMsg != "")
                    {
                        Thread.Sleep(50);
                        errMsg = "";
                        Common.CopyFolder(path, localDir, ref errMsg, true, true);
                        count++;
                    }
                    else
                        break;
                }
                Login login = new Login(args[0]);
                login.Show();
            }
        }
        
    }
}
