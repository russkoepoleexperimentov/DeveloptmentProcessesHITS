namespace GoogleClass.Models;

public abstract class BaseEntityWithId : BaseEntity
{
    public Guid Id { get; set; }
}