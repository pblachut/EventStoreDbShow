using System.Threading.Tasks;
using EventStore.Client;

namespace EventStoreDbShowProjection
{
    public delegate Task SaveStreamCheckpoint(string subscriptionName, long position);

    public delegate Task<long?> TryGetCheckpoint(string subscriptionName);
    
    public abstract class EsSubscription
    {
        private readonly EventStoreClient _esClient;
        private readonly string _streamName;
        private readonly TryGetCheckpoint _tryGetCheckpoint;
        private readonly SaveStreamCheckpoint _saveStreamCheckpoint;

        protected EsSubscription(
            EventStoreClient esClient, 
            string streamName, 
            TryGetCheckpoint tryGetCheckpoint, 
            SaveStreamCheckpoint saveStreamCheckpoint)
        {
            _esClient = esClient;
            _streamName = streamName;
            _tryGetCheckpoint = tryGetCheckpoint;
            _saveStreamCheckpoint = saveStreamCheckpoint;
        }

        public async Task Start()
        {
            var checkpoint = await _tryGetCheckpoint(_streamName);
            var streamPosition = checkpoint.HasValue
                ? StreamPosition.FromInt64(checkpoint.Value)
                : StreamPosition.Start;
            
            await _esClient.SubscribeToStreamAsync(
                _streamName,
                streamPosition, 
                async (subscription, evnt, cancellationToken) =>
                {
                    await Handle(evnt);

                    await _saveStreamCheckpoint(_streamName, evnt.OriginalEventNumber.ToInt64());
                });
        }
            

        protected abstract Task Handle(ResolvedEvent @event);
    }
}