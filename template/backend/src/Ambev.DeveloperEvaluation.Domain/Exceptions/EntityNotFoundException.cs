namespace Ambev.DeveloperEvaluation.Domain.Exceptions;

public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object entityId)
        : base($"{entityName} with identifier {entityId} was not found")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
