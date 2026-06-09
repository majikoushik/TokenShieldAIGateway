namespace TokenShield.Domain.Common;

public interface IHasTimestamps
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
}

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public abstract class BaseEntity : IHasTimestamps, ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
