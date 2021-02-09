/*
   This file is part of the d# project.
   Copyright (c) 2016-2018 ei8
   Authors: ei8
    This program is free software; you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License version 3
   as published by the Free Software Foundation with the addition of the
   following permission added to Section 15 as permitted in Section 7(a):
   FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
   EI8. EI8 DISCLAIMS THE WARRANTY OF NON INFRINGEMENT OF THIRD PARTY RIGHTS
    This program is distributed in the hope that it will be useful, but
   WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
   or FITNESS FOR A PARTICULAR PURPOSE.
   See the GNU Affero General Public License for more details.
   You should have received a copy of the GNU Affero General Public License
   along with this program; if not, see http://www.gnu.org/licenses or write to
   the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
   Boston, MA, 02110-1301 USA, or download the license from the following URL:
   https://github.com/ei8/cortex-diary/blob/master/LICENSE
    The interactive user interfaces in modified source and object code versions
   of this program must display Appropriate Legal Notices, as required under
   Section 5 of the GNU Affero General Public License.
    You can be released from the requirements of the license by purchasing
   a commercial license. Buying such a license is mandatory as soon as you
   develop commercial activities involving the d# software without
   disclosing the source code of your own applications.
    For more information, please contact ei8 at this address: 
    support@ei8.works
*/

