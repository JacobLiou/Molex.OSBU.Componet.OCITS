using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MolexUtility.Device
{
    public interface IPDLController
    {
        /// <summary>
        /// 开始摇偏振控制器
        /// </summary>
        /// <param name="nPDLIdx">摇第几个偏振控制器</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败 2-不支持</returns>
        int DoPDL(int nPDLIdx,ref string errMsg);

        /// <summary>
        /// 查询偏振控制器摇是否结束
        /// </summary>
        /// /// <param name="errMsg">错误信息</param>
        /// <returns>true--结束  false--未结束</returns>
        bool IsPDLFinish(ref string errMsg);
    }
}
