using ei8.Cortex.Library.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ei8.Cortex.Library.Client
{
    public static class ExtensionMethods
    {
        internal static void UnescapeTag(this Neuron value)
        {
            if (value.Tag != null)
                value.Tag = Regex.Unescape(value.Tag);
        }
    }
}
