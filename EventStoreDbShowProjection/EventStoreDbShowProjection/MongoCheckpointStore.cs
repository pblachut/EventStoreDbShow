using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace EventStoreDbShowProjection
{
    public static class MongoCheckpointStore
    {
        public static TryGetCheckpoint PrepareTryGetCheckpoint(IMongoCollection<Checkpoint> collection) =>
            async subscriptionName =>
            {
                var filter = Builders<Checkpoint>.Filter.Eq(c => c.SubscriptionName, subscriptionName);

                var checkpoint = await collection.Find(filter).SingleOrDefaultAsync();

                return checkpoint?.Value;
            };

        public static SaveStreamCheckpoint PrepareSaveStreamCheckpoint(IMongoCollection<Checkpoint> collection) =>
            async (subscriptionName, value) =>
            {

                var filter = Builders<Checkpoint>.Filter.Eq(c => c.SubscriptionName, subscriptionName);

                var update = Builders<Checkpoint>.Update.Set(c => c.Value, value);

                await collection.UpdateOneAsync(filter, update, new UpdateOptions {IsUpsert = true});

            };

    }

    public class Checkpoint
    {
        public Checkpoint(string subscriptionName, long value)
        {
            SubscriptionName = subscriptionName;
            Value = value;
        }

        [BsonId]
        public string SubscriptionName { get; set; }
        public long Value { get; set; }
    }
}
