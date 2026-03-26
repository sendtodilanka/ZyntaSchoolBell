using System.Collections.Generic;

namespace ZyntaSchoolBell.Models
{
    public static class AudioKeys
    {
        public const string OpeningBell = "opening_bell";
        public const string PeriodStart = "period_start";
        public const string Interval = "interval";
        public const string LunchBreak = "lunch_break";
        public const string AfternoonBell = "afternoon_bell";
        public const string ClosingBell = "closing_bell";
        public const string WarningBell = "warning_bell";
        public const string Assembly = "assembly";

        public static readonly Dictionary<string, string> FriendlyNames = new Dictionary<string, string>
        {
            { OpeningBell, "Opening Bell" },
            { PeriodStart, "Period Start" },
            { Interval, "Interval" },
            { LunchBreak, "Lunch Break" },
            { AfternoonBell, "Afternoon Bell" },
            { ClosingBell, "Closing Bell" },
            { WarningBell, "Warning Bell" },
            { Assembly, "Assembly" }
        };

        public static readonly string[] All =
        {
            OpeningBell, PeriodStart, Interval, LunchBreak,
            AfternoonBell, ClosingBell, WarningBell, Assembly
        };
    }
}
