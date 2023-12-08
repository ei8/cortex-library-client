using ei8.Cortex.Library.Common;
using neurUL.Common.Http;
using NLog;
using Polly;
using Polly.Retry;
using Splat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ei8.Cortex.Library.Client.Out
{
    public class HttpEventStoreCoreClient : IEventStoreCoreClient
    {
        private readonly IRequestProvider requestProvider;

        private static AsyncRetryPolicy exponentialRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                (ex, _) => HttpEventStoreCoreClient.logger.Error(ex, "Error occurred while querying Event Store. " + ex.InnerException?.Message)
            );

        private static readonly string GetEventStorePathTemplate = "cortex/eventstore";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public HttpEventStoreCoreClient(IRequestProvider requestProvider = null)
        {
            this.requestProvider = requestProvider ?? Locator.Current.GetService<IRequestProvider>();
        }

        public async Task<IEnumerable<Notification>> Get(string avatarUrl, Guid aggregateId, int fromVersion, string bearerToken, CancellationToken cancellationToken = default) =>
            await HttpEventStoreCoreClient.exponentialRetryPolicy.ExecuteAsync(
                async () => await this.GetInternal(avatarUrl, aggregateId, fromVersion, bearerToken, cancellationToken).ConfigureAwait(false)
                );

        private async Task<IEnumerable<Notification>> GetInternal(string avatarUrl, Guid aggregateId, int fromVersion, string bearerToken, CancellationToken token = default(CancellationToken))
        {
            return await requestProvider.GetAsync<IEnumerable<Notification>>(
                           $"{avatarUrl}{HttpEventStoreCoreClient.GetEventStorePathTemplate}/{aggregateId.ToString()}",
                           bearerToken,
                           token
                           );
        }
    }
}
