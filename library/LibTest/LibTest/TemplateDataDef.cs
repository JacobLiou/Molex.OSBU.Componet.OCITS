using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using MoUtilityLib;

namespace LibTest
{
    public static class EnumHelper
    {
        public static ParamInfoEnum creatType { get; set; }
        public static string StrCodeIndex { get { return creatType.GetStrCodeIndex(); } }
        public static string StrTestTemplate { get { return creatType.GetStrTestTemplate(); } }
        public static string StrSaveName { get { return creatType.GetStrSaveName(); } }

    }

    public class StrCodeIndexAttribute : Attribute
    {
        private string _StrCodeIndex;
        public string StrCodeIndex 
        {
            get
            {
                return _StrCodeIndex;
            }
        }
        public StrCodeIndexAttribute(string value)
        {
            _StrCodeIndex = value;
        }
    }

    public class StrTestTemplateAttribute : Attribute
    {
        private string _StrTestTemplate;
        public string StrTestTemplate 
        { 
            get
            {
                return _StrTestTemplate;
            }
        }

        public StrTestTemplateAttribute(string value)
        {
            _StrTestTemplate = value;
        }
    }

    public class StrSaveNameAttribute : Attribute
    {
        private string _StrSaveName;
        public string StrSaveName
        {
            get
            {
                return _StrSaveName;
            }
        }

        public StrSaveNameAttribute(string value)
        {
            _StrSaveName = value;
        }
    }

    public class AdditionalAttribute : Attribute
    {
        private string _Additional;
        public string Additional
        {
            get
            {
                return _Additional;
            }
        }

        public AdditionalAttribute(string value)
        {
            _Additional = value;
        }
    }

