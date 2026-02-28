using MongoDB.Bson.Serialization.Attributes;
using System.Threading.Tasks;

namespace OrdersMicroservice.API.Entities;

public class Order
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid _id { get; set; }

    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid OrderID { get; set; }

    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public string UserID { get; set; }

    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public DateTime OrderDate { get; set; }

    [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
    public decimal TotalBill { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
