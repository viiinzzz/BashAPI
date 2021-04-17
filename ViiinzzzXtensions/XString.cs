using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xtensions.Std
{
    public static class XString
    {
        const string bash_reserved = " \t!\"#$&'()*,;<=>?[\\]^`{|}~";
        static bool isBashReserved(char c) { if (c == '\r' || c == '\n') throw new ArgumentException("bash filename must not include newline char."); return bash_reserved.Contains(c); }
        public static string EscapeBashFileName(this string x)
            => x.Select(c => isBashReserved(c) ? @$"\{c}" : @$"{c}").Aggregate((x,y)=>$"{x}{y}");
    }
}
