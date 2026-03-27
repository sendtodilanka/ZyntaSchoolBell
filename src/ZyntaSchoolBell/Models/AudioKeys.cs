using System.Collections.Generic;

namespace ZyntaSchoolBell.Models
{
    public static class AudioKeys
    {
        public const string OpeningBell = "opening_bell";
        public const string Period1 = "period_1";
        public const string Period2 = "period_2";
        public const string Period3 = "period_3";
        public const string Period4 = "period_4";
        public const string Period5 = "period_5";
        public const string Period6 = "period_6";
        public const string Period7 = "period_7";
        public const string Period8 = "period_8";
        public const string Interval = "interval";
        public const string LunchBreak = "lunch_break";
        public const string AfternoonBell = "afternoon_bell";
        public const string ClosingBell = "closing_bell";
        public const string WarningBell = "warning_bell";
        public const string Assembly = "assembly";

        public static readonly Dictionary<string, string> FriendlyNames = new Dictionary<string, string>
        {
            { OpeningBell, "Opening Bell" },
            { Period1, "Period 1" },
            { Period2, "Period 2" },
            { Period3, "Period 3" },
            { Period4, "Period 4" },
            { Period5, "Period 5" },
            { Period6, "Period 6" },
            { Period7, "Period 7" },
            { Period8, "Period 8" },
            { Interval, "Interval" },
            { LunchBreak, "Lunch Break" },
            { AfternoonBell, "Afternoon Bell" },
            { ClosingBell, "Closing Bell" },
            { WarningBell, "Warning Bell" },
            { Assembly, "Assembly" }
        };

        public static readonly string[] All =
        {
            OpeningBell, Period1, Period2, Period3, Period4,
            Period5, Period6, Period7, Period8, Interval,
            LunchBreak, AfternoonBell, ClosingBell, WarningBell, Assembly
        };
    }
}
