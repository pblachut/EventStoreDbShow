using System;
using MongoDB.Bson.Serialization.Attributes;

namespace EventStoreDbShowProjection
{
    public class Order
    {
        [BsonId]
        public string Id { get; set; }
        
        public string Content { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }
        
        public DateTimeOffset? LastPaidAt { get; set; }
        public int? Total { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}