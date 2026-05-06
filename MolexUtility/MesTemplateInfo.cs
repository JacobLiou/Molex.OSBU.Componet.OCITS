using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Text;

namespace MolexUtility
{
    [Serializable]
    public class ScanData
    {
        /// <summary>
        /// 功率计通道号
        /// </summary>
        public int PowerMeterChannel;

        /// <summary>
        /// 扫描起始波长
        /// </summary>
        public double StartWL;

        /// <summary>
        /// 扫描终止波长
        /// </summary>
        public double StopWL;

        /// <summary>
        /// 扫描步长
        /// </summary>
        public double ScanStep;

        /// <summary>
        /// 扫描数据对应的通道号
        /// </summary>
        public string PortName;

        /// <summary>
        /// 0-归零、1-测试、2-gff数据
        /// </summary>
        public int Type;

        /// <summary>
        /// 扫描数据对应参数，例如memsvoa 0.5db扫描数据
        /// </summary>
        public double SettingValue;

        /// <summary>
        /// 扫描对应波长点
        /// </summary>
        List<double> ScanWL;

        /// <summary>
        /// 扫描数据
        /// </summary>
        List<double> ScanRawData;
    }

    public class AMTSRawdata
    {
        public string PortName { get; set; }
        public double Temperature { get; set; }

        public string Rawdata { get; set; }
        public AMTSRawdata()
        {
            PortName = "";
            Temperature = 0.0;
            Rawdata = "";
        }
    }

    [Serializable]
    public class MESTestInfo
    {
        /// <summary>
        /// 参数规则类型,如果为ex自定义，TestParam为0
        /// </summary>
        public MESParamRule ParamType { get; set; }
        public ulong BandType { get; set; }
        public double StartWL { get; set; }
        public double StopWL { get; set; }
        public double Step { get; set; }
        public double ITU { get; set; }
        public double Temperature { get; set; }
        public string TemperStr { get; set; }
        public string SaveTemperature { get; set; }
        public double TmptChangeTimes { get; set; }
        public MESParam TestParam { get; set; }
        public string ExParamName { get; set; }
        public string PortNameForAMTS { get; set; }
        public string PortNameForUser { get; set; }   //软件显示给用户的端口名
        public double SettingValue { get; set; }
        public double WLLeft { get; set; }
        public double WLRight { get; set; }

        public string Passband { get; set; }
        public string Deepth { get; set; }
        public string Voltage { get; set; }
        public string Atten { get; set; }

        public string ReverseVolt { get; set; }

        public string BPParamSet { get; set; }

        public string BPCurrentSet { get; set; }

        public double PowerLeft { get; set; }
        public double PowerRight { get; set; }
        public string Criterion { get; set; }
        public string Criterion1 { get; set; }
        public string FreeLowestCriterion { get; set; }
        public string FreeHighestCriterion { get; set; }
        //public double FreeLowestCriterion { get; set; }
        //public double FreeHighestCriterion { get; set; }
        //public string StrCriterion { get; set; }
        //public string StrCriterion1 { get; set; }
        public double TestedValue { get; set; }  //从无纸化取得之前保存值
        public double CurValue { get; set; }     //当前测试的值
        public bool Pass { get; set; }           //是否合格
        public bool Tested { get; set; }         //是否测试过
        public long RowIndex { get; set; }     //显示相关index
        //列index和名称如何决定
        //如果自定义，列名就用m_PortNameForUser，其他使用ParamInfoEnum.GetStrTestTemplate
        public string ParamColumnName { get; set; }

        /// <summary>
        /// IL归零值
        /// </summary>
        public double ILRef { get; set; }

        /// <summary>
        /// RL归零值
        /// </summary>
        public double RLRef { get; set; }

        public bool IsScanRef { get; set; }

        /// <summary>
        /// 进光功率
        /// </summary>
        public double InPower { get; set; }

        /// <summary>
        /// 出光功率
        /// </summary>
        public double OutPower { get; set; }

        public string ObjectID { get; set; }

        public string EnvironmentID { get; set; }

        public string PortID { get; set; }

