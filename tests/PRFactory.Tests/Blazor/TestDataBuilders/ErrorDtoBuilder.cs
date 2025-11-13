using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Tests.Blazor.TestDataBuilders;

/// <summary>
/// Builder for creating ErrorDto instances for testing
/// </summary>
public class ErrorDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _tenantId = Guid.NewGuid();
    private ErrorSeverity _severity = ErrorSeverity.Medium;
    private string _message = "Test error message";
    private string? _stackTrace = null;
    private string? _entityType = null;
    private Guid? _entityId = null;
    private string? _contextData = null;
    private bool _isResolved = false;
    private DateTime? _resolvedAt = null;
    private string? _resolvedBy = null;
    private string? _resolutionNotes = null;
    private DateTime _createdAt = DateTime.UtcNow;

    public ErrorDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ErrorDtoBuilder WithTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public ErrorDtoBuilder WithSeverity(ErrorSeverity severity)
    {
        _severity = severity;
        return this;
    }

    public ErrorDtoBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    public ErrorDtoBuilder WithStackTrace(string stackTrace)
    {
        _stackTrace = stackTrace;
        return this;
    }

    public ErrorDtoBuilder WithEntity(string entityType, Guid entityId)
    {
        _entityType = entityType;
        _entityId = entityId;
        return this;
    }

    public ErrorDtoBuilder WithContextData(string contextData)
    {
        _contextData = contextData;
        return this;
    }

    public ErrorDtoBuilder Resolved(string resolvedBy, string resolutionNotes)
    {
        _isResolved = true;
        _resolvedAt = DateTime.UtcNow;
        _resolvedBy = resolvedBy;
        _resolutionNotes = resolutionNotes;
        return this;
    }

    public ErrorDtoBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public ErrorDto Build()
    {
        return new ErrorDto
        {
            Id = _id,
            TenantId = _tenantId,
            Severity = _severity,
            Message = _message,
            StackTrace = _stackTrace,
            EntityType = _entityType,
            EntityId = _entityId,
            ContextData = _contextData,
            IsResolved = _isResolved,
            ResolvedAt = _resolvedAt,
            ResolvedBy = _resolvedBy,
            ResolutionNotes = _resolutionNotes,
            CreatedAt = _createdAt
        };
    }

    public static ErrorDtoBuilder Critical(string message) =>
        new ErrorDtoBuilder().WithSeverity(ErrorSeverity.Critical).WithMessage(message);

    public static ErrorDtoBuilder High(string message) =>
        new ErrorDtoBuilder().WithSeverity(ErrorSeverity.High).WithMessage(message);

    public static ErrorDtoBuilder Medium(string message) =>
        new ErrorDtoBuilder().WithSeverity(ErrorSeverity.Medium).WithMessage(message);

    public static ErrorDtoBuilder Low(string message) =>
        new ErrorDtoBuilder().WithSeverity(ErrorSeverity.Low).WithMessage(message);
}
