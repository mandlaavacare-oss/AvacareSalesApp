namespace Server.Infrastructure.Database.Entities;

public class OrderEntity
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;
}