using ei8.Cortex.Library.Common;
using IdentityModel.Client;
using Nancy.Helpers;
using neurUL.Common.Http;
using NLog;
using Polly;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ei8.Cortex.Library.Client.Out
{
    public class HttpNeuronQueryClient : INeuronQueryClient
    {
        private readonly IRequestProvider requestProvider;
        
        private static Policy exponentialRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                (ex, _) => HttpNeuronQueryClient.logger.Error(ex, "Error occurred while querying Cortex Graph. " + ex.InnerException?.Message)
            );

        private static readonly string GetNeuronsPathTemplate = "cortex/neurons";
        private static readonly string GetRelativesPathTemplate = GetNeuronsPathTemplate + "/{0}/relatives";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public HttpNeuronQueryClient(IRequestProvider requestProvider = null)
        {
            this.requestProvider = requestProvider ?? Locator.Current.GetService<IRequestProvider>();
        }

        public async Task<QueryResult> GetNeuronById(string avatarUrl, string id, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.GetNeuronByIdInternal(avatarUrl, id, neuronQuery, bearerToken, token).ConfigureAwait(false)
                );

        private async Task<QueryResult> GetNeuronByIdInternal(string avatarUrl, string id, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken))
        {
            return await HttpNeuronQueryClient.GetNeuronsUnescaped(
                avatarUrl,
                $"{HttpNeuronQueryClient.GetNeuronsPathTemplate}/{id}",
                neuronQuery.ToQueryString(),
                token,
                requestProvider,
                bearerToken
                );
        }

        public async Task<QueryResult> GetNeuronById(string avatarUrl, string id, string centralId, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.GetNeuronByIdInternal(avatarUrl, id, centralId, neuronQuery, bearerToken, token).ConfigureAwait(false));

        private async Task<QueryResult> GetNeuronByIdInternal(string avatarUrl, string id, string centralId, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken))
        {
            return await HttpNeuronQueryClient.GetNeuronsUnescaped(
                avatarUrl,
                $"{HttpNeuronQueryClient.GetNeuronsPathTemplate}/{centralId}/relatives/{id}",
                neuronQuery.ToQueryString(),
                token,
                requestProvider,
                bearerToken
                );
        }

        public async Task<QueryResult> GetNeurons(string avatarUrl, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await this.GetNeurons(avatarUrl, null, neuronQuery, bearerToken, token);

        public async Task<QueryResult> GetNeurons(string avatarUrl, string centralId, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.GetNeuronsInternal(avatarUrl, centralId, neuronQuery, bearerToken, token).ConfigureAwait(false));

        private async Task<QueryResult> GetNeuronsInternal(string avatarUrl, string centralId, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken))
        {
            var path = string.IsNullOrEmpty(centralId) ? 
                HttpNeuronQueryClient.GetNeuronsPathTemplate : 
                string.Format(HttpNeuronQueryClient.GetRelativesPathTemplate, centralId);

            return await HttpNeuronQueryClient.GetNeuronsUnescaped(
                avatarUrl, 
                path,
                neuronQuery.ToQueryString(), 
                token, 
                requestProvider, 
                bearerToken
                );
        }

        private static async Task<QueryResult> GetNeuronsUnescaped(string avatarUrl, string path, string queryString, CancellationToken token, IRequestProvider requestProvider, string bearerToken)
        {
            var result = await requestProvider.GetAsync<QueryResult>(
                           $"{avatarUrl}{path}{queryString}",
                           bearerToken,
                           token
                           );
            result.Neurons.ToList().ForEach(n => n.UnescapeTag());
            return result;
        }

        public async Task<QueryResult> SendQuery(string queryUrl, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.SendQueryInternal(queryUrl, bearerToken, token).ConfigureAwait(false));

        public async Task<QueryResult> SendQueryInternal(string queryUrl, string bearerToken, CancellationToken token = default)
        {
            QueryResult result = new QueryResult() { Neurons = new NeuronResult[0] };

            if (QueryUrl.TryParse(queryUrl, out QueryUrl request))
            {   
                NeuronQuery query = new NeuronQuery();
                if (request.QueryString.Count > 0)
                {                    
                    query.Id = HttpNeuronQueryClient.GetNameValue(request.QueryString, "Id");
                    query.IdNot = HttpNeuronQueryClient.GetNameValue(request.QueryString, "IdNot");
                    query.TagContains = HttpNeuronQueryClient.GetNameValue(request.QueryString, "TagContains");
                    query.TagContainsNot = HttpNeuronQueryClient.GetNameValue(request.QueryString, "TagContainsNot");
                    query.Presynaptic = HttpNeuronQueryClient.GetNameValue(request.QueryString, "Presynaptic");
                    query.PresynapticNot = HttpNeuronQueryClient.GetNameValue(request.QueryString, "PresynapticNot");
                    query.Postsynaptic = HttpNeuronQueryClient.GetNameValue(request.QueryString, "Postsynaptic");
                    query.PostsynapticNot = HttpNeuronQueryClient.GetNameValue(request.QueryString, "PostsynapticNot");
                    query.RegionId = HttpNeuronQueryClient.GetNameValue(request.QueryString, "RegionId");
                    query.RegionIdNot = HttpNeuronQueryClient.GetNameValue(request.QueryString, "RegionIdNot");
                    query.RelativeValues = HttpNeuronQueryClient.GetNullableEnumValue<RelativeValues>(request.QueryString, "relative");
                    query.PageSize = HttpNeuronQueryClient.GetNullableIntValue(request.QueryString, "pagesize");
                    query.Page = HttpNeuronQueryClient.GetNullableIntValue(request.QueryString, "page");
                    query.NeuronActiveValues = HttpNeuronQueryClient.GetNullableEnumValue<ActiveValues>(request.QueryString, "nactive");
                    query.TerminalActiveValues = HttpNeuronQueryClient.GetNullableEnumValue<ActiveValues>(request.QueryString, "tactive");
                    query.SortBy = HttpNeuronQueryClient.GetNullableEnumValue<SortByValue>(request.QueryString, "sortby");
                    query.SortOrder = HttpNeuronQueryClient.GetNullableEnumValue<SortOrderValue>(request.QueryString, "sortorder");
                }

                if (request.Id.Length > 0)
                {
                    if (request.HasRelatives)
                    {
                        // http://127.0.0.1:60001/avatars/[avatar]/cortex/neurons/[id]/relatives/[id2]
                        if (request.Id2.Length > 0)
                        {
                            result = await this.GetNeuronByIdInternal(request.AvatarUrl, request.Id2, request.Id, query, bearerToken, token);
                        }
                        // http://127.0.0.1:60001/avatars/[avatar]/cortex/neurons/[id]/relatives
                        else
                        {
                            result = await this.GetNeuronsInternal(request.AvatarUrl, request.Id, query, bearerToken, token);
                        }
                    }
                    else
                    {
                        // http://127.0.0.1:60001/avatars/[avatar]/cortex/neurons/[id]
                        result = await this.GetNeuronByIdInternal(request.AvatarUrl, request.Id, query, bearerToken, token);
                    }
                }
                // http://127.0.0.1:60001/avatars/[avatar]/cortex/neurons
                else
                    result = await this.GetNeuronsInternal(request.AvatarUrl, request.Id, query, bearerToken, token);
            }

            return result;
        }

        // TODO: Transfer to common
        private static int? GetNullableIntValue(NameValueCollection nameValues, string name)
        {
            int? result = null;

            if (nameValues[name] != null)
                result = int.Parse(nameValues[name]);

            return result;
        }

        // TODO: Transfer to common
        private static T? GetNullableEnumValue<T>(NameValueCollection nameValues, string name) where T : struct, Enum
        {
            return nameValues[name] != null ? (T?)Enum.Parse(typeof(T), nameValues[name].ToString(), true) : null;
        }

        // TODO: Transfer to common, consolidate with ei8.Cortex.Library.Port.Adapter.Out.Api.NeuronModule helper methods
        private static IEnumerable<string> GetNameValue(NameValueCollection nameValues, string name)
        {
            var parameterNameExclamation = name.Replace("Not", "!");
            string[] stringArray = nameValues[name] != null ?
                nameValues[name].ToString().Split(',') :
                    nameValues[parameterNameExclamation] != null ?
                    nameValues[parameterNameExclamation].ToString().Split(',') :
                    null;

            return stringArray != null ? stringArray.Select(s => s != "\0" ? s : null) : stringArray;
        }

    }
}