        public string ConditionID { get; set; }
        public string Units { get; set; }
        public string Active { get; set; }
        public string Scale { get; set; }
        public string Filename { get; set; }
        public string TestDate { get; set; }

        
        public MESTestInfo()
        {
            Clear();
        }
        public void Clear()
        {
            ParamType = MESParamRule.Default;
            BandType = 0;
            StartWL = 0.0;
            StopWL = 0.0;
            Step = 0.0;
            ITU = 0.0;
            Temperature = 0.0;
            TmptChangeTimes = 0.0;
            TestParam = MESParam.Default;
            PortNameForAMTS = "";
            PortNameForUser = "";
            SettingValue = 0.0;
            WLLeft = 0.0;
            WLRight = 0.0;
            PowerLeft = 0.0;
            PowerRight = 0.0;
            Criterion = "";
            Criterion1 = "";
            //StrCriterion = "";
            //StrCriterion1 = "";
            FreeLowestCriterion = "";
            FreeHighestCriterion = "";
            TestedValue = CommonFunction.GetDefaultValue();
            CurValue = CommonFunction.GetDefaultValue();
            Pass = true;
            Tested = false;
            RowIndex = -1;
            ParamColumnName = "";
            ILRef = CommonFunction.GetDefaultValue();
            RLRef = CommonFunction.GetDefaultValue();
            IsScanRef = false;
            InPower = CommonFunction.GetDefaultValue();
            OutPower = CommonFunction.GetDefaultValue();
            ObjectID = "";
            EnvironmentID = "";
            PortID = "";
            ConditionID = "";
            Units = "";
            Active = "";
            Scale = "";
            Filename = "";
            TestDate = "";
            Passband = "";
            Deepth = "";
            Voltage = "";
            Atten = "";
            ReverseVolt = "";
            TemperStr = "";
    }

