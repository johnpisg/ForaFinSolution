public enum BgTaskStatus { Created, Processing, Completed, Failed }

public class BgTask
{
    public Guid Id { get; set; }
    public string StartsWith { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public BgTaskStatus Status { get; set; }
    public string? Comments { get; set; }
}