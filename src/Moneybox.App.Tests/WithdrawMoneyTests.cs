using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Tests;

[TestClass]
public sealed class WithdrawMoneyTests
{
    private Mock<IAccountRepository> _mockAccountRepository;
    private Mock<INotificationService> _mockNotificationService;
    private WithdrawMoney _withdrawMoney;

    [TestInitialize]
    public void Setup()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _withdrawMoney = new WithdrawMoney(_mockAccountRepository.Object, _mockNotificationService.Object);
    }

    [TestMethod]
    public void Execute_SuccessfulWithdrawal_UpdatesAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 100m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = -200m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act
        _withdrawMoney.Execute(accountId, amount);

        // Assert
        Assert.AreEqual(900m, account.Balance);
        Assert.AreEqual(-300m, account.Withdrawn);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 1500m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _withdrawMoney.Execute(accountId, amount));
        Assert.AreEqual("Insufficient funds to make withdrawal", exception.Message);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Never);
    }

    [TestMethod]
    public void Execute_BalanceBelow500AfterWithdrawal_NotifiesFundsLow()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 600m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act
        _withdrawMoney.Execute(accountId, amount);

        // Assert
        Assert.AreEqual(400m, account.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow("test@test.com"), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_BalanceAbove500AfterWithdrawal_DoesNotNotifyFundsLow()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 100m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act
        _withdrawMoney.Execute(accountId, amount);

        // Assert
        Assert.AreEqual(900m, account.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_BalanceExactly500AfterWithdrawal_DoesNotNotifyFundsLow()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 500m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act
        _withdrawMoney.Execute(accountId, amount);

        // Assert
        Assert.AreEqual(500m, account.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_BalanceExactly499AfterWithdrawal_NotifiesFundsLow()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 501m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act
        _withdrawMoney.Execute(accountId, amount);

        // Assert
        Assert.AreEqual(499m, account.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow("test@test.com"), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_WithdrawEntireBalance_UpdatesAccountAndNotifies()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 1000m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act
        _withdrawMoney.Execute(accountId, amount);

        // Assert
        Assert.AreEqual(0m, account.Balance);
        Assert.AreEqual(-1000m, account.Withdrawn);
        _mockNotificationService.Verify(x => x.NotifyFundsLow("test@test.com"), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_ZeroAmount_UpdatesAccountWithoutNotifications()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 0m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = -100m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act
        _withdrawMoney.Execute(accountId, amount);

        // Assert
        Assert.AreEqual(1000m, account.Balance);
        Assert.AreEqual(-100m, account.Withdrawn);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_WithdrawalResultingInNegativeBalance_ThrowsException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 1000.01m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _withdrawMoney.Execute(accountId, amount));
        Assert.AreEqual("Insufficient funds to make withdrawal", exception.Message);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Never);
    }

    [TestMethod]
    public void Execute_AccountAlreadyHasWithdrawals_AccumulatesWithdrawnAmount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 200m;

        var account = new Account
        {
            Id = accountId,
            Balance = 1000m,
            Withdrawn = -300m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Test User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act
        _withdrawMoney.Execute(accountId, amount);

        // Assert
        Assert.AreEqual(800m, account.Balance);
        Assert.AreEqual(-500m, account.Withdrawn);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }
}
