using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MoUtilityLib
{
    public class CouplerMaterial
    {
        public static bool GetValidateEMSStation(string strUrl,string strStationID)
        {
            string[] args = new string[1];
            args[0] = strStationID;
            object res = WebServiceHelper.InvokeWebService(strUrl, "ValidateEMSStation", args);
            bool bSuccess = true;
            if (res.ToString() == bSuccess.ToString())
                return true;
            return false;
        }

        public static bool GetValidateWorkStation(string strUrl, string strStationID)
        {
            string[] args = new string[1];
            args[0] = strStationID;
            object res = WebServiceHelper.InvokeWebService(strUrl, "ValidateWorkStation", args);
            bool bSuccess = true;
            if (res.ToString() == bSuccess.ToString())
                return true;
            return false;
        }

        public static string GetValidateLotNumber(string strUrl, string strLotNo, string strOrderNo)
        {
            string[] args = new string[2];
            args[0] = strLotNo;
            args[1] = strOrderNo;
            object res = WebServiceHelper.InvokeWebService(strUrl, "ValidateLotNumber", args);
            return res.ToString();
        }

        public static bool GetValidateSISPosition(string strUrl, string strUserID, string strProcessType)
        {
            string[] args = new string[2];
            args[0] = strUserID;
            args[1] = strProcessType;
            object res = WebServiceHelper.InvokeWebService(strUrl, "ValidateSISPosition", args);
            bool bSuccess = true;
            if (res.ToString() == bSuccess.ToString())
                return true;
            return false;
        }

        public static string GetValidateWMSLabel4SN(string strUrl, string strLabelNum, string strSN)
        {
            string[] args = new string[2];
            args[0] = strLabelNum;
            args[1] = strSN;
            object res = WebServiceHelper.InvokeWebService(strUrl, "ValidateWMSLabel4SN", args);
            return res.ToString();
        }

        public static string GetValidateWMSLabel4WO(string strUrl, string strLabelNum, string strOrderNo)
        {
            string[] args = new string[2];
            args[0] = strLabelNum;
            args[1] = strOrderNo;
            object res = WebServiceHelper.InvokeWebService(strUrl, "ValidateWMSLabel4WO", args);
            return res.ToString();
        }
    }
}
