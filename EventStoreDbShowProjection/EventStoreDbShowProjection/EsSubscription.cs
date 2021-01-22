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

        private StreamSubscription? _streamSubscription;

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

            _streamSubscription = checkpoint.HasValue
                ? await _esClient.SubscribeToStreamAsync(
                    _streamName,
                    StreamPosition.FromInt64(checkpoint.Value), 
                    async (subscription, evnt, cancellationToken) => await OnEventAppeared(evnt),
                    resolveLinkTos: true)
                : await _esClient.SubscribeToStreamAsync(
                    _streamName,
                    async (subscription, evnt, cancellationToken) => await OnEventAppeared(evnt), 
                    resolveLinkTos: true);


            async Task OnEventAppeared(ResolvedEvent esEvent)
            {
                await Handle(esEvent);

                await _saveStreamCheckpoint(_streamName, esEvent.OriginalEventNumber.ToInt64());
            }
        }

        public async Task Stop() => _streamSubscription?.Dispose();
            

        protected abstract Task Handle(ResolvedEvent @event);
    }
}