using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Device
{
    public enum Devices
    {
        [AdditionalAttribute("")]
        Default =-1,
        [AdditionalAttribute("1830")]
        Pwm1830=0,
        [AdditionalAttribute("JH")]
        PwmJH,
        [AdditionalAttribute("Oplink1830")]
        PwmOplink1830,
        [AdditionalAttribute("8163A")]
        Pwm8163A,
        [AdditionalAttribute("8164")]
        Opitical8164,
        [AdditionalAttribute("SourceBank")]
        OpiticalSourceBank,
        [AdditionalAttribute("Interleaver")]
        Interleaver,
        [AdditionalAttribute("InterleaverSwitch")]
        InterleaverSwitch,
        [AdditionalAttribute("Min1X8Switch")]
        Min1X8Switch,
        [AdditionalAttribute("PboxSwitch")]
        PboxSwitch,
        [AdditionalAttribute("OMSSwitch")]
        OMSSwitch,
        [AdditionalAttribute("MPLUSSwitch")]
        MPLUSSwitch,
        [AdditionalAttribute("Automation")]
        Automation,
        [AdditionalAttribute("PDLController")]
        PDLController,
        [AdditionalAttribute("CDScan")]
        CDScan,
        [AdditionalAttribute("NEWFSTPScan")]
        NEWFSTPScan,
        [AdditionalAttribute("UDLSwitch")]
        UDLSwitch,
        [AdditionalAttribute("UDLTCC")]
        UDLTCC,
        [AdditionalAttribute("UDLFSTP")]
        UDLFSTP

    }
}