    public enum ParamInfoEnum
    {
        OP_Default = -1,
        OP_DefineEx=0,
        [StrCodeIndex("1")]
        [StrTestTemplate("Central WL")]
        [StrSaveName("CWL")]
        OP_CentralWL = 1,
        [StrCodeIndex("2")]
        [StrTestTemplate("Shift")]
        [StrSaveName("SHIFT")]
        OP_Shift,
        [StrCodeIndex("3")]
        [StrTestTemplate("Peak IL")]
        [StrSaveName("PEAKIL")]
        OP_PeakIL,
        [StrCodeIndex("4")]
        [StrTestTemplate("Ripple")]
        [StrSaveName("RIPPLE")]
        OP_Ripple,
        [StrCodeIndex("5")]
        [StrTestTemplate("Bandwidth")]
        [StrSaveName("BW")]
        OP_Bandwidth,
        [StrCodeIndex("6")]
        [StrTestTemplate("WL Left")]
        [StrSaveName("WLL")]
        OP_WLLeft,
        [StrCodeIndex("7")]
        [StrTestTemplate("WL Right")]
        [StrSaveName("WLR")]
        OP_WLRight,
        [StrCodeIndex("8")]
        [StrTestTemplate("Power Left")]
        [StrSaveName("PWL")]
        OP_PowerLeft,
        [StrCodeIndex("9")]
        [StrTestTemplate("Power Right")]
        [StrSaveName("PWR")]
        OP_PowerRight,
        [StrCodeIndex("10")]
        [StrTestTemplate("PDL")]
        [StrSaveName("PDL")]
        OP_PDL,
        [StrCodeIndex("11")]
        [StrTestTemplate("Max IL")]
        [StrSaveName("MAXIL")]
        OP_MaxIL,
        [StrCodeIndex("12")]
        [StrTestTemplate("WDL")]
        [StrSaveName("WDL")]
        OP_WDL,
        [StrCodeIndex("13")]
        [StrTestTemplate("TDL")]
        [StrSaveName("TDL")]
        OP_TDL,
        [StrCodeIndex("14")]
        [StrTestTemplate("Return Loss")]
        [StrSaveName("RL")]
        OP_ReturnLoss,
        [StrCodeIndex("15")]
        [StrTestTemplate("Directivity")]
        [StrSaveName("DIR")]
        OP_Directivity,
        [StrCodeIndex("16")]
        [StrTestTemplate("AEL")]
        [StrSaveName("AEL")]
        OP_AEL,
        [StrCodeIndex("17")]
        [StrTestTemplate("Slope")]
        [StrSaveName("SLOPE")]
        OP_Slope,
        [StrCodeIndex("18")]
        [StrTestTemplate("NWDL")]
        [StrSaveName("NWDL")]
        OP_NWDL,
        [StrCodeIndex("19")]
        [StrTestTemplate("AT-RES")]
        [StrSaveName("RES")]
        OP_ATRES,
        [StrCodeIndex("20")]
        [StrTestTemplate("AT-Range")]
        [StrSaveName("RANGE")]
        OP_ATRange,
        [StrCodeIndex("21")]
        [StrTestTemplate("Backlash")]
        [StrSaveName("BACKLASH")]
        OP_Backlash,
        [StrCodeIndex("22")]
        [StrTestTemplate("Repeatability")]
        [StrSaveName("ERP")]
        OP_Repeatability,
        [StrCodeIndex("23")]
        [StrTestTemplate("Darkness")]
        [StrSaveName("DARK")]
        OP_Darkness,
        [StrCodeIndex("24")]
        [StrTestTemplate("AD-V")]
        [StrSaveName("ADV")]
        OP_ADV,
        [StrCodeIndex("25")]
        [StrTestTemplate("AD-L")]
        [StrSaveName("ADL")]
        OP_ADL,
        [StrCodeIndex("26")]
        [StrTestTemplate("AD-P25")]
        [StrSaveName("ADP25")]
        OP_ADP25,
        [StrCodeIndex("27")]
        [StrTestTemplate("AD-P75")]
        [StrSaveName("ADP75")]
        OP_ADP75,
        [StrCodeIndex("28")]
        [StrTestTemplate("Vmin")]
        [StrSaveName("VMIN")]
        OP_Vmin,
        [StrCodeIndex("29")]
        [StrTestTemplate("Vmax")]
        [StrSaveName("VMAX")]
        OP_Vmax,
        [StrCodeIndex("30")]
        [StrTestTemplate("MSD")]
        [StrSaveName("MSD")]
        OP_MSD,
        [StrCodeIndex("31")]
        [StrTestTemplate("C-Max IL")]
        [StrSaveName("CMAXIL")]
        OP_CMaxIL,
        [StrCodeIndex("32")]
        [StrTestTemplate("C-WDL")]
        [StrSaveName("CWDL")]
        OP_CWDL,
        [StrCodeIndex("33")]
        [StrTestTemplate("C-PDL")]
        [StrSaveName("CPDL")]
        OP_CPDL,
        [StrCodeIndex("34")]
        [StrTestTemplate("C-TDL")]
        [StrSaveName("CTDL")]
        OP_CTDL,
        [StrCodeIndex("35")]
        [StrTestTemplate("C-RL")]
        [StrSaveName("CRL")]
        OP_CRL,
        [StrCodeIndex("36")]
        [StrTestTemplate("C-CTRep")]
        [StrSaveName("CCTREP")]
        OP_CCTRep,
        [StrCodeIndex("37")]
        [StrTestTemplate("C-Repeatability")]
        [StrSaveName("CREP")]
        OP_CRepeatability,
        [StrCodeIndex("38")]
        [StrTestTemplate("CT")]
        [StrSaveName("CT")]
        OP_CT,
        [StrCodeIndex("39")]
        [StrTestTemplate("C-CT")]
        [StrSaveName("CCT")]
        OP_CCT,
        [StrCodeIndex("40")]
        [StrTestTemplate("Darkness-V")]
        [StrSaveName("DARKV")]
        OP_DarknessV,
        [StrCodeIndex("41")]
        [StrTestTemplate("Leak")]
        [StrSaveName("LEAK")]
        OP_Leak,
        [StrCodeIndex("42")]
        [StrTestTemplate("Bump")]
        [StrSaveName("BUMP")]
        OP_Bump,
        [StrCodeIndex("43")]
        [StrTestTemplate("Flop")]
        [StrSaveName("FLOP")]
        OP_Flop,
        [StrCodeIndex("44")]
        [StrTestTemplate("Set")]
        [StrSaveName("SET")]
        OP_Set,
        [StrCodeIndex("45")]
        [StrTestTemplate("PDR")]
        [StrSaveName("PDR")]
        OP_PDR,
        [StrCodeIndex("46")]
        [StrTestTemplate("WDR")]
        [StrSaveName("WDR")]
        OP_WDR,
        [StrCodeIndex("47")]
        [StrTestTemplate("TDR")]
        [StrSaveName("TDR")]
        OP_TDR,
        [StrCodeIndex("48")]
        [StrTestTemplate("Linearity")]
        [StrSaveName("LINE")]
        OP_Linearity,
        [StrCodeIndex("49")]
        [StrTestTemplate("RES-IN")]
        [StrSaveName("RESIN")]
        OP_RESIN,
        [StrCodeIndex("50")]
        [StrTestTemplate("RES-OUT")]
        [StrSaveName("RESOUT")]
        OP_RESOUT,
        [StrCodeIndex("51")]
        [StrTestTemplate("DK")]
        [StrSaveName("DK")]
        OP_DK,
        [StrCodeIndex("52")]
        [StrTestTemplate("Step")]
        [StrSaveName("STEP")]
        OP_Step,
        [StrCodeIndex("53")]
        [StrTestTemplate("TDR-L")]
        [StrSaveName("TDRL")]
        OP_TDRL,
        [StrCodeIndex("54")]
        [StrTestTemplate("TDR-H")]
        [StrSaveName("TDRH")]
        OP_TDRH,
        [StrCodeIndex("55")]
        [StrTestTemplate("PD-ISO")]
        [StrSaveName("PDISO")]
        OP_PDISO,
        [StrCodeIndex("56")]
        [StrTestTemplate("WDL1")]
        [StrSaveName("WDL1")]
        OP_WDL1,
        [StrCodeIndex("57")]
        [StrTestTemplate("TDR-M")]
        [StrSaveName("TDRM")]
        OP_TDRM,
        [StrCodeIndex("58")]
        [StrTestTemplate("WDR-M")]
        [StrSaveName("WDRM")]
        OP_WDRM,
        [StrCodeIndex("59")]//OK
        [StrTestTemplate("Uniformity")]
        [StrSaveName("UNI")]
        OP_Uniformity,
        [StrCodeIndex("60")]
        [StrTestTemplate("Adj")]
        [StrSaveName("ADJ")]
        OP_Adj,
        [StrCodeIndex("61")]
        [StrTestTemplate("NonAdj")]
        [StrSaveName("NONADJ")]
        OP_NonAdj,
        [StrCodeIndex("62")]
        [StrTestTemplate("CTRep")]
        [StrSaveName("CTREP")]
        OP_CTRep,
        [StrCodeIndex("63")]
        [StrTestTemplate("BFL")]
        [StrSaveName("BFL")]
        OP_BFL,
        [StrCodeIndex("64")]
        [StrTestTemplate("EL")]
        [StrSaveName("EL")]
        OP_EL,
        [StrCodeIndex("65")]
        [StrTestTemplate("CR")]
        [StrSaveName("CR")]
        OP_CR,
        [StrCodeIndex("66")]
        [StrTestTemplate("ΔIL")]
        [StrSaveName("ΔIL")]
        OP_DeltaIL,
        [StrCodeIndex("67")]
        [StrTestTemplate("SLOPE1")]
        [StrSaveName("SLOPE1")]
        OP_SLOPE1,
        [StrCodeIndex("68")]
        [StrTestTemplate("Min IL")]
        [StrSaveName("MINIL")]
        OP_MinIL,
        [StrCodeIndex("69")]
        [StrTestTemplate("RLX")]
        [StrSaveName("RLX")]
        OP_RLX,
        [StrCodeIndex("70")]
        [StrTestTemplate("ER")]
        [StrSaveName("ER")]
        OP_ER,
        [StrCodeIndex("71")]
        [StrTestTemplate("WIL")]
        [StrSaveName("WIL")]
        OP_WIL,
        [StrCodeIndex("72")]
        [StrTestTemplate("EF")]
        [StrSaveName("EF")]
        OP_EF,
        [StrCodeIndex("73")]
        [StrTestTemplate("Reflection ISO")]
        [StrSaveName("RISO")]
        OP_ReflectionISO,
        [StrCodeIndex("74")]
        [StrTestTemplate("Slope-Max")]
        [StrSaveName("SLOPEMAX")]
        OP_SlopeMax,
        [StrCodeIndex("75")]
        [StrTestTemplate("Slope-MIN")]
        [StrSaveName("SLOPEMIN")]
        OP_SlopeMIN,
        [StrCodeIndex("76")]
        [StrTestTemplate("Res-R")]
        [StrSaveName("RESR")]
        OP_ResR,
        [StrCodeIndex("77")]
        [StrTestTemplate("Min-RES")]
        [StrSaveName("MINRES")]
        OP_MinRES,
        [StrCodeIndex("78")]
        [StrTestTemplate("Max-RES")]
        [StrSaveName("MAXRES")]
        OP_MaxRES,
        [StrCodeIndex("79")]
        [StrTestTemplate("Locking-WL")]
        [StrSaveName("LOCKINGWL")]
        OP_LockingWL,
        [StrCodeIndex("80")]
        [StrTestTemplate("CuptureRange-L")]
        [StrSaveName("CUPTURERANGEL")]
        OP_CuptureRangeL,
        [StrCodeIndex("81")]
        [StrTestTemplate("CuptureRange-R")]
        [StrSaveName("CUPTURERANGER")]
        OP_CuptureRangeR,
        [StrCodeIndex("82")]
        [StrTestTemplate("Locking-Acry")]
        [StrSaveName("LOCKINGACRY")]
        OP_LockingAcry,
        [StrCodeIndex("83")]
        [StrTestTemplate("Locking-Slope")]
        [StrSaveName("LOCKINGSLOPE")]
        OP_LockingSlope,
        [StrCodeIndex("84")]
        [StrTestTemplate("Contrast")]
        [StrSaveName("CONTRAST")]
        OP_Contrast,
        [StrCodeIndex("85")]
        [StrTestTemplate("PDA")]
        [StrSaveName("PDA")]
        OP_PDA,
        [StrCodeIndex("86")]
        [StrTestTemplate("Distance")]
        [StrSaveName("DIS")]
        OP_Distance,
        [StrCodeIndex("87")]
        [StrTestTemplate("PD1 DIF")]
        [StrSaveName("PD1DIF")]
        OP_PD1DIF,
        [StrCodeIndex("88")]
        [StrTestTemplate("EF-Pmax")]
        [StrSaveName("EFPMAX")]
        OP_EFPmax,
        [StrCodeIndex("89")]
        [StrTestTemplate("Res ratio")]
        [StrSaveName("RESRATIO")]
        OP_Resratio,
        [StrCodeIndex("90")]
        [StrTestTemplate("EF-Pmin")]
        [StrSaveName("EFPMIN")]
        OP_EFPmin,
        [StrCodeIndex("91")]
        [StrTestTemplate("Axis")]
        [StrSaveName("AXIS")]
        OP_Axis,
        [StrCodeIndex("92")]
        [StrTestTemplate("IL-Change")]
        [StrSaveName("ILCHANGE")]
        OP_ILChange,
        [StrCodeIndex("93")]
        [StrTestTemplate("Dop")]
        [StrSaveName("DOP")]
        OP_Dop,
        [StrCodeIndex("94")]
        [StrTestTemplate("PMDN")]
        [StrSaveName("PMDN")]
        OP_PMDN,
        [StrCodeIndex("95")]
        [StrTestTemplate("WDL2")]
        [StrSaveName("WDL2")]
        OP_WDL2,
        [StrCodeIndex("96")]
        [StrTestTemplate("MIL")]
        [StrSaveName("MIL")]
        OP_MIL,
        [StrCodeIndex("97")]
        [StrTestTemplate("C-Dir")]
        [StrSaveName("CDIR")]
        OP_CDir,
        [StrCodeIndex("98")]
        [StrTestTemplate("CHK")]
        [StrSaveName("CHK")]
        OP_CHK,
        [StrCodeIndex("99")]
        [StrTestTemplate("CIE-OUTRES")]
        [StrSaveName("CIEOUTRES")]
        OP_CIEOUTRES
    }

