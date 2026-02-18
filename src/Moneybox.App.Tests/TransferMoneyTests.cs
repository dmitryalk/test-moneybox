using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Tests;

[TestClass]
public sealed class TransferMoneyTests
{
    private Mock<IAccountRepository> _mockAccountRepository;
    private Mock<INotificationService> _mockNotificationService;
    private TransferMoney _transferMoney;

    [TestInitialize]
    public void Setup()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _transferMoney = new TransferMoney(_mockAccountRepository.Object, _mockNotificationService.Object);
    }

    [TestMethod]
    public void Execute_SuccessfulTransfer_UpdatesBothAccounts()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 100m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "from@test.com", Name = "From User" }
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Balance = 500m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "to@test.com", Name = "To User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccountId)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccountId, toAccountId, amount);

        // Assert
        Assert.AreEqual(900m, fromAccount.Balance);
        Assert.AreEqual(-100m, fromAccount.Withdrawn);
        Assert.AreEqual(600m, toAccount.Balance);
        Assert.AreEqual(100m, toAccount.PaidIn);
        _mockAccountRepository.Verify(x => x.Update(fromAccount), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(toAccount), Times.Once);
    }

    [TestMethod]
    public void Execute_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 1500m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "from@test.com", Name = "From User" }
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Balance = 500m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "to@test.com", Name = "To User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccountId)).Returns(toAccount);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _transferMoney.Execute(fromAccountId, toAccountId, amount));

        Assert.AreEqual("Insufficient funds to make transfer", exception.Message);
    }

    [TestMethod]
    public void Execute_BalanceBelow500_SuccessAndNotifiesFundsLow()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 600m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "from@test.com", Name = "From User" }
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Balance = 500m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "to@test.com", Name = "To User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccountId)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccountId, toAccountId, amount);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyFundsLow("from@test.com"), Times.Once);

        Assert.AreEqual(400m, fromAccount.Balance);
        Assert.AreEqual(-600m, fromAccount.Withdrawn);
        Assert.AreEqual(1100m, toAccount.Balance);
        Assert.AreEqual(600m, toAccount.PaidIn);
    }

    [TestMethod]
    public void Execute_BalanceAbove500_DoesNotNotifyFundsLow()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 100m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "from@test.com", Name = "From User" }
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Balance = 500m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "to@test.com", Name = "To User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccountId)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccountId, toAccountId, amount);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void Execute_ExceedsPayInLimit_ThrowsException()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 500m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Balance = 5000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "from@test.com", Name = "From User" }
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 3600m, // Already paid in 3600, adding 500 would exceed 4000 limit
            User = new User { Id = Guid.NewGuid(), Email = "to@test.com", Name = "To User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccountId)).Returns(toAccount);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _transferMoney.Execute(fromAccountId, toAccountId, amount));
        Assert.AreEqual("Account pay in limit reached", exception.Message);
    }

    [TestMethod]
    public void Execute_ApproachingPayInLimit_SuccessAndNotifiesApproachingLimit()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 100m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Balance = 5000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "from@test.com", Name = "From User" }
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 3600m, // After adding 100, will be 3700, leaving 300 from limit
            User = new User { Id = Guid.NewGuid(), Email = "to@test.com", Name = "To User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccountId)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccountId, toAccountId, amount);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyApproachingPayInLimit("to@test.com"), Times.Once);
        Assert.AreEqual(4900m, fromAccount.Balance);
        Assert.AreEqual(-100m, fromAccount.Withdrawn);
        Assert.AreEqual(1100m, toAccount.Balance);
        Assert.AreEqual(3700m, toAccount.PaidIn);
    }

    [TestMethod]
    public void Execute_NotApproachingPayInLimit_DoesNotNotifyApproachingLimit()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 100m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Balance = 5000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "from@test.com", Name = "From User" }
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 1000m, // After adding 100, will be 1100, leaving 2900 from limit
            User = new User { Id = Guid.NewGuid(), Email = "to@test.com", Name = "To User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccountId)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccountId, toAccountId, amount);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void Execute_ZeroAmount_UpdatesAccountsWithoutNotifications()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 0m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Balance = 1000m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "from@test.com", Name = "From User" }
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Balance = 500m,
            Withdrawn = 0m,
            PaidIn = 0m,
            User = new User { Id = Guid.NewGuid(), Email = "to@test.com", Name = "To User" }
        };

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccountId)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccountId, toAccountId, amount);

        // Assert
        Assert.AreEqual(1000m, fromAccount.Balance);
        Assert.AreEqual(500m, toAccount.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
}
