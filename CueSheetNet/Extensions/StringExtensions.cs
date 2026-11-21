using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Extensions;

internal static class StringExtensions
{
    extension(string s)
    {
        public string? NonEmpty() => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
