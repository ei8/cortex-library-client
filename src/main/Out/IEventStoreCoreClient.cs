using ei8.Cortex.Library.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ei8.Cortex.Library.Client.Out
{
    public interface IEventStoreCoreClient
    {
        Task<IEnumerable<Notification>> Get(string avatarUrl, Guid aggregateId, int fromVersion, string bearerToken, CancellationToken cancellationToken = default);
    }
}