    //1--DB 2--ITU  3--EX  4--WL  
    public enum ParamRuleEnum
    {
        [Additional("")]
        PARAM_DEFAULT=0,
        [Additional("DB")]
        PARAM_DB=1,
        [Additional("ITU")]
        PARAM_ITU,
        [Additional("EX")]
        PARAM_EX,
        [Additional("WL")]
        PARAM_WL
    }

    //模板类型枚举
    public enum TemplateTypeEnum
    {
        [Additional("Atd_GenerateTempletIni.aspx?serialNo=")]
        Template_GFQC=1,
        [Additional("Atd_GenerateTempletIniLaser.aspx?serialNo=")]
        Template_OSA,
        [Additional("Atd_GenerateTempletIniManual.aspx?serialNo=")]
        Template_1830,
        [Additional("Atd_GenerateTempletIniEVOA.aspx?serialNo=")]
        Template_EVOA,
        [Additional("Atd_GenerateTempletIniDC.aspx?serialNo=")]
        Template_DC,
    }

    //测试工序枚举
    /*Test → 1：终测
        *      b)	Adjust → 2：调节/焊接
        *      c)	Pretest → 3：预测
        *      d)	Preadjust=4 → 4：预调
        *      e)	Test5 → 5：步骤5
        *      f)	Test6 → 6：步骤6
        *      g)	Test7 → 7：步骤7
        *      h)	Test8 → 8：步骤8
        *      i)	Test9 → 9：步骤9 */
    public enum ProcessEnum
    {
        [Additional("&preadjust=1")]
        Process_PreAdjust=1,
        [Additional("&adjust=1")]
        Process_Adjust,
        [Additional("&pretest=1")]
        Process_Pretest,
        [Additional("")]
        Process_Test,
        [Additional("&test5=1")]
        Process_Test5,
        [Additional("&test6=1")]
        Process_Test6,
        [Additional("&test7=1")]
        Process_Test7,
        [Additional("&test8=1")]
        Process_Test8,
        [Additional("&test9=1")]
        Process_Test9,
    }

