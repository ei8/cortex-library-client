using ei8.Cortex.Library.Common;
using neurUL.Common.Domain.Model;
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

        internal static void ValidateStringParameter(this string value, string parameterName)
        {
            AssertionConcern.AssertArgumentNotNull(value, parameterName);
            AssertionConcern.AssertArgumentNotEmpty(value, "Specified parameter cannot be an empty string.", parameterName);
        }
    }
}
