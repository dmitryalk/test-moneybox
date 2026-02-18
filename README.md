# Moneybox Application

A .NET 8 application demonstrating domain-driven design principles and Test-Driven Development (TDD)
Github copilot was used to improve productivity:
- to generate Readme.md
- to stub initial unit tests

## Work Completed

### 1. Created Unit Tests for existing TransferMoney Service
Solidified business rules by creating comprehensive unit tests for the `TransferMoney` service. The test suite validates:
- Successful transfers between accounts
- Insufficient funds handling
- Low balance notifications (below £500)
- Pay-in limit enforcement (£4000 limit)
- Approaching pay-in limit notifications

**Test Coverage:**
- `TransferMoneyTests.cs` - 9 comprehensive test cases covering all business scenarios

### 2. Utilized TDD to Enforce WithdrawMoney Business Rules
Following Test-Driven Development principles, created unit tests first to define expected behavior before implementation:
- Wrote failing tests defining withdrawal business rules
- Ensured consistency with `TransferMoney` logic
- Validated low funds notifications
- Verified insufficient funds exceptions

**Test Coverage:**
- `WithdrawMoneyTests.cs` - 10 test cases including boundary conditions and edge cases

### 3. Implemented WithdrawMoney Service
Implemented the `WithdrawMoney.Execute()` method to satisfy the test requirements:
- Withdrawal validation
- Balance updates
- Low funds notifications
- Exception handling for insufficient funds

### 4. Refactored Services and Moved Domain Rules to Domain Models
Refactored both `WithdrawMoney` and `TransferMoney` services by extracting business logic into the domain model, following Domain-Driven Design principles:

**Before:** Business logic scattered in service layer  
**After:** Domain logic encapsulated in `Account` entity

#### Refactored TransferMoney Service

The `TransferMoney.Execute()` method now delegates business logic to domain methods:

```csharp
from.Withdraw(amount);
to.Deposit(amount);
```

This approach:
- Encapsulates business rules in the domain model
- Makes the service layer thin and focused on orchestration
- Improves testability and maintainability
- Follows Single Responsibility Principle

#### Account Domain Model - Deposit Method

The `Account.Deposit()` method encapsulates deposit business rules:

```csharp
public Account Deposit(decimal amount)
{
    if (PaidIn + amount > PayInLimit)
    {
        throw Errors.PayInLimitReached;
    }
    Balance += amount;
    PaidIn += amount;
    return this;
}
```

**Key Features:**
- Validates pay-in limit before accepting deposit
- Updates both balance and paid-in tracking
- Throws domain-specific exception on limit breach
- Returns `this` for method chaining (fluent interface)


#### Fluent Interface Pattern

The `Account` domain model implements a fluent interface, allowing for clean and readable object creation and configuration:

```csharp
// From WithdrawMoneyTests.cs - lines 36-38
var account = Account
    .Create(_user)
    .SetBalance(1000m, -200m, 0m);
```

**Benefits:**
- **Readable:** Code reads like natural language
- **Chainable:** Multiple operations can be chained together
- **Immutable-friendly:** Each method returns the modified object
- **Type-safe:** Compile-time checking of method calls

#### Account Domain Model - Errors Class

Centralized error handling using a static nested class:

```csharp
public static class Errors
{
    public static InvalidOperationException InsufficientFunds => 
        new InvalidOperationException("Insufficient funds to make transfer");
    
    public static InvalidOperationException PayInLimitReached => 
        new InvalidOperationException("Account pay in limit reached");
}
```

**Benefits:**
- Consistent error messages across the application
- Single source of truth for domain exceptions
- Easy to maintain and update error messages
- Type-safe error handling


This pattern is extensively used in tests to create account objects with specific states, making test setup clear and concise.

## Architecture

### Domain-Driven Design Principles
- **Entities:** `Account`, `User` - Core domain objects with identity
- **Value Objects:** Business constants (`PayInLimit`, `LowFunds`)
- **Domain Services:** `TransferMoney`, `WithdrawMoney` - Orchestrate operations
- **Repository Pattern:** `IAccountRepository` - Abstract data access
- **Domain Events:** `INotificationService` - Notification infrastructure

### Business Rules
- **Pay-in Limit:** £4,000 per account
- **Low Funds Threshold:** £500
- **Withdrawal Validation:** Must have sufficient funds
- **Transfer Validation:** Both withdrawal and deposit rules apply

## Technology Stack
- **.NET 8** - Target framework
- **C# 12.0** - Language version
- **MSTest** - Testing framework
- **Moq** - Mocking library for unit tests

## Project Structure
```
Moneybox.App/
├── DataAccess/
│   └── IAccountRepository.cs
├── Domain/
│   ├── Account.cs
│   ├── User.cs
│   └── Services/
│       └── INotificationService.cs
└── Features/
    ├── TransferMoney.cs
    └── WithdrawMoney.cs

Moneybox.App.Tests/
├── TransferMoneyTests.cs
└── WithdrawMoneyTests.cs
```


## Key Learnings

1. **TDD Benefits:**
   - Tests define behavior before implementation
   - Ensures comprehensive coverage
   - Drives better design decisions

2. **Domain-Driven Design:**
   - Rich domain models with business logic
   - Thin service layer for orchestration
   - Clear separation of concerns

3. **Refactoring Impact:**
   - Moved from anemic domain model to rich domain model
   - Improved code readability and maintainability
   - Enhanced testability through encapsulation

## Future Enhancements
- Add integration tests with real database
- Implement transaction support for atomicity
- Add audit logging for all operations
- Implement event sourcing for transaction history
- Add API layer for external access
