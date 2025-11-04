# PRFactory.Domain

This is the Domain layer for PRFactory, containing the core business entities, value objects, and repository interfaces following Clean Architecture principles.

## Structure

```
PRFactory.Domain/
├── Entities/              # Domain entities
│   ├── Ticket.cs         # Main aggregate root
│   ├── Repository.cs     # Repository entity
│   ├── Tenant.cs         # Tenant entity
│   └── WorkflowEvent.cs  # Base event class and derived types
├── ValueObjects/          # Value objects
│   ├── WorkflowState.cs  # Workflow state enum
│   ├── Question.cs       # Question value object
│   ├── Answer.cs         # Answer value object
│   └── WorkflowStateTransitions.cs  # State transition logic
├── Interfaces/            # Repository interfaces
│   ├── ITicketRepository.cs
│   ├── IRepositoryRepository.cs
│   └── ITenantRepository.cs
└── PRFactory.Domain.csproj

```

## Key Features

### Entities

- **Ticket** - Main aggregate root with:
  - Full workflow state management
  - Question/Answer tracking
  - Plan and PR information
  - Rich domain methods (TransitionTo, AddQuestion, AddAnswer, etc.)
  - Domain events

- **Repository** - Git repository entity with platform support for GitHub, Bitbucket, and Azure DevOps

- **Tenant** - Multi-tenant support with encrypted credentials and configuration

- **WorkflowEvent** - Base event class with derived types:
  - WorkflowStateChanged
  - QuestionAdded
  - AnswerAdded
  - PlanCreated
  - PullRequestCreated

### Value Objects

- **WorkflowState** - Enum with all workflow states from Triggered to Completed/Failed
- **Question** - Clarifying questions with category support
- **Answer** - Developer-provided answers
- **WorkflowStateTransitions** - Centralized state transition validation

### Design Principles

- **Encapsulation**: Private setters, factory methods
- **Immutability**: Value objects are immutable
- **Domain Events**: Events raised for important state changes
- **Validation**: All inputs validated in constructors and methods
- **Nullable Reference Types**: Full C# 12 nullable support
- **No External Dependencies**: Pure domain layer, no infrastructure concerns

## Technology

- .NET 8
- C# 12 with latest language features
- Nullable reference types enabled
- Implicit usings enabled
