using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UIOperateITLCD
{
    public class CDScanRes
    {
        public static int ReadScanResFromFile(string filename, ref double[][] fre1Rawdata, ref double[][] fre2Rawdata, ref string errMsg)
        {
            if (!File.Exists(filename))
            {
                errMsg = string.Format("文件不存在:{0}", filename);
                return 1;
            }

            StreamReader sR = new StreamReader(filename);
            int dataCount = 0;
            while (sR.ReadLine() != null)
            {
                dataCount++;
            }
            //第一行不是数据
            dataCount--;
            fre1Rawdata = new double[6][];
            fre2Rawdata = new double[2][];
            for (int i = 0; i < 6; i++)
            {
                fre1Rawdata[i] = new double[dataCount];
                Array.Clear(fre1Rawdata[i], 0, dataCount - 1);
            }
            for (int i = 0; i < 2; i++)
            {
                fre2Rawdata[i] = new double[dataCount - 1];
                Array.Clear(fre2Rawdata[i], 0, dataCount - 2);
            }

            sR.BaseStream.Seek(0, SeekOrigin.Begin);
            sR.DiscardBufferedData();
            //第一行不是数据
            string line = sR.ReadLine();
            line = sR.ReadLine();
            int dataIndex = dataCount - 1;
            while (line != null)
            {
                string[] splitDatas = line.Split(',');
                for (int i = 0; i < splitDatas.Length; i++)
                {
                    if (i < fre1Rawdata.Length)
                    {
                        fre1Rawdata[i][dataIndex] = Convert.ToDouble(splitDatas[i]);
                    }
                    else
                    {
                        if (splitDatas[i].Length > 0)
                        {
                            fre2Rawdata[i - 6][dataIndex-1] = Convert.ToDouble(splitDatas[i]);
                        }
                    }
                }
                dataIndex--;
                line = sR.ReadLine();
            }
            sR.Close();
            return 0;
        }
    }
}
