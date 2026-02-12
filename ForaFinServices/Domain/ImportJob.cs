public class ImportJob
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Tries { get; set; } = 0;
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // Ej: "StartImport"
    public string Content { get; set; } = string.Empty; // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; } // Para saber si ya se envió a la cola
}