    //测试类型枚举，正常、复测、终测
    public enum TestType
    {
        Test_Normal=1,
        [Additional("&type=2")]
        Test_Retest,
        [Additional("&type=3")]
        Test_FinalTest,
    }

    static public class EnumExtend
    {
        static public string GetStrCodeIndex(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            StrCodeIndexAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(StrCodeIndexAttribute), false) as StrCodeIndexAttribute[];
            return attribs.Length > 0 ? attribs[0].StrCodeIndex : null;
        }
        static public string GetStrTestTemplate(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            StrTestTemplateAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(StrTestTemplateAttribute), false) as StrTestTemplateAttribute[];
            return attribs.Length > 0 ? attribs[0].StrTestTemplate : null;
        }
        static public string GetStrSaveName(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            StrSaveNameAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(StrSaveNameAttribute), false) as StrSaveNameAttribute[];
            return attribs.Length > 0 ? attribs[0].StrSaveName : null;
        }

        static public string GetAdditional(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            AdditionalAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(AdditionalAttribute), false) as AdditionalAttribute[];
            return attribs.Length > 0 ? attribs[0].Additional : null;
        }
    }

    
    public class OPTestInfo
    {
        public const double c_DefaultValue = -9999.9999;
        public ParamRuleEnum m_ulParamType { get; set; } //如果为ex自定义，m_TestParam为0
        public ulong m_ulBandType { get; set; }
        public double m_dStartWL { get; set; }
        public double m_dStopWL { get; set; }
        public double m_dStep { get; set; }
        public double m_dITU { get; set; }
        public double m_dTemperature { get; set; }
        public double m_dTmptChangeTimes { get; set; }
        public ParamInfoEnum m_TestParam { get; set; }
        public string m_PortNameForAMTS { get; set; }
        public string m_PortNameForUser { get; set; }   //软件显示给用户的端口名
        public double m_dSettingValue { get; set; }
        public double m_dWLLeft { get; set; }
        public double m_dWLRight { get; set; }
        public double m_dPowerLeft { get; set; }
        public double m_dPowerRight { get; set; }
        public double m_dLowestCriterion { get; set; }
        public double m_dHighestCriterion { get; set; }
        public double m_dFreeLowestCriterion { get; set; }
        public double m_dFreeHighestCriterion { get; set; }
        public double m_dTestedValue { get; set; }  //从无纸化取得之前保存值
        public double m_dCurValue { get; set; }     //当前测试的值
        public bool m_bPass { get; set; }           //是否合格
        public bool m_bTested { get; set; }         //是否测试过
        public long m_lRowIndex { get; set; }     //显示相关index
        //列index和名称如何决定
        //如果自定义，列名就用m_PortNameForUser，其他使用ParamInfoEnum.GetStrTestTemplate
        public string m_ParamColumnName { get; set; }

        public double m_dILRef { get; set; }
        public double m_dRLRef { get; set; }

        public OPTestInfo()
        {
            m_ulParamType = ParamRuleEnum.PARAM_DEFAULT;
            m_ulBandType = 0;
            m_dStartWL = 0.0;
            m_dStopWL = 0.0;
            m_dStep = 0.0;
            m_dITU = 0.0;
            m_dTemperature = 0.0;
            m_dTmptChangeTimes = 0.0;
            m_TestParam = ParamInfoEnum.OP_Default;
            m_PortNameForAMTS = "";
            m_PortNameForUser = "";
            m_dSettingValue = 0.0;
            m_dWLLeft = 0.0;
            m_dWLRight = 0.0;
            m_dPowerLeft = 0.0;
            m_dPowerRight = 0.0;
            m_dLowestCriterion = 0.0;
            m_dHighestCriterion = 0.0;
            m_dFreeLowestCriterion = 0.0;
            m_dFreeHighestCriterion = 0.0;
            m_dTestedValue = 0.0;
            m_dCurValue = 0.0;
            m_bPass = false;
            m_bTested = false;
            m_lRowIndex = -1;
            m_ParamColumnName = "";
            m_dILRef = c_DefaultValue;
            m_dRLRef = c_DefaultValue;
        }

        public double GetDefaultValue()
        {
            return c_DefaultValue;
        }
    }

    public class GlobalSetting
    {
        public int m_iTmptCount { get; set; }
        public double[] m_dTmptArray { get; set; }
        public double[] m_dTmptTimeArray { get; set; }
        public double m_dITU { get; set; }
        public double m_dVstart { get; set; }
        public int m_iSelfLock { get; set; }
        public int m_iSwitchType { get; set; }
        public int m_iOSAStep { get; set; }
        public int m_iFreeCheckType { get; set; }
        public double m_dMEMS_VOA_Volt { get; set; }
        public int m_iMEMS_VOA_Type { get; set; }
        public double m_dMEMS_VOA_Step { get; set; }
        public double m_dMEMS_VOA_Max_Volt { get; set; }
        public double m_dVmin { get; set; }
        public int m_iExactModel { get; set; }
        public double m_dTBL_LVALUE { get; set; }
        public double m_dTBL_LVALUE1 { get; set; }
        public double m_dTBL_HVALUE { get; set; }
        public double m_dTBL_HVALUE1 { get; set; }
        public double m_dTBL_RVALUE { get; set; }
        public double m_dTBL_RVALUE1 { get; set; }
        public int m_iPD_PIN1_Type { get; set; }
        public int m_iPD_PIN2_Type { get; set; }
        public int m_iPD_PIN3_Type { get; set; }
        public int m_iPD_PIN4_Type { get; set; }
        public int m_iPD_PIN5_Type { get; set; }
        public int m_iPD_PIN6_Type { get; set; }
        public int m_iPD_PIN7_Type { get; set; }
        public int m_iPD_PIN8_Type { get; set; }
        public int m_iPD_PIN9_Type { get; set; }
        public int m_iPD_PIN10_Type { get; set; }
        public int m_iPD_PIN11_Type { get; set; }
        public int m_iPD_PIN12_Type { get; set; }
        public int m_iPD_PIN13_Type { get; set; }
        public int m_iPD_PIN14_Type { get; set; }
        public string m_iPD_PINT_GROUP { get; set; }
        public int m_iPD_PIN_COUNT { get; set; }
        public int m_iEF_PEAK { get; set; }

        public GlobalSetting()
        {
            m_iTmptCount = 0;
            m_dTmptArray = new double[4];
            m_dTmptTimeArray = new double[4];
            m_dITU = 0;
            m_dVstart = 0;
            m_iSelfLock = 0;
            m_iSwitchType = 0;
            m_iOSAStep = 0;
            m_iFreeCheckType = 0;
            m_dMEMS_VOA_Volt = 0;
            m_iMEMS_VOA_Type = 0;
            m_dMEMS_VOA_Step = 0;
            m_dMEMS_VOA_Max_Volt = 0;
            m_dVmin = 0;
            m_iExactModel = 0;
            m_dTBL_LVALUE = 0;
            m_dTBL_LVALUE1 = 0;
            m_dTBL_HVALUE = 0;
            m_dTBL_HVALUE1 = 0;
            m_dTBL_RVALUE = 0;
            m_dTBL_RVALUE1 = 0;
            m_iPD_PIN1_Type = 0;
            m_iPD_PIN2_Type = 0;
            m_iPD_PIN3_Type = 0;
            m_iPD_PIN4_Type = 0;
            m_iPD_PIN5_Type = 0;
            m_iPD_PIN6_Type = 0;
            m_iPD_PIN7_Type = 0;
            m_iPD_PIN8_Type = 0;
            m_iPD_PIN9_Type = 0;
            m_iPD_PIN10_Type = 0;
            m_iPD_PIN11_Type = 0;
            m_iPD_PIN12_Type = 0;
            m_iPD_PIN13_Type = 0;
            m_iPD_PIN14_Type = 0;
            m_iPD_PINT_GROUP = "";
            m_iPD_PIN_COUNT = 0;
            m_iEF_PEAK = 0;
        }
    }

    public class ProductInfo
    {

        public string m_SN { get; set; }
        public string m_TemplateID { get; set; }
        public string m_ProductPN { get; set; }
        public string m_Version { get; set; }
        public string m_PC { get; set; }
        public string m_PT { get; set; }
        public string m_SO { get; set; }
        public string m_Spec { get; set; }
        public string m_SpecNO { get; set; }
        public string m_DeviceNO { get; set; }
        public string m_SetupModel { get; set; }
        public string m_FinishModel { get; set; }
        public string m_Precheck { get; set; }
        public string m_GSStatus { get; set; }
        public string m_GSDate { get; set; }
        public string m_Hint { get; set; }
        public string m_TempletHint { get; set; }
        public string m_FreeCheckType { get; set; }
        public string m_ProcessType { get; set; }
        public string m_ProcessTypeUp { get; set; }
        public ProductInfo()
        {
            m_SN = "";
            m_TemplateID = "";
            m_ProductPN = "";
            m_Version = "";
            m_PC = "";
            m_PT = "";
            m_SO = "";
            m_Spec = "";
            m_SpecNO = "";
            m_DeviceNO = "";
            m_SetupModel = "";
            m_FinishModel = "";
            m_Precheck = "";
            m_GSStatus = "";
            m_GSDate = "";
            m_Hint = "";
            m_TempletHint = "";
            m_FreeCheckType = "";
            m_ProcessType = "";
            m_ProcessTypeUp = "";
        }
    }

    public class TemplateData
    {
        public GlobalSetting m_GlobalSetting=new GlobalSetting();
        public List<OPTestInfo> m_AllTestInfo = new List<OPTestInfo>();
        public ProductInfo m_ProductInfo = new ProductInfo();

        public bool OpenTemplate(
            string strSvr,
            TemplateTypeEnum tmplatType,
            string strSN,
            ProcessEnum testProcess,
            TestType testType,
            string strUserID,
            string strGoldSample,
            bool bLoad, //是否加载并解析模板数据
            out string errMsg
            )
        {
            string strAdrress = strSvr + tmplatType.GetAdditional() + strSN + testProcess.GetAdditional() + testType.GetAdditional();
            strAdrress += "&user=" + strUserID;
            strAdrress += "&workstation=" + strGoldSample;
            m_ProductInfo.m_SN = strSN;
            string[] strNodeName = new string[20];
            strNodeName[0] = "TempletID";
            strNodeName[1] = "Version";
            strNodeName[2] = "PN";
            strNodeName[3] = "PC";
            strNodeName[4] = "PT";
            strNodeName[5] = "SpecNo";
            strNodeName[6] = "Spec";
            strNodeName[7] = "SO";
            strNodeName[8] = "PROCESS_TYPE";
            strNodeName[9] = "PROCESS_TYPE_UP";
            strNodeName[10] = "DeviceNo";
            strNodeName[11] = "SetupModel";
            strNodeName[12] = "FinishModel";
            strNodeName[13] = "Precheck";
            strNodeName[14] = "GS_Status";
            strNodeName[15] = "GS_Date";
            strNodeName[16] = "Hint";
            strNodeName[17] = "TempletHint";
            strNodeName[18] = "FreeCheckType";
            strNodeName[19] = "TempleData";
            string[] strNodeContent;
            if (!CommonFunction.GetNodeContentByName(strAdrress, "AutoTemplate", strNodeName, out strNodeContent, out errMsg))
                return false;
            m_ProductInfo.m_TemplateID = strNodeContent[0];
            m_ProductInfo.m_Version = strNodeContent[1];
            m_ProductInfo.m_ProductPN = strNodeContent[2];
            m_ProductInfo.m_PC = strNodeContent[3];
            m_ProductInfo.m_PT = strNodeContent[4];
            m_ProductInfo.m_SpecNO = strNodeContent[5];
            m_ProductInfo.m_Spec = strNodeContent[6];
            m_ProductInfo.m_SO = strNodeContent[7];
            m_ProductInfo.m_ProcessType = strNodeContent[8];
            m_ProductInfo.m_ProcessTypeUp = strNodeContent[9];
            m_ProductInfo.m_DeviceNO = strNodeContent[10];
            m_ProductInfo.m_SetupModel = strNodeContent[11];
            m_ProductInfo.m_FinishModel = strNodeContent[12];
            m_ProductInfo.m_Precheck = strNodeContent[13];
            m_ProductInfo.m_GSStatus = strNodeContent[14];
            m_ProductInfo.m_GSDate = strNodeContent[15];
            m_ProductInfo.m_Hint = strNodeContent[16];
            m_ProductInfo.m_TempletHint = strNodeContent[17];
            m_ProductInfo.m_FreeCheckType = strNodeContent[18];
            if (bLoad)
            {
                string strPath = System.Environment.CurrentDirectory;
                strPath += "\\temple\\tempdata.ini";
               //MoUtilityLib.CommonFunction.Write(strPath, strNodeContent[19]);
                ParserTemplateFile(strPath);
            }
            return true;
        }

        //GFQC模板ini文件结构不同，后续增加区分解析.EX解析未做，后续增加
        public bool ParserTemplateFile(string strFilePath)
        {
            
            IniParser templateParser = new IniParser(strFilePath);
            //globalsetting 用单独函数解析
            ParserGlobalSetting(ref templateParser);

            string strSection = "";
            string strKey = "";
            for (int i = 0; i < m_GlobalSetting.m_iTmptCount; i++)
            {
                string strTmptSection = "Tmpt" + i.ToString();
                strKey = "Channel Config";
                string strAllPort = templateParser.readStringData(strTmptSection, strKey);
                string[] PortArray = strAllPort.Split(';');
                foreach (string strPort in PortArray)
                {
                    string strStartWL = "";
                    string strStopWL = "";
                    string strStepWL = "";
                    string strUserPortName = "";
                    string strITU = "";
                    int nDBCount = 0;
                    int nITUCount = 0;
                    int nWLCount = 0;
                    int nEXCount = 0;
                    string strParserPort=strPort.Replace(":", " -> ");
                    strSection = strTmptSection + " Port " + strParserPort + " Setting";
                    strUserPortName=templateParser.readStringData(strSection, "Port Caption");
                    strStepWL = templateParser.readStringData(strSection, "Step Size","0");
                    strStartWL = templateParser.readStringData(strSection, "Start WL", "0");
                    strStopWL = templateParser.readStringData(strSection, "Stop WL", "0");
                    strITU = templateParser.readStringData(strSection, "ITU", "0");
                    nDBCount = templateParser.readIntData(strSection, "dB Count");
                    nITUCount = templateParser.readIntData(strSection, "ITU Count");
                    nWLCount = templateParser.readIntData(strSection, "WL Count");
                    nEXCount = templateParser.readIntData(strSection, "EX Count");
                    List<OPTestInfo> PortAllInfo=new List<OPTestInfo>();
                    for (int j = 0; j < nDBCount; j++)
                    {
                        List<OPTestInfo> ParamInfo;
                        ParserParam(ref templateParser, strTmptSection, strParserPort, "dB", j, out ParamInfo);
                        foreach (OPTestInfo info in ParamInfo)
                        {
                            info.m_ulParamType = ParamRuleEnum.PARAM_DB;
                            PortAllInfo.Add(info);
                        }
                    }
                    for (int j = 0; j < nITUCount; j++)
                    {
                        List<OPTestInfo> ParamInfo;
                        ParserParam(ref templateParser, strTmptSection, strParserPort, "ITU", j, out ParamInfo);
                        foreach (OPTestInfo info in ParamInfo)
                        {
                            info.m_ulParamType = ParamRuleEnum.PARAM_ITU;
                            PortAllInfo.Add(info);
                        }
                    }
                    for (int j = 0; j < nWLCount; j++)
                    {
                        List<OPTestInfo> ParamInfo;
                        ParserParam(ref templateParser, strTmptSection, strParserPort, "WL", j, out ParamInfo);
                        foreach (OPTestInfo info in ParamInfo)
                        {
                            info.m_ulParamType = ParamRuleEnum.PARAM_WL;
                            PortAllInfo.Add(info);
                        }
                    }
                    //差ex
                    foreach (OPTestInfo info in PortAllInfo)
                    {
                        info.m_PortNameForAMTS = strPort.Replace(":", "->");
                        info.m_PortNameForUser = strUserPortName;
                        info.m_dStartWL = Convert.ToDouble(strStartWL);
                        info.m_dStopWL = Convert.ToDouble(strStopWL);
                        info.m_dStep = Convert.ToDouble(strStepWL);
                        info.m_dITU = Convert.ToDouble(strITU);
                        info.m_dTemperature = m_GlobalSetting.m_dTmptArray[i];
                        info.m_dTmptChangeTimes = m_GlobalSetting.m_dTmptTimeArray[i];
                        m_AllTestInfo.Add(info);
                    }
                }
            }
            return true;
        }

        public GlobalSetting GetGlobalSetting()
        {
            return m_GlobalSetting;
        }

        public OPTestInfo GetTestInfoByIndex(int nIndex)
        {
            if (nIndex < 0||m_AllTestInfo.Count==0)
                return null;
            return m_AllTestInfo[nIndex];
        }

        public bool UpdateTestData(int nIndex, double dRes)
        {
            if (m_AllTestInfo.Count > nIndex)
            {
                m_AllTestInfo[nIndex].m_dCurValue = dRes;
                m_AllTestInfo[nIndex].m_bTested = true;
                return CheckPassOrFail(nIndex);
            }
            return true;
        }

        //是否全部都已经测试过
        public bool GetAllTested(out int nIndex)
        {
            int nUnTestedIdx = -1;
            for (int i = 0; i < m_AllTestInfo.Count; i++)
            {
                if (!m_AllTestInfo[i].m_bTested)
                {
                    nUnTestedIdx = i;
                    nIndex = nUnTestedIdx;
                    return false;
                }
            }
            nIndex = nUnTestedIdx;
            return true;
        }

        //是否已经测试过的都合格
        public bool GetAllTestedPassed()
        {
            foreach (OPTestInfo info in m_AllTestInfo)
            {
                if (info.m_bTested&&(!info.m_bPass))
                    return false;
            }
            return true;
        }

        private bool CheckPassOrFail(int nIndex)
        {
            if (m_AllTestInfo.Count > nIndex)
            {
                double dTestValue = -m_AllTestInfo[nIndex].m_dCurValue;
                bool bPass = MeetCritereon(dTestValue, m_AllTestInfo[nIndex].m_dLowestCriterion) 
                    | MeetCritereon(dTestValue, m_AllTestInfo[nIndex].m_dHighestCriterion);
                m_AllTestInfo[nIndex].m_bPass = bPass;
                return bPass;
            }
            return true;
        }

        //dRes为结果取反，dCritereon如果为正，dRes>=dCritereon,如果为负，dRes<=|dCritereon|
        private bool MeetCritereon(double dRes, double dCritereon)
        {
            bool bOK = false;
            double dblLimit;
            dblLimit = Math.Abs(dCritereon);
            if (dCritereon >= 0)
            {
                bOK = (dRes >= dblLimit);
            }
            else
            {
                bOK = (dRes <= dblLimit);
            }
            return bOK;
        }

        private bool ParserParam(ref IniParser tpParser, string strTmpt, string strPort, string strType, int nIndex, out List<OPTestInfo> ParamInfo)
        {
            List<OPTestInfo> listParamInfo = new List<OPTestInfo>();

            string strSection = "";
            strSection = string.Format("{0} Port {1} {2}{3:00} Settings", strTmpt, strPort, strType, nIndex);
            string strCode = tpParser.readStringData(strSection, "Setting Code EX");
            int nCodeIndex = 0;
            foreach (char ch in strCode)
            {
                nCodeIndex++;
                if (ch == '0')
                    continue;
                OPTestInfo getParamInfo = new OPTestInfo();

                getParamInfo.m_TestParam = (ParamInfoEnum)nCodeIndex;
                getParamInfo.m_ParamColumnName = getParamInfo.m_TestParam.GetStrTestTemplate();
                string strKey = "";
                strKey = getParamInfo.m_TestParam.GetStrTestTemplate();
                getParamInfo.m_dLowestCriterion = Convert.ToDouble(tpParser.readStringData(strSection, strKey, "0"));
                strKey = string.Format("{0}1", getParamInfo.m_TestParam.GetStrTestTemplate());
                getParamInfo.m_dHighestCriterion = Convert.ToDouble(tpParser.readStringData(strSection, strKey, "0"));
                strKey = string.Format("Free {0}", getParamInfo.m_TestParam.GetStrTestTemplate());
                getParamInfo.m_dFreeLowestCriterion = Convert.ToDouble(tpParser.readStringData(strSection, strKey, "0"));
                strKey = string.Format("Free {0}1", getParamInfo.m_TestParam.GetStrTestTemplate());
                getParamInfo.m_dFreeHighestCriterion = Convert.ToDouble(tpParser.readStringData(strSection, strKey, "0"));
                strKey = string.Format("{0} Value", getParamInfo.m_TestParam.GetStrTestTemplate());
                getParamInfo.m_dTestedValue = Convert.ToDouble(tpParser.readStringData(strSection, strKey, "0"));
                getParamInfo.m_ulBandType = Convert.ToUInt64(tpParser.readIntData(strSection, "Band Type", 0));
                getParamInfo.m_dWLLeft = Convert.ToDouble(tpParser.readStringData(strSection, "WL Left", "0"));
                getParamInfo.m_dWLRight = Convert.ToDouble(tpParser.readStringData(strSection, "WL Right", "0"));
                getParamInfo.m_dSettingValue = Convert.ToDouble(tpParser.readStringData(strSection, "Setting Value", "0"));
                listParamInfo.Add(getParamInfo);
            }

            ParamInfo = listParamInfo;
            return true;
        }

        private bool ParserGlobalSetting(ref IniParser tpParser)
        {
            string strSection = "Global Setting";
            string strKey = "";
            m_GlobalSetting.m_iTmptCount = tpParser.readIntData(strSection, "Tmpt Count", 0);
            for (int i = 0; i < m_GlobalSetting.m_iTmptCount; i++)
            {
                strKey = string.Format("Tmpt{0}", i);
                m_GlobalSetting.m_dTmptArray[i] = Convert.ToDouble(tpParser.readStringData(strSection, strKey));
                strKey = string.Format("Time{0}", i);
                m_GlobalSetting.m_dTmptTimeArray[i] = Convert.ToDouble(tpParser.readStringData(strSection, strKey));
            }
            //剩余解析

            return true;
        }
       

        
    }
}
