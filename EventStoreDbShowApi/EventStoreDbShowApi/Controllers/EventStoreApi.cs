using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.AspNetCore.Mvc;

namespace EventStoreDbShowApi.Controllers
{
    [Route("api")]
    public class EventStoreApi : ControllerBase
    {
        private readonly EventStoreClient _esClient;

        public EventStoreApi(EventStoreClient esClient)
        {
            _esClient = esClient;
        }

        [HttpPost]
        [Route("register")]
        public async Task Register(OrderRegistered @event)
        {
            if (@event.Id == default)
                @event.Id = Guid.NewGuid();

            var eventData = new EventData(
                Uuid.NewUuid(),
                "OrderRegistered",
                JsonSerializer.SerializeToUtf8Bytes(@event)
            );

            await _esClient.AppendToStreamAsync(
                $"order-{@event.Id}",
                StreamState.NoStream,
                new[] {eventData});
        }
        
        [HttpPost]
        [Route("pay")]
        public async Task Pay(OrderPaid @event)
        {
            var eventData = new EventData(
                Uuid.NewUuid(),
                "OrderPaid",
                JsonSerializer.SerializeToUtf8Bytes(@event)
            );

            await _esClient.AppendToStreamAsync(
                $"order-{@event.Id}",
                StreamState.Any,
                new[] {eventData});
        }
        
        
        [HttpPost]
        [Route("complete")]
        public async Task Pay(OrderCompleted @event)
        {
            var eventData = new EventData(
                Uuid.NewUuid(),
                "OrderCompleted",
                JsonSerializer.SerializeToUtf8Bytes(@event)
            );

            await _esClient.AppendToStreamAsync(
                $"order-{@event.Id}",
                StreamRevision.FromInt64(@event.StreamExpectedVersion), 
                new[] {eventData});
        }

        [HttpGet]
        [Route("getOrder")]
        public async Task<List<string>> ReadStream(Guid orderId)
        {
            var read = _esClient.ReadStreamAsync(
                Direction.Forwards,
                $"order-{orderId}", StreamPosition.Start
            );

            var events = await read.ToListAsync();

            return events
                .Select(e => Encoding.UTF8.GetString(e.Event.Data.ToArray()))
                .ToList();
        }
    }
}