﻿using ei8.Cortex.Library.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ei8.Cortex.Library.Client
{
    public static class ExtensionMethods
    {
        internal static string ToQueryString(this NeuronQuery value)
        {
            var queryStringBuilder = new StringBuilder();

            ExtensionMethods.AppendQuery(value.Id, nameof(NeuronQuery.Id), queryStringBuilder);
            ExtensionMethods.AppendQuery(value.IdNot, nameof(NeuronQuery.IdNot), queryStringBuilder);
            ExtensionMethods.AppendQuery(value.TagContains, nameof(NeuronQuery.TagContains), queryStringBuilder);
            ExtensionMethods.AppendQuery(value.TagContainsNot, nameof(NeuronQuery.TagContainsNot), queryStringBuilder);
            ExtensionMethods.AppendQuery(value.Presynaptic, nameof(NeuronQuery.Presynaptic), queryStringBuilder);
            ExtensionMethods.AppendQuery(value.PresynapticNot, nameof(NeuronQuery.PresynapticNot), queryStringBuilder);
            ExtensionMethods.AppendQuery(value.Postsynaptic, nameof(NeuronQuery.Postsynaptic), queryStringBuilder);
            ExtensionMethods.AppendQuery(value.PostsynapticNot, nameof(NeuronQuery.PostsynapticNot), queryStringBuilder);

            ExtensionMethods.AppendQuery(
                    value.RelativeValues,
                    "relative",
                    v => ((int)v).ToString(),
                    queryStringBuilder
                    );

            ExtensionMethods.AppendQuery(
                    value.Limit,
                    "limit",
                    v => v.ToString(),
                    queryStringBuilder
                    );

            ExtensionMethods.AppendQuery(
                    value.NeuronActiveValues,
                    "nactive",
                    v => ((int)v).ToString(),
                    queryStringBuilder
                    );

            ExtensionMethods.AppendQuery(
                    value.TerminalActiveValues,
                    "tactive",
                    v => ((int)v).ToString(),
                    queryStringBuilder
                    );

            if (queryStringBuilder.Length > 0)
                queryStringBuilder.Insert(0, '?');

            return queryStringBuilder.ToString();
        }

        internal static void UnescapeTag(this Neuron value)
        {
            if (value.Tag != null)
                value.Tag = Regex.Unescape(value.Tag);
        }

        private static void AppendQuery(IEnumerable<string> field, string fieldName, StringBuilder queryStringBuilder)
        {
            if (field != null && field.Any())
            {
                if (queryStringBuilder.Length > 0)
                    queryStringBuilder.Append('&');
                queryStringBuilder.Append(string.Join("&", field.Select(s => $"{fieldName}={s}")));
            }
        }
        private static void AppendQuery<T>(Nullable<T> nullableValue, string queryStringKey, Func<T, string> valueProcessor, StringBuilder queryStringBuilder) where T : struct
        {
            if (nullableValue.HasValue)
            {
                if (queryStringBuilder.Length > 0)
                    queryStringBuilder.Append('&');

                queryStringBuilder
                    .Append($"{queryStringKey}=")
                    .Append(valueProcessor(nullableValue.Value));
            }
        }
    }
}
