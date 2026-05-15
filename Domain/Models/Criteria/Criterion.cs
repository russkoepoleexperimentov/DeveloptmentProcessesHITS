using GoogleClass.Models;

namespace Domain.Models.Criteria;

public abstract class Criterion : BaseEntityWithId
{
    public string Title { get; set; } = string.Empty;

    public Guid PostId { get; set; }
    public virtual GenericPost Post { get; set; } = null!;

    public int OrderIndex { get; set; }
}
