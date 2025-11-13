using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Tests.Blazor.TestDataBuilders;

/// <summary>
/// Builder for creating TicketUpdateDto instances for testing
/// </summary>
public class TicketUpdateDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _ticketId = Guid.NewGuid();
    private string _updatedTitle = "Updated Test Ticket";
    private string _updatedDescription = "Updated test ticket description";
    private List<SuccessCriterionDto> _successCriteria = new();
    private string _acceptanceCriteria = "Default acceptance criteria";
    private int _version = 1;
    private bool _isDraft = true;
    private bool _isApproved = false;
    private string? _rejectionReason = null;
    private DateTime _generatedAt = DateTime.UtcNow;
    private DateTime? _approvedAt = null;
    private DateTime? _postedAt = null;

    public TicketUpdateDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TicketUpdateDtoBuilder WithTicketId(Guid ticketId)
    {
        _ticketId = ticketId;
        return this;
    }

    public TicketUpdateDtoBuilder WithUpdatedTitle(string title)
    {
        _updatedTitle = title;
        return this;
    }

    public TicketUpdateDtoBuilder WithUpdatedDescription(string description)
    {
        _updatedDescription = description;
        return this;
    }

    public TicketUpdateDtoBuilder WithSuccessCriteria(List<SuccessCriterionDto> criteria)
    {
        _successCriteria = criteria;
        return this;
    }

    public TicketUpdateDtoBuilder AddSuccessCriterion(
        SuccessCriterionCategory category,
        string description,
        int priority = 0,
        bool isTestable = true)
    {
        _successCriteria.Add(new SuccessCriterionDto
        {
            Category = category,
            Description = description,
            Priority = priority,
            IsTestable = isTestable
        });
        return this;
    }

    public TicketUpdateDtoBuilder WithAcceptanceCriteria(string criteria)
    {
        _acceptanceCriteria = criteria;
        return this;
    }

    public TicketUpdateDtoBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public TicketUpdateDtoBuilder AsDraft()
    {
        _isDraft = true;
        _isApproved = false;
        return this;
    }

    public TicketUpdateDtoBuilder AsApproved()
    {
        _isDraft = false;
        _isApproved = true;
        _approvedAt = DateTime.UtcNow;
        return this;
    }

    public TicketUpdateDtoBuilder AsRejected(string reason)
    {
        _isDraft = false;
        _isApproved = false;
        _rejectionReason = reason;
        return this;
    }

    public TicketUpdateDtoBuilder WithGeneratedAt(DateTime generatedAt)
    {
        _generatedAt = generatedAt;
        return this;
    }

    public TicketUpdateDtoBuilder WithApprovedAt(DateTime? approvedAt)
    {
        _approvedAt = approvedAt;
        return this;
    }

    public TicketUpdateDtoBuilder WithPostedAt(DateTime? postedAt)
    {
        _postedAt = postedAt;
        return this;
    }

    public TicketUpdateDto Build()
    {
        return new TicketUpdateDto
        {
            Id = _id,
            TicketId = _ticketId,
            UpdatedTitle = _updatedTitle,
            UpdatedDescription = _updatedDescription,
            SuccessCriteria = _successCriteria,
            AcceptanceCriteria = _acceptanceCriteria,
            Version = _version,
            IsDraft = _isDraft,
            IsApproved = _isApproved,
            RejectionReason = _rejectionReason,
            GeneratedAt = _generatedAt,
            ApprovedAt = _approvedAt,
            PostedAt = _postedAt
        };
    }

    /// <summary>
    /// Creates a draft ticket update with sample success criteria
    /// </summary>
    public static TicketUpdateDtoBuilder WithSampleCriteria()
    {
        return new TicketUpdateDtoBuilder()
            .AddSuccessCriterion(
                SuccessCriterionCategory.Functional,
                "User can log in with email and password",
                priority: 0,
                isTestable: true)
            .AddSuccessCriterion(
                SuccessCriterionCategory.Performance,
                "Login response time < 2 seconds",
                priority: 1,
                isTestable: true)
            .AddSuccessCriterion(
                SuccessCriterionCategory.Security,
                "Password must be hashed with bcrypt",
                priority: 0,
                isTestable: true);
    }

    /// <summary>
    /// Creates an approved ticket update ready for posting
    /// </summary>
    public static TicketUpdateDtoBuilder ApprovedForPosting()
    {
        var builder = TicketUpdateDtoBuilder.WithSampleCriteria();
        builder.AsApproved();
        return builder;
    }
}
