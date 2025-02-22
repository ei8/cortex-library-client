/*
   This file is part of the d# project.
   Copyright (c) 2016-2021 ei8
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
using neurUL.Common.Http;
using NLog;
using Polly;
using Polly.Retry;
using Splat;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ei8.Cortex.Library.Client.Out
{
    public class HttpNeuronQueryClient : INeuronQueryClient
    {
        private readonly IRequestProvider requestProvider;

        private static AsyncRetryPolicy exponentialRetryPolicy = Policy
           .Handle<Exception>()
           .WaitAndRetryAsync(
               3,
               attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
               (ex, _) => HttpNeuronQueryClient.logger.Error(ex, "Error occurred while communicating with ei8 Cortex Library. " + ex.InnerException?.Message)
           );

        private static readonly string GetNeuronsPathTemplate = "cortex/neurons";
        private static readonly string GetRelativesPathTemplate = GetNeuronsPathTemplate + "/{0}/relatives";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public HttpNeuronQueryClient(IRequestProvider requestProvider = null)
        {
            this.requestProvider = requestProvider ?? Locator.Current.GetService<IRequestProvider>();
        }

        public async Task<QueryResult<Neuron>> GetNeuronById(string avatarUrl, string id, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.GetNeuronByIdInternal(avatarUrl, id, neuronQuery, bearerToken, token).ConfigureAwait(false)
                );

        private async Task<QueryResult<Neuron>> GetNeuronByIdInternal(string avatarUrl, string id, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken))
        {
            return await HttpNeuronQueryClient.GetNeuronsUnescaped(
                avatarUrl,
                $"{HttpNeuronQueryClient.GetNeuronsPathTemplate}/{id}",
                neuronQuery.ToString(),
                token,
                requestProvider,
                bearerToken,
                string.Empty
                );
        }

        public async Task<QueryResult<Neuron>> GetNeuronById(string avatarUrl, string id, string centralId, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.GetNeuronByIdInternal(avatarUrl, id, centralId, neuronQuery, bearerToken, token).ConfigureAwait(false));

        private async Task<QueryResult<Neuron>> GetNeuronByIdInternal(string avatarUrl, string id, string centralId, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken))
        {
            return await HttpNeuronQueryClient.GetNeuronsUnescaped(
                avatarUrl,
                $"{HttpNeuronQueryClient.GetNeuronsPathTemplate}/{centralId}/relatives/{id}",
                neuronQuery.ToString(),
                token,
                requestProvider,
                bearerToken,
                string.Empty
                );
        }

        public async Task<QueryResult<Neuron>> GetNeurons(string avatarUrl, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await this.GetNeurons(avatarUrl, null, neuronQuery, bearerToken, token);

        public async Task<QueryResult<Neuron>> GetNeuronsInternal(string avatarUrl, NeuronQuery neuronQuery, string userId, CancellationToken token = default) =>
            await this.GetNeuronsInternal(avatarUrl, null, neuronQuery, userId, token);

        public async Task<QueryResult<Neuron>> GetNeuronsInternal(string avatarUrl, string centralId, NeuronQuery neuronQuery, string userId, CancellationToken token = default)
        {
            userId.ValidateStringParameter(nameof(userId));

            return await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.GetNeuronsCore(avatarUrl, centralId, neuronQuery, string.Empty, userId, token).ConfigureAwait(false));
        }

        public async Task<QueryResult<Neuron>> GetNeurons(string avatarUrl, string centralId, NeuronQuery neuronQuery, string bearerToken, CancellationToken token = default(CancellationToken))
        { 
            return await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.GetNeuronsCore(avatarUrl, centralId, neuronQuery, bearerToken, string.Empty, token).ConfigureAwait(false));
        }

        private async Task<QueryResult<Neuron>> GetNeuronsCore(string avatarUrl, string centralId, NeuronQuery neuronQuery, string bearerToken, string userId, CancellationToken token = default(CancellationToken))
        {
            var path = string.IsNullOrEmpty(centralId) ? 
                HttpNeuronQueryClient.GetNeuronsPathTemplate : 
                string.Format(HttpNeuronQueryClient.GetRelativesPathTemplate, centralId);

            return await HttpNeuronQueryClient.GetNeuronsUnescaped(
                avatarUrl, 
                path,
                neuronQuery.ToString(), 
                token, 
                requestProvider, 
                bearerToken,
                userId
                );
        }

        private static async Task<QueryResult<Neuron>> GetNeuronsUnescaped(string avatarUrl, string path, string queryString, CancellationToken token, IRequestProvider requestProvider, string bearerToken, string userId)
        {
            queryString +=
                // user id is specified
                !string.IsNullOrEmpty(userId) ?
                    (
                        // querystring is specified
                        !string.IsNullOrEmpty(queryString) ?
                            "&userid=" + userId :
                            "?userid=" + userId
                    ) :
                    string.Empty;

            var result = await requestProvider.GetAsync<QueryResult<Neuron>>(
                           $"{avatarUrl}{path}{queryString}",
                           bearerToken,
                           token
                           );
            result.Items.ToList().ForEach(n => n.UnescapeTag());
            return result;
        }

        public async Task<QueryResult<Neuron>> SendQuery(string queryUrl, string bearerToken, CancellationToken token = default(CancellationToken)) =>
            await HttpNeuronQueryClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.SendQueryInternal(queryUrl, bearerToken, token).ConfigureAwait(false));

        public async Task<QueryResult<Neuron>> SendQueryInternal(string queryUrl, string bearerToken, CancellationToken token = default)
        {
            QueryResult<Neuron> result = new QueryResult<Neuron>() { Items = new Neuron[0] };

            if (QueryUrl.TryParse(queryUrl, out QueryUrl request))
            {   
                NeuronQuery query = NeuronQuery.TryParse(request.QueryString, out NeuronQuery nquery) ? 
                    nquery : 
                    new NeuronQuery(); 

                if (request.Id.Length > 0)
                {
                    if (request.HasRelatives)
                    {
                        // http://[avatar]/cortex/neurons/[id]/relatives/[id2]
                        if (request.Id2.Length > 0)
                        {
                            result = await this.GetNeuronByIdInternal(request.AvatarUrl, request.Id2, request.Id, query, bearerToken, token);
                        }
                        // http://[avatar]/cortex/neurons/[id]/relatives
                        else
                        {
                            result = await this.GetNeuronsCore(request.AvatarUrl, request.Id, query, bearerToken, string.Empty, token);
                        }
                    }
                    else
                    {
                        // http://[avatar]/cortex/neurons/[id]
                        result = await this.GetNeuronByIdInternal(request.AvatarUrl, request.Id, query, bearerToken, token);
                    }
                }
                // http://[avatar]/cortex/neurons
                else
                    result = await this.GetNeuronsCore(request.AvatarUrl, request.Id, query, bearerToken, string.Empty, token);
            }

            return result;
        }
    }
}