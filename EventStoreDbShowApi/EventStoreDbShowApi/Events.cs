using System;

namespace EventStoreDbShowApi
{
    public class OrderRegistered
    {
         public Guid Id { get; set; }
         public string Content { get; set; }
         public DateTimeOffset When { get; set; }
    }
    
    public class OrderPaid
    {
        public Guid Id { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTimeOffset When { get; set; }
    }

    public class OrderCompleted
    {
        public Guid Id { get; set; }
        public DateTimeOffset When { get; set; }
        public long StreamExpectedVersion { get; set; }
    }
}