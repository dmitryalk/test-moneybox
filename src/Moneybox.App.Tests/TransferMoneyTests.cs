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

    private User _fromUser;
    private User _toUser;

    public TransferMoneyTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _transferMoney = new TransferMoney(_mockAccountRepository.Object, _mockNotificationService.Object);
        _fromUser = User.Create("From User", "from@test.com");
        _toUser = User.Create("To User", "to@test.com");
    }

    [TestInitialize]
    public void Setup()
    {
        
    }

    [TestMethod]
    public void Execute_SuccessfulTransfer_UpdatesBothAccounts()
    {
        // Arrange
        var amount = 100m;

        var fromAccount = Account
            .Create(_fromUser)
            .SetBalance(1000m);       

        var toAccount = Account
            .Create(_toUser)
            .SetBalance(500m);      

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccount.Id, toAccount.Id, amount);

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
        var amount = 1500m;

        var fromAccount = Account
            .Create(_fromUser)
            .SetBalance(1000m);

        var toAccount = Account
            .Create(_toUser)
            .SetBalance(500m);

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _transferMoney.Execute(fromAccount.Id, toAccount.Id, amount));

        Assert.AreEqual(Account.Errors.InsufficientFunds.Message, exception.Message);
        _mockAccountRepository.Verify(x => x.Update(fromAccount), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(toAccount), Times.Never);
    }

    [TestMethod]
    public void Execute_BalanceBelow500_SuccessAndNotifiesFundsLow()
    {
        // Arrange
        var amount = 600m;

        var fromAccount = Account
            .Create(_fromUser)
            .SetBalance(1000m);

        var toAccount = Account
            .Create(_toUser)
            .SetBalance(500m);

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccount.Id, toAccount.Id, amount);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyFundsLow(_fromUser.Email), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(fromAccount), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(toAccount), Times.Once);

        Assert.AreEqual(400m, fromAccount.Balance);
        Assert.AreEqual(-600m, fromAccount.Withdrawn);
        Assert.AreEqual(1100m, toAccount.Balance);
        Assert.AreEqual(600m, toAccount.PaidIn);
    }

    [TestMethod]
    public void Execute_BalanceAbove500_DoesNotNotifyFundsLow()
    {
        // Arrange
        var amount = 100m;

        var fromAccount = Account
            .Create(_fromUser)
            .SetBalance(1000m);

        var toAccount = Account
            .Create(_toUser)
            .SetBalance(500m);

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccount.Id, toAccount.Id, amount);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(fromAccount), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(toAccount), Times.Once);
    }

    [TestMethod]
    public void Execute_ExceedsPayInLimit_ThrowsException()
    {
        // Arrange
        var amount = 500m;

        var fromAccount = Account
            .Create(_fromUser)
            .SetBalance(5000m);

        var toAccount = Account
           .Create(_toUser)
           .SetBalance(1000m, 0m, 3600m);

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _transferMoney.Execute(fromAccount.Id, toAccount.Id, amount));
        Assert.AreEqual(Account.Errors.PayInLimitReached.Message, exception.Message);

        _mockAccountRepository.Verify(x => x.Update(fromAccount), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(toAccount), Times.Never);
    }

    [TestMethod]
    public void Execute_ApproachingPayInLimit_SuccessAndNotifiesApproachingLimit()
    {
        // Arrange
        var amount = 100m;

        var fromAccount = Account
           .Create(_fromUser)
           .SetBalance(5000m);

        var toAccount = Account
           .Create(_toUser)
           .SetBalance(1000m, 0m, 3600m);

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccount.Id, toAccount.Id, amount);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyApproachingPayInLimit(_toUser.Email), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(fromAccount), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(toAccount), Times.Once);

        Assert.AreEqual(4900m, fromAccount.Balance);
        Assert.AreEqual(-100m, fromAccount.Withdrawn);
        Assert.AreEqual(1100m, toAccount.Balance);
        Assert.AreEqual(3700m, toAccount.PaidIn);
    }

    [TestMethod]
    public void Execute_NotApproachingPayInLimit_DoesNotNotifyApproachingLimit()
    {
        // Arrange
        var amount = 100m;

        var fromAccount = Account
          .Create(_fromUser)
          .SetBalance(5000m);

        var toAccount = Account
           .Create(_toUser)
           .SetBalance(1000m, 0m, 1000m);

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccount.Id, toAccount.Id, amount);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(fromAccount), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(toAccount), Times.Once);
    }

    [TestMethod]
    public void Execute_ZeroAmount_UpdatesAccountsWithoutNotifications()
    {
        // Arrange
        var amount = 0m;

        var fromAccount = Account
             .Create(_fromUser)
             .SetBalance(1000m);

        var toAccount = Account
            .Create(_toUser)
            .SetBalance(500m);

        _mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);
        _mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);

        // Act
        _transferMoney.Execute(fromAccount.Id, toAccount.Id, amount);

        // Assert
        Assert.AreEqual(1000m, fromAccount.Balance);
        Assert.AreEqual(500m, toAccount.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(x => x.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);

        _mockAccountRepository.Verify(x => x.Update(fromAccount), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(toAccount), Times.Once);
    }
}