    public MESTestInfo Clone()
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return formatter.Deserialize(stream) as MESTestInfo;
            //return this.MemberwiseClone() as MESTestInfo;
        }
    }

    [Serializable]
    public class MESGlobalSetting
    {
        public int TmptCount { get; set; }
        public double[] TmptArray { get; set; }
        public double[] TmptTimeArray { get; set; }
        public double ITU { get; set; }
        public double Vstart { get; set; }
        public int SelfLock { get; set; }
        public int SwitchType { get; set; }
        public int OSAStep { get; set; }
        public int FreeCheckType { get; set; }
        public double MEMSVOAVolt { get; set; }
        public int MEMSVOAType { get; set; }
        public double MEMSVOAStep { get; set; }
        public double MEMSVOAMaxVolt { get; set; }
        public double Vmin { get; set; }
        public int ExactModel { get; set; }
        public double TBLLValue { get; set; }
        public double TBLLValue1 { get; set; }
        public double TBLHValue { get; set; }
        public double TBLHValue1 { get; set; }
        public double TBLRValue { get; set; }
        public double TBLRValue1 { get; set; }
        public int PDPin1Type { get; set; }
        public int PDPin2Type { get; set; }
        public int PDPin3Type { get; set; }
        public int PDPin4Type { get; set; }
        public int PDPin5Type { get; set; }
        public int PDPin6Type { get; set; }
        public int PDPin7Type { get; set; }
        public int PDPin8Type { get; set; }
        public int PDPin9Type { get; set; }
        public int PDPin10Type { get; set; }
        public int PDPin11Type { get; set; }
        public int PDPin12Type { get; set; }
        public int PDPin13Type { get; set; }
        public int PDPin14Type { get; set; }
        public string PDPinGroup { get; set; }
        public int PDPinCount { get; set; }
        public int EFPeak { get; set; }

        public MESGlobalSetting()
        {
            Clear();
        }

        public MESGlobalSetting Clone()
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return formatter.Deserialize(stream) as MESGlobalSetting;

        }

        public void Clear()
        {
            TmptCount = 0;
            TmptArray = new double[4];
            TmptTimeArray = new double[4];
            ITU = 0;
            Vstart = 0;
            SelfLock = 0;
            SwitchType = 0;
            OSAStep = 0;
            FreeCheckType = 0;
            MEMSVOAVolt = 0;
            MEMSVOAType = 0;
            MEMSVOAStep = 0;
            MEMSVOAMaxVolt = 0;
            Vmin = 0;
            ExactModel = 0;
            TBLLValue = 0;
            TBLLValue1 = 0;
            TBLHValue = 0;
            TBLHValue1 = 0;
            TBLRValue = 0;
            TBLRValue1 = 0;
            PDPin1Type = 0;
            PDPin2Type = 0;
            PDPin3Type = 0;
            PDPin4Type = 0;
            PDPin5Type = 0;
            PDPin6Type = 0;
            PDPin7Type = 0;
            PDPin8Type = 0;
            PDPin9Type = 0;
            PDPin10Type = 0;
            PDPin11Type = 0;
            PDPin12Type = 0;
            PDPin13Type = 0;
            PDPin14Type = 0;
            PDPinGroup = "";
            PDPinCount = 0;
            EFPeak = 0;
        }
    }

    [Serializable]
    public class MESProductInfo
    {
        public string SN { get; set; }//PROD_SN
        public string TemplateID { get; set; }
        public string ProductPN { get; set; }//PROD_PN
        public string Version { get; set; }
        public string PC { get; set; }
        public string PT { get; set; }
        public string SO { get; set; }
        public string Spec { get; set; }
        public string SpecNO { get; set; }
        public string DeviceNO { get; set; }
        public string SetupModel { get; set; }
        public string FinishModel { get; set; }
        public string Precheck { get; set; }
        public string GSStatus { get; set; }
        public string GSDate { get; set; }
        public string Hint { get; set; }
        public string TempletHint { get; set; }
        public string FreeCheckType { get; set; }
        public string ProcessType { get; set; }
        public string ProcessTypeUp { get; set; }

        public string ProductCategory { get; set; }
        public string ProductFamily { get; set; }
        public string ProductType { get; set; }
        public string ProductName { get; set; }
        public string ProductRev { get; set; }
        public string ProductPhase { get; set; }
        public string InProcess { get; set; }
        public string Hold { get; set; }
        public string Rework { get; set; }
        public string SerialNo1 { get; set; }
        public string SerialNo2 { get; set; }
        public string SerialNo3 { get; set; }
        public string WONum { get; set; }
        public string WOType { get; set; }
        public string WOPlanedDate { get; set; }
        public string WOIssueDate { get; set; }
        public string LotNum { get; set; }
        public string SpecNum { get; set; }
        public string SpecRev { get; set; }
        public string CsmName { get; set; }
        public string WorkflowName { get; set; }
        public string CurProcess { get; set; }
        public string PreProcess { get; set; }
        public string Parent { get; set; }
        public string Status { get; set; }
        public string Operation { get; set; }
        public string Qty { get; set; }
        public string MaterialCategory { get; set; }

        public MESProductInfo()
        {
            Clear();
        }
        public void Clear()
        {
            SN = "";
            TemplateID = "";
            ProductPN = "";
            Version = "";
            PC = "";
            PT = "";
            SO = "";
            Spec = "";
            SpecNO = "";
            DeviceNO = "";
            SetupModel = "";
            FinishModel = "";
            Precheck = "";
            GSStatus = "";
            GSDate = "";
            Hint = "";
            TempletHint = "";
            FreeCheckType = "";
            ProcessType = "";
            ProcessTypeUp = "";
            ProductCategory = "";
            ProductFamily = "";
            ProductType = "";
            ProductName = "";
            ProductRev = "";
            ProductPhase = "";
            InProcess = "";
            Hold = "";
            Rework = "";
            SerialNo1 = "";
            SerialNo2 = "";
            SerialNo3 = "";
            WONum = "";
            WOType = "";
            WOPlanedDate = "";
            WOIssueDate = "";
            LotNum = "";
            SpecNum = "";
            SpecRev = "";
            CsmName = "";
            WorkflowName = "";
            CurProcess = "";
            PreProcess = "";
            Parent = "";
            Status = "";
            Operation = "";
            Qty = "";
            MaterialCategory = "";
        }

        public MESProductInfo Clone()
        {
            /*MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return formatter.Deserialize(stream) as MESProductInfo;*/
            return this.MemberwiseClone() as MESProductInfo;
        }
    }

    [Serializable]
    public class DocrevRecordInfo
    {
        public string Author { get; set; }
        public string CreationDate { get; set; }
        public string Version { get; set; }
        public string ApprovedBy { get; set; }
        public string ApprovedDate { get; set; }
        public string RecipeID { get; set; }
        public string InventoryRecheck { get; set; }
        public string ProdPN { get; set; }
        public string ProdProcess { get; set; }
        public DocrevRecordInfo()
        {
            Author = "";
            CreationDate = "";
            Version = "";
            ApprovedBy = "";
            ApprovedDate = "";
            RecipeID = "";
            InventoryRecheck = "";
            ProdPN = "";
            ProdProcess = "";
        }
    }

    [Serializable]
    public class RecipeRecordInfo
    {
        public string RecipeCRC { get; set; }
        public string ProdSN { get; set; }
        public string ProdProcess { get; set; }
        public string FW_PN { get; set; }
        public string FW_VER { get; set; }
        public string FW_Author { get; set; }
        public string FW_Date { get; set; }
        public string FW_Status { get; set; }
        public string TS_PN { get; set; }
        public string TS_VER { get; set; }
        public string TS_Author { get; set; }
        public string TS_Date { get; set; }
        public string TS_Status { get; set; }
        public string CS_PN { get; set; }
        public string CS_VER { get; set; }
        public string CS_Author { get; set; }
        public string CS_Date { get; set; }
        public string CS_Status { get; set; }
        public string TP_PN { get; set; }
        public string TP_VER { get; set; }
        public string TP_Author { get; set; }
        public string TP_Date { get; set; }
        public string TP_Status { get; set; }
        public string TT_PN { get; set; }
        public string TT_VER { get; set; }
        public string TT_Author { get; set; }
        public string TT_Date { get; set; }
        public string TT_Status { get; set; }
        public string Workflow_Name { get; set; }
        public string Operation_Name { get; set; }
        public RecipeRecordInfo()
        {
            RecipeCRC = "";
            ProdSN = "";
            ProdProcess = "";
            FW_PN = "";
            FW_VER = "";
            FW_Author = "";
            FW_Date = "";
            FW_Status = "";
            TS_PN = "";
            TS_VER = "";
            TS_Author = "";
            TS_Date = "";
            TS_Status = "";
            CS_PN = "";
            CS_VER = "";
            CS_Author = "";
            CS_Date = "";
            CS_Status = "";
            TP_PN = "";
            TP_VER = "";
            TP_Author = "";
            TP_Date = "";
            TP_Status = "";
            TT_PN = "";
            TT_VER = "";
            TT_Author = "";
            TT_Date = "";
            TT_Status = "";
            Workflow_Name = "";
            Operation_Name = "";
        }
    }

    [Serializable]
    public class MFGRecordInfo
    {
        public string ProdSN { get; set; }
        public string ProdProcess { get; set; }
        public string WONum { get; set; }
        public string Operator { get; set; }
        public string PermsLevel { get; set; }
        public string TestArea { get; set; }
        public string TestStation { get; set; }
        public string TesterID { get; set; }
        public string TestType { get; set; }
        public string ATECode { get; set; }
        public string ReferFile { get; set; }
        public string GRRStatus { get; set; }
        public string GDSStatus { get; set; }
        public string ESDStatus { get; set; }
        public string MoveIn { get; set; }
        public string MoveOut { get; set; }
        public string FailureCode { get; set; }
        public string TestResult { get; set; }
        public MFGRecordInfo()
        {
            ProdSN = "";
            ProdProcess = "";
            WONum = "";
            Operator = "";
            PermsLevel = "";
            TestArea = "";
            TestStation = "";
            TesterID = "";
            TestType = "";
            ATECode = "";
            ReferFile = "";
            GRRStatus = "";
            GDSStatus = "";
            ESDStatus = "";
            MoveIn = "";
            MoveOut = "";
            FailureCode = "";
            TestResult = "";
        }
    }


    [Serializable]
    public class CFGRecordInfo
    {
        public string SectionName { get; set; }
        public string SectionDesc { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Desc { get; set; }
        public string Units { get; set; }
        public string Scale { get; set; }
        public CFGRecordInfo()
        {
            SectionName = "";
            SectionDesc = "";
            Name = "";
            Value = "";
            Desc = "";
            Units = "";
            Scale = "";
        }
    }

    [Serializable]
    public class MISCRecordInfo
    {
        public string ObjectValue { get; set; }
        public string ObjectDesc { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Desc { get; set; }
        public MISCRecordInfo()
        {
            ObjectValue = "";
            ObjectDesc = "";
            Name = "";
            Value = "";
            Desc = "";
        }
    }

    [Serializable]
    public class FusionEnvironmentInfo
    {
        public string EnvironmentID { get; set; }
        public string EnvironmentDesc { get; set; }
        public string Active { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Value { get; set; }
        public string Units { get; set; }
        public string Scale { get; set; }
        public FusionEnvironmentInfo()
        {
            EnvironmentID = "";
            EnvironmentDesc = "";
            Active = "1";
            Name = "";
            Desc = "";
            Value = "";
            Units = "";
            Scale = "1";
        }
    }

    [Serializable]
    public class FusionObjectInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }

        public string Instance { get; set; }
        public string Active { get; set; }
        public FusionObjectInfo()
        {
            Instance = "";
            Name = "";
            Desc = "";
            Active = "1";
        }
    }

    [Serializable]
    public class FusionPortInfo
    {
        public string Value { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Active { get; set; }
        public FusionPortInfo()
        {
            Value = "";
            Name = "";
            Desc = "";
            Active = "1";
        }
    }

    [Serializable]
    public class FusionConditionInfo
    {
        public string ConditionID { get; set; }
        public string ConditionDesc { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Value { get; set; }
        public string Units { get; set; }
        public string Scale { get; set; }
        public FusionConditionInfo()
        {
            ConditionID = "";
            ConditionDesc = "";
            Name = "";
            Desc = "";
            Value = "";
            Units = "";
            Scale = "1";
        }
    }
}
