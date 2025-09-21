namespace Server.Transactions.Inventory.Models;

public record Product(string Id, string Name, string Description, decimal Price, int QuantityOnHand);
