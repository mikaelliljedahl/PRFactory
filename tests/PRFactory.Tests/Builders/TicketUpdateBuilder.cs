using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Tests.Builders;

/// <summary>
/// Fluent builder for creating TicketUpdate entities in tests with sensible defaults
/// </summary>
public class TicketUpdateBuilder
{
    private Guid _ticketId = Guid.NewGuid();
    private string _updatedTitle = "Updated Ticket Title";
    private string _updatedDescription = "Updated ticket description with more details";
    private List<SuccessCriterion> _successCriteria = new()
    {
        SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Default functional criterion", true)
    };
    private string _acceptanceCriteria = "- Default acceptance criterion 1\n- Default acceptance criterion 2";
    private int _version = 1;
    private bool _isApproved = false;
    private string? _rejectionReason;
    private DateTime? _postedAt;

    public TicketUpdateBuilder()
    {
    }

    public TicketUpdateBuilder ForTicket(Guid ticketId)
    {
        _ticketId = ticketId;
        return this;
    }

    public TicketUpdateBuilder WithUpdatedTitle(string title)
    {
        _updatedTitle = title;
        return this;
    }

    public TicketUpdateBuilder WithUpdatedDescription(string description)
    {
        _updatedDescription = description;
        return this;
    }

    public TicketUpdateBuilder WithSuccessCriteria(params SuccessCriterion[] criteria)
    {
        _successCriteria = criteria.ToList();
        return this;
    }

    public TicketUpdateBuilder AddSuccessCriterion(SuccessCriterion criterion)
    {
        _successCriteria.Add(criterion);
        return this;
    }

    public TicketUpdateBuilder AddMustHaveCriterion(string description, SuccessCriterionCategory category = SuccessCriterionCategory.Functional)
    {
        _successCriteria.Add(SuccessCriterion.CreateMustHave(category, description));
        return this;
    }

    public TicketUpdateBuilder AddShouldHaveCriterion(string description, SuccessCriterionCategory category = SuccessCriterionCategory.Functional)
    {
        _successCriteria.Add(SuccessCriterion.CreateShouldHave(category, description));
        return this;
    }

    public TicketUpdateBuilder WithAcceptanceCriteria(string criteria)
    {
        _acceptanceCriteria = criteria;
        return this;
    }

    public TicketUpdateBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public TicketUpdateBuilder AsDraft()
    {
        _isApproved = false;
        _postedAt = null;
        return this;
    }

    public TicketUpdateBuilder AsApproved()
    {
        _isApproved = true;
        return this;
    }

    public TicketUpdateBuilder AsPosted()
    {
        _isApproved = true;
        _postedAt = DateTime.UtcNow;
        return this;
    }

    public TicketUpdateBuilder AsRejected(string reason)
    {
        _rejectionReason = reason;
        return this;
    }

    public TicketUpdate Build()
    {
        var ticketUpdate = TicketUpdate.Create(
            _ticketId,
            _updatedTitle,
            _updatedDescription,
            _successCriteria,
            _acceptanceCriteria,
            _version);

        if (_isApproved)
        {
            ticketUpdate.Approve();
        }

        if (!string.IsNullOrEmpty(_rejectionReason))
        {
            ticketUpdate.Reject(_rejectionReason);
        }

        if (_postedAt.HasValue && _isApproved)
        {
            ticketUpdate.MarkAsPosted();
        }

        return ticketUpdate;
    }
}
