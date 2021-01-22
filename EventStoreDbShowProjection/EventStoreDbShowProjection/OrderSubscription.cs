using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using EventStoreDbShowApi;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace EventStoreDbShowProjection
{
    public class OrderSubscription : EsSubscription, IHostedService
    {
        private readonly IMongoCollection<Order> _orders;

        public OrderSubscription(IMongoCollection<Order> orders, EventStoreClient esClient, TryGetCheckpoint tryGetCheckpoint, SaveStreamCheckpoint saveStreamCheckpoint) : base(esClient, "$ce-order", tryGetCheckpoint, saveStreamCheckpoint)
        {
            _orders = orders;
        }

        protected override Task Handle(ResolvedEvent @event)
            => @event.Event.EventType switch
            {
                "OrderRegistered" => OnOrderRegistered(Deserialize<OrderRegistered>(@event)),
                "OrderPaid" => OnOrderPaid(Deserialize<OrderPaid>(@event)),
                "OrderCompleted" => OnOrderCompleted(Deserialize<OrderCompleted>(@event)),
                _ => throw new Exception("Not supported event")
            };

        private T Deserialize<T>(ResolvedEvent @event)
            => JsonSerializer.Deserialize<T>(@event.Event.Data.ToArray())!;

        public Task OnOrderRegistered(OrderRegistered @event)
        {
            var order = new Order
            {
                Id = @event.Id.ToString(),
                Content = @event.Content,
                CreatedAt = @event.When
            };

            return _orders.InsertOneAsync(order);
        }

        public async Task OnOrderPaid(OrderPaid @event)
        {
            var filter = Builders<Order>.Filter.Eq(c => c.Id, @event.Id.ToString());

            var order = await _orders.Find(filter).SingleAsync();

            order.Total += @event.TotalPaid;
            order.LastPaidAt = @event.When;

            await _orders.ReplaceOneAsync(filter, order);
        }

        public async Task OnOrderCompleted(OrderCompleted @event)
        {
            var filter = Builders<Order>.Filter.Eq(c => c.Id, @event.Id.ToString());

            var order = await _orders.Find(filter).SingleAsync();

            order.CompletedAt = @event.When;

            await _orders.ReplaceOneAsync(filter, order);
        }

        public Task StartAsync(CancellationToken cancellationToken) => Start();

        public Task StopAsync(CancellationToken cancellationToken) => Stop();
    }
}
