using System.Text.RegularExpressions;

namespace ei8.Cortex.Library.Client
{
    public class QueryUrl
    {
        private const string Pattern = @"^(?<AvatarUrl>.+?(?=cortex\/neurons))
								cortex\/neurons
								(
									\/
									(?<Id>[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12})
									(?<Relatives>
										\/
										relatives
										(
											\/
											(?<Id2>[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12})
										)?
									)?
								)?
								(
									\?
									(?<QueryString>.+)
								)?
								$";

        public static bool TryParse(string input, out QueryUrl result)
        {
            var bResult = false;
            result = null;

            // recognize path and delegate to other methods (ie. GetNeurons, GetNeuronById, etc.)
            var m = Regex.Match(input, QueryUrl.Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            if (m.Success)
            {
                bResult = true;
                result = new QueryUrl();
                result.AvatarUrl = QueryUrl.GetMatchValue(m, "AvatarUrl");
                result.Id = QueryUrl.GetMatchValue(m, "Id");
                result.HasRelatives = QueryUrl.GetMatchValue(m, "Relatives").Length > 0;
                result.Id2 = QueryUrl.GetMatchValue(m, "Id2");

                var queryString = QueryUrl.GetMatchValue(m, "QueryString");
                if (queryString.Length > 0)
                {
                    // replace with actual null character
                    // TODO: transfer to common to centralize processing nullable regionid
                    queryString = queryString.Replace("\\0", "\0");
                    result.QueryString = queryString;
                }
            }

            return bResult;
        }

        public string AvatarUrl { get; private set; }

        public string Id { get; private set; }

        public bool HasRelatives { get; private set; }
        
        public string Id2 { get; private set; }

        public string QueryString { get; private set; }

        private static string GetMatchValue(Match m, string groupName)
        {
            return m.Groups[groupName].Success ? m.Groups[groupName].Value : string.Empty;
        }
    }
}
