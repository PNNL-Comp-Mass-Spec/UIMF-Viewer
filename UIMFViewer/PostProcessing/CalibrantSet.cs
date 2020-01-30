using System;
using System.ComponentModel;

namespace UIMFViewer.PostProcessing
{
    [Flags]
    public enum CalibrantSet : uint
    {
        [Description("All")]
        All = 0xFFFFFFFF,

        [Description("Agilent Tune Mix (Positive)")]
        AgilentTuneMixPositive = 1 << 0,

        [Description("Agilent Tune Mix (Negative)")]
        AgilentTuneMixNegative = 1 << 1,

        [Description("Human Peptides")]
        HumanPeptides = 1 << 2,

        [Description("BSA (Bovine) Peptides")]
        BSAPeptides = 1 << 3,

        [Description("Quaternary Amines (Positive)")]
        QuaternaryAminesPositive = 1 << 4,

        [Description("Polyalanines (Negative)")]
        PolyalaninesNegative = 1 << 5,

        // Last option, always
        [Description("Custom (Provide Path)")]
        Custom = (uint)1 << 31,
    }
}
