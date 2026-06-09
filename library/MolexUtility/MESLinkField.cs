using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MolexUtility
{
    /// <summary>
    /// 全局属性，每个测试项的settingcode
    /// </summary>
    public class MESCodeSettingAttribute : Attribute
    {
        private string mesCodeSetting;
        public string MESCodeSetting
        {
            get
            {
                return mesCodeSetting;
            }
        }
        public MESCodeSettingAttribute(string value)
        {
            mesCodeSetting = value;
        }
    }

    /// <summary>
    /// 全局属性，加载模板ini文件里面的对应关键字
    /// </summary>
    public class MESTemplateKeywordsAttribute : Attribute
    {
        private string mesTemplateKeywords;
        public string MESTemplateKeywords
        {
            get
            {
                return mesTemplateKeywords;
            }
        }

        public MESTemplateKeywordsAttribute(string value)
        {
            mesTemplateKeywords = value;
        }
    }

    /// <summary>
    /// 全局属性，保存数据时的关键字
    /// </summary>
    public class MESSaveDataKeywordsAttribute : Attribute
    {
        private string mesSaveDataKeywords;
        public string MESSaveDataKeywords
        {
            get
            {
                return mesSaveDataKeywords;
            }
        }

        public MESSaveDataKeywordsAttribute(string value)
        {
            mesSaveDataKeywords = value;
        }
    }

    /// <summary>
    /// 全局属性
    /// </summary>
    public class AdditionalAttribute : Attribute
    {
        private string additional;
        public string Additional
        {
            get
            {
                return additional;
            }
        }

        public AdditionalAttribute(string value)
        {
            additional = value;
        }
    }

   
    static public class EnumExtend
    {
        static public string GetMESCodeSetting(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            MESCodeSettingAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(MESCodeSettingAttribute), false) as MESCodeSettingAttribute[];
            return attribs.Length > 0 ? attribs[0].MESCodeSetting : null;
        }
        static public string GetMESTemplateKeywords(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            MESTemplateKeywordsAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(MESTemplateKeywordsAttribute), false) as MESTemplateKeywordsAttribute[];
            return attribs.Length > 0 ? attribs[0].MESTemplateKeywords : null;
        }
        static public string GetMESSaveDataKeywords(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            MESSaveDataKeywordsAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(MESSaveDataKeywordsAttribute), false) as MESSaveDataKeywordsAttribute[];
            return attribs.Length > 0 ? attribs[0].MESSaveDataKeywords : null;
        }

        static public string GetAdditional(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            AdditionalAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(AdditionalAttribute), false) as AdditionalAttribute[];
            return attribs.Length > 0 ? attribs[0].Additional : null;
        }
    }

    ///// <summary>
    ///// 无纸化定义的测试项相关信息枚举
    ///// </summary>
    //public enum MESParam
    //{
    //    Default = -1,
    //    DefineEx = 0,
    //    [MESCodeSetting("1")]
    //    [MESTemplateKeywords("Central WL")]
    //    [MESSaveDataKeywords("CWL")]
    //    CentralWL = 1,
    //    [MESCodeSetting("2")]
    //    [MESTemplateKeywords("Shift")]
    //    [MESSaveDataKeywords("SHIFT")]
    //    Shift,
    //    [MESCodeSetting("3")]
    //    [MESTemplateKeywords("Peak IL")]
    //    [MESSaveDataKeywords("PEAKIL")]
    //    PeakIL,
    //    [MESCodeSetting("4")]
    //    [MESTemplateKeywords("Ripple")]
    //    [MESSaveDataKeywords("RIPPLE")]
    //    Ripple,
    //    [MESCodeSetting("5")]
    //    [MESTemplateKeywords("Bandwidth")]
    //    [MESSaveDataKeywords("BW")]
    //    Bandwidth,
    //    [MESCodeSetting("6")]
    //    [MESTemplateKeywords("WL Left")]
    //    [MESSaveDataKeywords("WLL")]
    //    WLLeft,
    //    [MESCodeSetting("7")]
    //    [MESTemplateKeywords("WL Right")]
    //    [MESSaveDataKeywords("WLR")]
    //    WLRight,
    //    [MESCodeSetting("8")]
    //    [MESTemplateKeywords("Power Left")]
    //    [MESSaveDataKeywords("PWL")]
    //    PowerLeft,
    //    [MESCodeSetting("9")]
    //    [MESTemplateKeywords("Power Right")]
    //    [MESSaveDataKeywords("PWR")]
    //    PowerRight,
    //    [MESCodeSetting("10")]
    //    [MESTemplateKeywords("PDL")]
    //    [MESSaveDataKeywords("PDL")]
    //    PDL,
    //    [MESCodeSetting("11")]
    //    [MESTemplateKeywords("Max IL")]
    //    [MESSaveDataKeywords("MAXIL")]
    //    MaxIL,
    //    [MESCodeSetting("12")]
    //    [MESTemplateKeywords("WDL")]
    //    [MESSaveDataKeywords("WDL")]
    //    WDL,
    //    [MESCodeSetting("13")]
    //    [MESTemplateKeywords("TDL")]
    //    [MESSaveDataKeywords("TDL")]
    //    TDL,
    //    [MESCodeSetting("14")]
    //    [MESTemplateKeywords("Return Loss")]
    //    [MESSaveDataKeywords("RL")]
    //    ReturnLoss,

    //    [MESCodeSetting("15")]
    //    [MESTemplateKeywords("Directivity")]
    //    [MESSaveDataKeywords("DIR")]
    //    Directivity,

    //    [MESCodeSetting("16")]
    //    [MESTemplateKeywords("AEL")]
    //    [MESSaveDataKeywords("AEL")]
    //    AEL,

    //    [MESCodeSetting("17")]
    //    [MESTemplateKeywords("Slope")]
    //    [MESSaveDataKeywords("SLOPE")]
    //    Slope,

    //    [MESCodeSetting("18")]
    //    [MESTemplateKeywords("NWDL")]
    //    [MESSaveDataKeywords("NWDL")]
    //    NWDL,

    //    [MESCodeSetting("19")]
    //    [MESTemplateKeywords("AT-RES")]
    //    [MESSaveDataKeywords("RES")]
    //    ATRES,

    //    [MESCodeSetting("20")]
    //    [MESTemplateKeywords("AT-Range")]
    //    [MESSaveDataKeywords("RANGE")]
    //    ATRange,

    //    [MESCodeSetting("21")]
    //    [MESTemplateKeywords("Backlash")]
    //    [MESSaveDataKeywords("BACKLASH")]
    //    Backlash,

    //    [MESCodeSetting("22")]
    //    [MESTemplateKeywords("Repeatability")]
    //    [MESSaveDataKeywords("ERP")]
    //    Repeatability,

    //    [MESCodeSetting("23")]
    //    [MESTemplateKeywords("Darkness")]
    //    [MESSaveDataKeywords("DARK")]
    //    Darkness,

    //    [MESCodeSetting("24")]
    //    [MESTemplateKeywords("AD-V")]
    //    [MESSaveDataKeywords("ADV")]
    //    ADV,

    //    [MESCodeSetting("25")]
    //    [MESTemplateKeywords("AD-L")]
    //    [MESSaveDataKeywords("ADL")]
    //    ADL,

    //    [MESCodeSetting("26")]
    //    [MESTemplateKeywords("AD-P25")]
    //    [MESSaveDataKeywords("ADP25")]
    //    ADP25,

    //    [MESCodeSetting("27")]
    //    [MESTemplateKeywords("AD-P75")]
    //    [MESSaveDataKeywords("ADP75")]
    //    ADP75,

    //    [MESCodeSetting("28")]
    //    [MESTemplateKeywords("Vmin")]
    //    [MESSaveDataKeywords("VMIN")]
    //    Vmin,

    //    [MESCodeSetting("29")]
    //    [MESTemplateKeywords("Vmax")]
    //    [MESSaveDataKeywords("VMAX")]
    //    Vmax,

    //    [MESCodeSetting("30")]
    //    [MESTemplateKeywords("MSD")]
    //    [MESSaveDataKeywords("MSD")]
    //    MSD,

    //    [MESCodeSetting("31")]
    //    [MESTemplateKeywords("C-Max IL")]
    //    [MESSaveDataKeywords("CMAXIL")]
    //    CMaxIL,

    //    [MESCodeSetting("32")]
    //    [MESTemplateKeywords("C-WDL")]
    //    [MESSaveDataKeywords("CWDL")]
    //    CWDL,

    //    [MESCodeSetting("33")]
    //    [MESTemplateKeywords("C-PDL")]
    //    [MESSaveDataKeywords("CPDL")]
    //    CPDL,

    //    [MESCodeSetting("34")]
    //    [MESTemplateKeywords("C-TDL")]
    //    [MESSaveDataKeywords("CTDL")]
    //    CTDL,
    //    [MESCodeSetting("35")]
    //    [MESTemplateKeywords("C-RL")]
    //    [MESSaveDataKeywords("CRL")]
    //    CRL,
    //    [MESCodeSetting("36")]
    //    [MESTemplateKeywords("C-CTRep")]
    //    [MESSaveDataKeywords("CCTREP")]
    //    CCTRep,
    //    [MESCodeSetting("37")]
    //    [MESTemplateKeywords("C-Repeatability")]
    //    [MESSaveDataKeywords("CREP")]
    //    CRepeatability,
    //    [MESCodeSetting("38")]
    //    [MESTemplateKeywords("CT")]
    //    [MESSaveDataKeywords("CT")]
    //    CT,
    //    [MESCodeSetting("39")]
    //    [MESTemplateKeywords("C-CT")]
    //    [MESSaveDataKeywords("CCT")]
    //    CCT,
    //    [MESCodeSetting("40")]
    //    [MESTemplateKeywords("Darkness-V")]
    //    [MESSaveDataKeywords("DARKV")]
    //    DarknessV,
    //    [MESCodeSetting("41")]
    //    [MESTemplateKeywords("Leak")]
    //    [MESSaveDataKeywords("LEAK")]
    //    Leak,
    //    [MESCodeSetting("42")]
    //    [MESTemplateKeywords("Bump")]
    //    [MESSaveDataKeywords("BUMP")]
    //    Bump,
    //    [MESCodeSetting("43")]
    //    [MESTemplateKeywords("Flop")]
    //    [MESSaveDataKeywords("FLOP")]
    //    Flop,
    //    [MESCodeSetting("44")]
    //    [MESTemplateKeywords("Set")]
    //    [MESSaveDataKeywords("SET")]
    //    Set,
    //    [MESCodeSetting("45")]
    //    [MESTemplateKeywords("PDR")]
    //    [MESSaveDataKeywords("PDR")]
    //    PDR,
    //    [MESCodeSetting("46")]
    //    [MESTemplateKeywords("WDR")]
    //    [MESSaveDataKeywords("WDR")]
    //    WDR,
    //    [MESCodeSetting("47")]
    //    [MESTemplateKeywords("TDR")]
    //    [MESSaveDataKeywords("TDR")]
    //    TDR,
    //    [MESCodeSetting("48")]
    //    [MESTemplateKeywords("Linearity")]
    //    [MESSaveDataKeywords("LINE")]
    //    Linearity,
    //    [MESCodeSetting("49")]
    //    [MESTemplateKeywords("RES-IN")]
    //    [MESSaveDataKeywords("RESIN")]
    //    RESIN,
    //    [MESCodeSetting("50")]
    //    [MESTemplateKeywords("RES-OUT")]
    //    [MESSaveDataKeywords("RESOUT")]
    //    RESOUT,
    //    [MESCodeSetting("51")]
    //    [MESTemplateKeywords("DK")]
    //    [MESSaveDataKeywords("DK")]
    //    DK,
    //    [MESCodeSetting("52")]
    //    [MESTemplateKeywords("Step")]
    //    [MESSaveDataKeywords("STEP")]
    //    Step,
    //    [MESCodeSetting("53")]
    //    [MESTemplateKeywords("TDR-L")]
    //    [MESSaveDataKeywords("TDRL")]
    //    TDRL,
    //    [MESCodeSetting("54")]
    //    [MESTemplateKeywords("TDR-H")]
    //    [MESSaveDataKeywords("TDRH")]
    //    TDRH,
    //    [MESCodeSetting("55")]
    //    [MESTemplateKeywords("PD-ISO")]
    //    [MESSaveDataKeywords("PDISO")]
    //    PDISO,
    //    [MESCodeSetting("56")]
    //    [MESTemplateKeywords("WDL1")]
    //    [MESSaveDataKeywords("WDL1")]
    //    WDL1,
    //    [MESCodeSetting("57")]
    //    [MESTemplateKeywords("TDR-M")]
    //    [MESSaveDataKeywords("TDRM")]
    //    TDRM,
    //    [MESCodeSetting("58")]
    //    [MESTemplateKeywords("WDR-M")]
    //    [MESSaveDataKeywords("WDRM")]
    //    WDRM,
    //    [MESCodeSetting("59")]//OK
    //    [MESTemplateKeywords("Uniformity")]
    //    [MESSaveDataKeywords("UNI")]
    //    Uniformity,
    //    [MESCodeSetting("60")]
    //    [MESTemplateKeywords("Adj")]
    //    [MESSaveDataKeywords("ADJ")]
    //    Adj,
    //    [MESCodeSetting("61")]
    //    [MESTemplateKeywords("NonAdj")]
    //    [MESSaveDataKeywords("NONADJ")]
    //    NonAdj,
    //    [MESCodeSetting("62")]
    //    [MESTemplateKeywords("CTRep")]
    //    [MESSaveDataKeywords("CTREP")]
    //    CTRep,
    //    [MESCodeSetting("63")]
    //    [MESTemplateKeywords("BFL")]
    //    [MESSaveDataKeywords("BFL")]
    //    BFL,
    //    [MESCodeSetting("64")]
    //    [MESTemplateKeywords("EL")]
    //    [MESSaveDataKeywords("EL")]
    //    EL,
    //    [MESCodeSetting("65")]
    //    [MESTemplateKeywords("CR")]
    //    [MESSaveDataKeywords("CR")]
    //    CR,
    //    [MESCodeSetting("66")]
    //    [MESTemplateKeywords("ΔIL")]
    //    [MESSaveDataKeywords("ΔIL")]
    //    DeltaIL,
    //    [MESCodeSetting("67")]
    //    [MESTemplateKeywords("SLOPE1")]
    //    [MESSaveDataKeywords("SLOPE1")]
    //    SLOPE1,
    //    [MESCodeSetting("68")]
    //    [MESTemplateKeywords("Min IL")]
    //    [MESSaveDataKeywords("MINIL")]
    //    MinIL,
    //    [MESCodeSetting("69")]
    //    [MESTemplateKeywords("RLX")]
    //    [MESSaveDataKeywords("RLX")]
    //    RLX,
    //    [MESCodeSetting("70")]
    //    [MESTemplateKeywords("ER")]
    //    [MESSaveDataKeywords("ER")]
    //    ER,
    //    [MESCodeSetting("71")]
    //    [MESTemplateKeywords("WIL")]
    //    [MESSaveDataKeywords("WIL")]
    //    WIL,
    //    [MESCodeSetting("72")]
    //    [MESTemplateKeywords("EF")]
    //    [MESSaveDataKeywords("EF")]
    //    EF,
    //    [MESCodeSetting("73")]
    //    [MESTemplateKeywords("Reflection ISO")]
    //    [MESSaveDataKeywords("RISO")]
    //    ReflectionISO,
    //    [MESCodeSetting("74")]
    //    [MESTemplateKeywords("Slope-Max")]
    //    [MESSaveDataKeywords("SLOPEMAX")]
    //    SlopeMax,
    //    [MESCodeSetting("75")]
    //    [MESTemplateKeywords("Slope-MIN")]
    //    [MESSaveDataKeywords("SLOPEMIN")]
    //    SlopeMIN,
    //    [MESCodeSetting("76")]
    //    [MESTemplateKeywords("Res-R")]
    //    [MESSaveDataKeywords("RESR")]
    //    ResR,
    //    [MESCodeSetting("77")]
    //    [MESTemplateKeywords("Min-RES")]
    //    [MESSaveDataKeywords("MINRES")]
    //    MinRES,
    //    [MESCodeSetting("78")]
    //    [MESTemplateKeywords("Max-RES")]
    //    [MESSaveDataKeywords("MAXRES")]
    //    MaxRES,
    //    [MESCodeSetting("79")]
    //    [MESTemplateKeywords("Locking-WL")]
    //    [MESSaveDataKeywords("LOCKINGWL")]
    //    LockingWL,
    //    [MESCodeSetting("80")]
    //    [MESTemplateKeywords("CuptureRange-L")]
    //    [MESSaveDataKeywords("CUPTURERANGEL")]
    //    CuptureRangeL,
    //    [MESCodeSetting("81")]
    //    [MESTemplateKeywords("CuptureRange-R")]
    //    [MESSaveDataKeywords("CUPTURERANGER")]
    //    CuptureRangeR,
    //    [MESCodeSetting("82")]
    //    [MESTemplateKeywords("Locking-Acry")]
    //    [MESSaveDataKeywords("LOCKINGACRY")]
    //    LockingAcry,
    //    [MESCodeSetting("83")]
    //    [MESTemplateKeywords("Locking-Slope")]
    //    [MESSaveDataKeywords("LOCKINGSLOPE")]
    //    LockingSlope,
    //    [MESCodeSetting("84")]
    //    [MESTemplateKeywords("Contrast")]
    //    [MESSaveDataKeywords("CONTRAST")]
    //    Contrast,
    //    [MESCodeSetting("85")]
    //    [MESTemplateKeywords("PDA")]
    //    [MESSaveDataKeywords("PDA")]
    //    PDA,
    //    [MESCodeSetting("86")]
    //    [MESTemplateKeywords("Distance")]
    //    [MESSaveDataKeywords("DIS")]
    //    Distance,
    //    [MESCodeSetting("87")]
    //    [MESTemplateKeywords("PD1 DIF")]
    //    [MESSaveDataKeywords("PD1DIF")]
    //    PD1DIF,
    //    [MESCodeSetting("88")]
    //    [MESTemplateKeywords("EF-Pmax")]
    //    [MESSaveDataKeywords("EFPMAX")]
    //    EFPmax,
    //    [MESCodeSetting("89")]
    //    [MESTemplateKeywords("Res ratio")]
    //    [MESSaveDataKeywords("RESRATIO")]
    //    Resratio,
    //    [MESCodeSetting("90")]
    //    [MESTemplateKeywords("EF-Pmin")]
    //    [MESSaveDataKeywords("EFPMIN")]
    //    EFPmin,
    //    [MESCodeSetting("91")]
    //    [MESTemplateKeywords("Axis")]
    //    [MESSaveDataKeywords("AXIS")]
    //    Axis,
    //    [MESCodeSetting("92")]
    //    [MESTemplateKeywords("IL-Change")]
    //    [MESSaveDataKeywords("ILCHANGE")]
    //    ILChange,
    //    [MESCodeSetting("93")]
    //    [MESTemplateKeywords("Dop")]
    //    [MESSaveDataKeywords("DOP")]
    //    Dop,
    //    [MESCodeSetting("94")]
    //    [MESTemplateKeywords("PMDN")]
    //    [MESSaveDataKeywords("PMDN")]
    //    PMDN,
    //    [MESCodeSetting("95")]
    //    [MESTemplateKeywords("WDL2")]
    //    [MESSaveDataKeywords("WDL2")]
    //    WDL2,
    //    [MESCodeSetting("96")]
    //    [MESTemplateKeywords("MIL")]
    //    [MESSaveDataKeywords("MIL")]
    //    MIL,
    //    [MESCodeSetting("97")]
    //    [MESTemplateKeywords("C-Dir")]
    //    [MESSaveDataKeywords("CDIR")]
    //    CDir,
    //    [MESCodeSetting("98")]
    //    [MESTemplateKeywords("CHK")]
    //    [MESSaveDataKeywords("CHK")]
    //    CHK,
    //    [MESCodeSetting("99")]
    //    [MESTemplateKeywords("CIE-OUTRES")]
    //    [MESSaveDataKeywords("CIEOUTRES")]
    //    CIEOUTRES
    //}


    /// <summary>
    /// 无纸化定义的测试项相关信息枚举
    /// </summary>
    public enum MESParam
    {
        Default = -1,
        [MESTemplateKeywords("Accuracy")]
        Accuracy = 1,
        [MESTemplateKeywords("Adj")]
        Adj,
        [MESTemplateKeywords("AEL")]
        AEL,
        [MESTemplateKeywords("Angle_X")]
        Angle_X,
        [MESTemplateKeywords("Angle_Y")]
        Angle_Y,
        [MESTemplateKeywords("AttenIL")]
        AttenIL,
        [MESTemplateKeywords("AttenShift")]
        AttenShift,
        [MESTemplateKeywords("AttenSLP")]
        AttenSLP,
        [MESTemplateKeywords("AttenVolt")]
        AttenVolt,
        [MESTemplateKeywords("Average_EF")]
        Average_EF,
        [MESTemplateKeywords("Axis")]
        Axis,
        [MESTemplateKeywords("A_SWTimeDown")]
        A_SWTimeDown,
        [MESTemplateKeywords("A_SWTimeUp")]
        A_SWTimeUp,
        [MESTemplateKeywords("BasicInfoDL")]
        BasicInfoDL,
        [MESTemplateKeywords("Bearing")]
        Bearing,
        [MESTemplateKeywords("BFL")]
        BFL,
        [MESTemplateKeywords("BL")]
        BL,
        [MESTemplateKeywords("BlockAtten")]
        BlockAtten,
        [MESTemplateKeywords("BrightRipple")]
        BrightRipple,
        [MESTemplateKeywords("Bulge")]
        Bulge,
        [MESTemplateKeywords("BW")]
        BW,
        [MESTemplateKeywords("BW_15dB")]
        BW_15dB,
        [MESTemplateKeywords("BW_3dB")]
        BW_3dB,
        [MESTemplateKeywords("B_SWTimeDown")]
        B_SWTimeDown,
        [MESTemplateKeywords("B_SWTimeUp")]
        B_SWTimeUp,
        [MESTemplateKeywords("CalibWL")]
        CalibWL,
         [MESTemplateKeywords("Cavity_Len")]
        Cavity_Len,
        [MESTemplateKeywords("Cavity_Len_BP")]
        Cavity_Len_BP,
        [MESTemplateKeywords("CHSelect")]
        CHSelect,
        [MESTemplateKeywords("CJ")]
        CJ1,
        [MESTemplateKeywords("Concentricity")]
        Concentricity,
        [MESTemplateKeywords("CT")]
        CT,
        [MESTemplateKeywords("CT_BP")]
        CT_BP,
        [MESTemplateKeywords("Current")]
        Current,
        [MESTemplateKeywords("CWL")]
        CWL,
        [MESTemplateKeywords("DarkIL")]
        DarkIL,
        [MESTemplateKeywords("DarkReportPower")]
        DarkReportPower,
        [MESTemplateKeywords("DeltaEF")]
        DeltaEF,
        [MESTemplateKeywords("DIR")]
        DIR,
        [MESTemplateKeywords("DIR_COM")]
        DIR_COM,
        [MESTemplateKeywords("DIR_RESULT")]
        DIR_RESULT,
        [MESTemplateKeywords("Distance")]
        Distance,
        [MESTemplateKeywords("DK")]
        DK,
        [MESTemplateKeywords("DL")]
        DL,
        [MESTemplateKeywords("DOP")]
        DOP,
        [MESTemplateKeywords("DriveTest")]
        DriveTest,
        [MESTemplateKeywords("EF")]
        EF,
        [MESTemplateKeywords("EL")]
        EL,
        [MESTemplateKeywords("ER")]
        ER,
        [MESTemplateKeywords("FQC")]
        FQC,
        [MESTemplateKeywords("FSR")]
        FSR,
        [MESTemplateKeywords("Gap")]
        Gap,
        [MESTemplateKeywords("GDR")]
        GDR,
        [MESTemplateKeywords("Hitless")]
        Hitless,
        [MESTemplateKeywords("HBW_Max")]
        HBW_Max,
        [MESTemplateKeywords("HBW_Min")]
        HBW_Min,
        [MESTemplateKeywords("HBW_L")]
        HBW_L,
        [MESTemplateKeywords("HBW_R")]
        HBW_R,
        [MESTemplateKeywords("IL")]
        IL,
        [MESTemplateKeywords("IL_BP")]
        IL_BP,
        [MESTemplateKeywords("IL_PRE")]
        IL_PRE,
        [MESTemplateKeywords("InnerDiameter")]
        InnerDiameter,
        [MESTemplateKeywords("ISO")]
        ISO,
        [MESTemplateKeywords("ISO_BP")]
        ISO_BP,
        [MESTemplateKeywords("LC")]
        LC,
        [MESTemplateKeywords("Linearity")]
        Linearity,
        [MESTemplateKeywords("LinearityAB")]
        LinearityAB,
        [MESTemplateKeywords("LinearityB")]
        LinearityB,
        [MESTemplateKeywords("LOSRaisePower")]
        LOSRaisePower,
        [MESTemplateKeywords("LOSReleasePower")]
        LOSReleasePower,
        [MESTemplateKeywords("MaxAtten")]
        MaxAtten,
        [MESTemplateKeywords("MaxAttenDAC")]
        MaxAttenDAC,
        [MESTemplateKeywords("MaxAttenVolt")]
        MaxAttenVolt,
        [MESTemplateKeywords("MaxIL")]
        MaxIL,
        [MESTemplateKeywords("MaxRES")]
        MaxRES,
        [MESTemplateKeywords("MaxSlope")]
        MaxSlope,
        [MESTemplateKeywords("MaxShift")]
        MaxShift,
        [MESTemplateKeywords("MinShift")]
        MinShift,
        [MESTemplateKeywords("MaxISO")]
        MaxISO,
        [MESTemplateKeywords("MaxBW")]
        MaxBW,
        [MESTemplateKeywords("MinISO")]
        MinISO,
        [MESTemplateKeywords("MinAttenDAC")]
        MinAttenDAC,
        [MESTemplateKeywords("MinAttenVolt")]
        MinAttenVolt,
        [MESTemplateKeywords("MinCT")]
        MinCT,
        [MESTemplateKeywords("MinDIR")]
        MinDIR,
        [MESTemplateKeywords("MinIL")]
        MinIL,
        [MESTemplateKeywords("MinPeakIL")]
        MinPeakIL,
        [MESTemplateKeywords("MinRES")]
        MinRES,
        [MESTemplateKeywords("MinSlope")]
        MinSlope,
        [MESTemplateKeywords("MPI")]
        MPI,
        [MESTemplateKeywords("MSD")]
        MSD,
        [MESTemplateKeywords("MT")]
        MT,
        [MESTemplateKeywords("NET_HalfBW")]
        NET_HalfBW,
        [MESTemplateKeywords("NonAdj")]
        NonAdj,
        [MESTemplateKeywords("Non_Interval")]
        Non_Interval,
        [MESTemplateKeywords("NWDL")]
        NWDL,
        [MESTemplateKeywords("PDL")]
        PDL,
        [MESTemplateKeywords("PDR")]
        PDR,
        [MESTemplateKeywords("PD_ISO")]
        PD_ISO,
        [MESTemplateKeywords("PeakIL")]
        PeakIL,
        [MESTemplateKeywords("PMD")]
        PMD,
        [MESTemplateKeywords("PortVsTPMap")]
        PortVsTPMap,
        [MESTemplateKeywords("Pos_Shift")]
        Pos_Shift,
        [MESTemplateKeywords("PowerOffIL")]
        PowerOffIL,
        [MESTemplateKeywords("PPEF")]
        PPEF,
        [MESTemplateKeywords("PW_L")]
        PW_L,
        [MESTemplateKeywords("PW_R")]
        PW_R,
        [MESTemplateKeywords("RangeScanFile")]
        RangeScanFile,
        [MESTemplateKeywords("Reliability")]
        Reliability,
        [MESTemplateKeywords("Repeatability")]
        Repeatability,
        [MESTemplateKeywords("RES")]
        RES,
        [MESTemplateKeywords("Resistor")]
        Resistor,
        [MESTemplateKeywords("ResistorA")]
        ResistorA,
        [MESTemplateKeywords("ResistorB")]
        ResistorB,
        [MESTemplateKeywords("ResistorC")]
        ResistorC,
        [MESTemplateKeywords("ResistorD")]
        ResistorD,
        [MESTemplateKeywords("RESTime")]
        RESTime,
        [MESTemplateKeywords("RES_BP")]
        RES_BP,
        [MESTemplateKeywords("RES_CIE")]
        RES_CIE,
        [MESTemplateKeywords("RES_DK")]
        RES_DK,
        [MESTemplateKeywords("RES_RP")]
        RES_RP,
        [MESTemplateKeywords("Ripple")]
        Ripple,
        [MESTemplateKeywords("RL")]
        RL,
        [MESTemplateKeywords("RLX")]
        RLX,
        [MESTemplateKeywords("RL_BP")]
        RL_BP,
        [MESTemplateKeywords("Set")]
        Set,
        [MESTemplateKeywords("Shift")]
        Shift,
        [MESTemplateKeywords("Shock")]
        Shock,
        [MESTemplateKeywords("ShortCircuit")]
        ShortCircuit,
        [MESTemplateKeywords("ShortCircuitA")]
        ShortCircuitA,
        [MESTemplateKeywords("ShortCircuitB")]
        ShortCircuitB,
        [MESTemplateKeywords("ShortCircuitC")]
        ShortCircuitC,
        [MESTemplateKeywords("ShortCircuitD")]
        ShortCircuitD,
        [MESTemplateKeywords("Sidelobe")]
        Sidelobe,
        [MESTemplateKeywords("Slope")]
        Slope,
        [MESTemplateKeywords("Slope_HR")]
        Slope_HR,
        [MESTemplateKeywords("Slope_LR")]
        Slope_LR,
        [MESTemplateKeywords("Stability")]
        Stability,
        [MESTemplateKeywords("SwitchingCycle")]
        SwitchingCycle,
        [MESTemplateKeywords("SWTime")]
        SWTime,
        [MESTemplateKeywords("StopBand")]
        StopBand,
        [MESTemplateKeywords("TDL")]
        TDL,
        [MESTemplateKeywords("TDL_HR")]
        TDL_HR,
        [MESTemplateKeywords("TDL_LH")]
        TDL_LH,
        [MESTemplateKeywords("TDL_LR")]
        TDL_LR,
        [MESTemplateKeywords("TDL_XR")]
        TDL_XR,
        [MESTemplateKeywords("TDR")]
        TDR,
        [MESTemplateKeywords("TDR_HR")]
        TDR_HR,
        [MESTemplateKeywords("TDR_LR")]
        TDR_LR,
        [MESTemplateKeywords("TEC_Stability")]
        TEC_Stability,
        [MESTemplateKeywords("TEC_Temp")]
        TEC_Temp,
        [MESTemplateKeywords("TP_DAC_X")]
        TP_DAC_X,
        [MESTemplateKeywords("TP_DAC_Y")]
        TP_DAC_Y,
        [MESTemplateKeywords("UNI")]
        UNI,
        [MESTemplateKeywords("UNIPDL")]
        UNIPDL,
        [MESTemplateKeywords("VOAType")]
        VOAType,
        [MESTemplateKeywords("VOLTAGE_X_RE")]
        VOLTAGE_X_RE,
        [MESTemplateKeywords("VOLTAGE_Y_RE")]
        VOLTAGE_Y_RE,
        [MESTemplateKeywords("VoltRng")]
        VoltRng,
        [MESTemplateKeywords("Volt_X")]
        Volt_X,
        [MESTemplateKeywords("Volt_X_BP")]
        Volt_X_BP,
        [MESTemplateKeywords("Volt_Y")]
        Volt_Y,
        [MESTemplateKeywords("Volt_Y_BP")]
        Volt_Y_BP,
        [MESTemplateKeywords("WDL")]
        WDL,
        [MESTemplateKeywords("WDL_HR")]
        WDL_HR,
        [MESTemplateKeywords("WDL_LR")]
        WDL_LR,
        [MESTemplateKeywords("WDL_SLP")]
        WDL_SLP,
        [MESTemplateKeywords("WDR")]
        WDR,
        [MESTemplateKeywords("WIL")]
        WIL,
        [MESTemplateKeywords("WL_L")]
        WL_L,
        [MESTemplateKeywords("WL_R")]
        WL_R,
        [MESTemplateKeywords("MaxAdj_Iso")]
        MaxAdj_Iso,
        [MESTemplateKeywords("MinAdj_Iso")]
        MinAdj_Iso,
        [MESTemplateKeywords("Adj_Iso")]
        Adj_Iso,
        [MESTemplateKeywords("MaxAdj_Shift")]
        MaxAdj_Shift,
        [MESTemplateKeywords("MinAdj_Shift")]
        MinAdj_Shift,
        [MESTemplateKeywords("Adj_Shift")]
        Adj_Shift,
        [MESTemplateKeywords("CD")]
        CD,
        [MESTemplateKeywords("Shift_BP")]
        Shift_BP,
        [MESTemplateKeywords("RL")]
        ReturnLoss,
        [MESTemplateKeywords("RES-IN")]
        RESIN,
        [MESTemplateKeywords("RES-OUT")]
        RESOUT,
        [MESTemplateKeywords("TDR-L")]
        TDRL,
        [MESTemplateKeywords("TDR-H")]
        TDRH,
        [MESTemplateKeywords("TDR-M")]
        TDRM,
        [MESTemplateKeywords("WDR-M")]
        WDRM,
        [MESTemplateKeywords("PD-ISO")]
        PDISO
    }

    /// <summary>
    /// 无纸化测试项规则枚举
    /// </summary>  
    public enum MESParamRule
    {
        [Additional("")]
        Default = 0,
        [Additional("dB")]
        DB = 1,
        [Additional("ITU")]
        ITU,
        [Additional("EX")]
        EX,
        [Additional("WL")]
        WL
    }

    /// <summary>
    /// 模板类型枚举
    /// </summary>
    public enum MESTemplateType
    {
        [MESTemplateKeywords("Atd_GenerateTempletIni.aspx?serialNo=")]
        [MESSaveDataKeywords("GFQC")]
        GFQC = 1,
        [MESTemplateKeywords("Atd_GenerateTempletIniLaser.aspx?serialNo=")]
        [MESSaveDataKeywords("OSA")]
        OSA,
        [MESTemplateKeywords("Atd_GenerateTempletIniManual.aspx?serialNo=")]
        [MESSaveDataKeywords("1830")]
        Manual1830,
        [MESTemplateKeywords("Atd_GenerateTempletIniEVOA.aspx?serialNo=")]
        [MESSaveDataKeywords("EVOA")]
        EVOA,
        [MESTemplateKeywords("Atd_GenerateTempletIniDC.aspx?serialNo=")]
        [MESSaveDataKeywords("DC")]
        DC,
        [MESTemplateKeywords("Atd_GenerateTempletIntEXFO.aspx?serialNo=")]
        [MESSaveDataKeywords("EXFO")]
        EXFO,
        [MESTemplateKeywords("Atd_GenerateTempletIntDevice.aspx?serialNo=")]
        [MESSaveDataKeywords("DEVICE")]
        DEVICE
    }

    /// <summary>
    /// 测试工序枚举
    /// </summary>
    //测试工序枚举
    public enum MESTestProcess
    {
        [Additional("preadjust")]
        [MESTemplateKeywords("&preadjust=1")]
        [MESSaveDataKeywords("-F")]
        PreAdjust = 1,
        [Additional("adjust")]
        [MESTemplateKeywords("&adjust=1")]
        [MESSaveDataKeywords("-A")]
        Adjust,
        [Additional("pretest")]
        [MESTemplateKeywords("&pretest=1")]
        [MESSaveDataKeywords("-P")]
        Pretest,
        [Additional("test")]
        [MESTemplateKeywords("")]
        [MESSaveDataKeywords("")]
        Test,
        [Additional("test5")]
        [MESTemplateKeywords("&test5=1")]
        [MESSaveDataKeywords("-5")]
        Test5,
        [Additional("test6")]
        [MESTemplateKeywords("&test6=1")]
        [MESSaveDataKeywords("-6")]
        Test6,
        [Additional("test7")]
        [MESTemplateKeywords("&test7=1")]
        [MESSaveDataKeywords("-7")]
        Test7,
        [Additional("test8")]
        [MESTemplateKeywords("&test8=1")]
        [MESSaveDataKeywords("-8")]
        Test8,
        [Additional("test9")]
        [MESTemplateKeywords("&test9=1")]
        [MESSaveDataKeywords("-9")]
        Test9,
    }

    /// <summary>
    /// 测试类型枚举，正常、复测、终测
    /// </summary>
    public enum MESTestType
    {
        [MESTemplateKeywords("")]
        [MESSaveDataKeywords("1")]
        Normal = 1,
        [MESTemplateKeywords("&type=2")]
        [MESSaveDataKeywords("2")]
        Retest,
        [MESTemplateKeywords("&type=3")]
        [MESSaveDataKeywords("3")]
        FinalTest,
    }

    /// <summary>
    /// 保存数据时，rawdata数据类型
    /// </summary>
    public enum MESRawdataType
    {
        [MESSaveDataKeywords("")]
        Default=0,
        [MESSaveDataKeywords("SC")]
        SC = 1,
        [MESSaveDataKeywords("SC_RELATIVE")]
        SCRelative ,
        [MESSaveDataKeywords("EXFO")]
        Exfo,
        [MESSaveDataKeywords("EXFO_RELATIVE")]
        ExfoRelative,
        [MESSaveDataKeywords("MEMSVOA")]
        MemsVOA,
        [MESSaveDataKeywords("MEMSVOA_RELATIVE")]
        MemsVOARelative,
        [MESSaveDataKeywords("EF")]
        EF
    }
}
