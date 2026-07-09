using Domain.Common;

namespace Domain.Entities;

public class RequestType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<InternalRequest> InternalRequests { get; set; } = [];
}
