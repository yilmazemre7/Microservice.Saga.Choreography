using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Stock.API.Models
{
    public class Stock
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement(Order = 0)]
        public Guid Id { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement(Order = 1)]
        public Guid ProductId { get; set; }

        [BsonElement(Order = 2)]
        public int Count { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonElement(Order = 3)]
        public DateTime CreatedDate { get; set; }
    }
}