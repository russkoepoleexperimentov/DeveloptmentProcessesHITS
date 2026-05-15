using Domain.Models.Criteria;

namespace GoogleClass.Models;

public abstract class GenericPost : Post
{
    public virtual ICollection<FilePost> FilePosts { get; set; } = new List<FilePost>();
    public virtual ICollection<Criterion> Criteria { get; set; } = new List<Criterion>();
}
