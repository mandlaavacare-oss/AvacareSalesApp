using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Server.Common.Exceptions;
using Server.Infrastructure.Database;

namespace Server.Tests.Infrastructure.Database;

public class DatabaseContextTests
{
    private static DatabaseContext CreateContext(
        Mock<ISageSession> sessionMock,
        Mock<ISageSessionFactory>? factoryMock = null,
        SageSessionOptions? options = null,
        Mock<ILogger<DatabaseContext>>? loggerMock = null)
    {
        factoryMock ??= new Mock<ISageSessionFactory>();
        factoryMock.Setup(f => f.CreateSession()).Returns(sessionMock.Object);

        options ??= new SageSessionOptions { BranchCode = "MAIN" };
        loggerMock ??= new Mock<ILogger<DatabaseContext>>();

        return new DatabaseContext(
            loggerMock.Object,
            factoryMock.Object,
            Options.Create(options));
    }

    [Fact]
    public void BeginTran_WhenSessionNotConnected_ConnectsSetsBranchAndStartsTransaction()
    {
        var sessionMock = new Mock<ISageSession>();
        var transactionMock = new Mock<ISageTransaction>();
        var isConnected = false;
        sessionMock.Setup(s => s.IsConnected).Returns(() => isConnected);
        sessionMock.Setup(s => s.Connect()).Callback(() => isConnected = true);
        sessionMock.Setup(s => s.SetBranch("MAIN"));
        sessionMock.Setup(s => s.BeginTransaction()).Returns(transactionMock.Object);

        var context = CreateContext(sessionMock);

        context.BeginTran();

        sessionMock.Verify(s => s.Connect(), Times.Once);
        sessionMock.Verify(s => s.SetBranch("MAIN"), Times.Once);
        sessionMock.Verify(s => s.BeginTransaction(), Times.Once);
    }

    [Fact]
    public void BeginTran_WhenSessionAlreadyConnected_DoesNotReconnect()
    {
        var sessionMock = new Mock<ISageSession>();
        var transactionMock = new Mock<ISageTransaction>();
        sessionMock.Setup(s => s.IsConnected).Returns(true);
        sessionMock.Setup(s => s.BeginTransaction()).Returns(transactionMock.Object);

        var context = CreateContext(sessionMock);

        context.BeginTran();

        sessionMock.Verify(s => s.Connect(), Times.Never);
    }

    [Fact]
    public void CommitTran_CommitsAndDisposesTransaction()
    {
        var sessionMock = new Mock<ISageSession>();
        var transactionMock = new Mock<ISageTransaction>();
        sessionMock.Setup(s => s.IsConnected).Returns(false);
        sessionMock.Setup(s => s.Connect());
        sessionMock.Setup(s => s.SetBranch(It.IsAny<string>()));
        sessionMock.Setup(s => s.BeginTransaction()).Returns(transactionMock.Object);

        var context = CreateContext(sessionMock);
        context.BeginTran();

        context.CommitTran();

        transactionMock.Verify(t => t.Commit(), Times.Once);
        transactionMock.Verify(t => t.Dispose(), Times.Once);
        sessionMock.Verify(s => s.Disconnect(), Times.Once);
        sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void CommitTran_WhenUnderlyingCommitFails_ThrowsDomainException()
    {
        var sessionMock = new Mock<ISageSession>();
        var transactionMock = new Mock<ISageTransaction>();
        sessionMock.Setup(s => s.IsConnected).Returns(false);
        sessionMock.Setup(s => s.Connect());
        sessionMock.Setup(s => s.SetBranch(It.IsAny<string>()));
        sessionMock.Setup(s => s.BeginTransaction()).Returns(transactionMock.Object);
        transactionMock.Setup(t => t.Commit()).Throws(new InvalidOperationException("fail"));

        var context = CreateContext(sessionMock);
        context.BeginTran();

        Action act = () => context.CommitTran();

        act.Should().Throw<DomainException>().WithMessage("Unable to commit the Sage SDK transaction.*");
        transactionMock.Verify(t => t.Dispose(), Times.Once);
        sessionMock.Verify(s => s.Disconnect(), Times.Once);
    }

    [Fact]
    public void RollbackTran_RollsBackAndDisposesTransaction()
    {
        var sessionMock = new Mock<ISageSession>();
        var transactionMock = new Mock<ISageTransaction>();
        sessionMock.Setup(s => s.IsConnected).Returns(false);
        sessionMock.Setup(s => s.Connect());
        sessionMock.Setup(s => s.SetBranch(It.IsAny<string>()));
        sessionMock.Setup(s => s.BeginTransaction()).Returns(transactionMock.Object);

        var context = CreateContext(sessionMock);
        context.BeginTran();

        context.RollbackTran();

        transactionMock.Verify(t => t.Rollback(), Times.Once);
        transactionMock.Verify(t => t.Dispose(), Times.Once);
        sessionMock.Verify(s => s.Disconnect(), Times.Once);
        sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void RollbackTran_WhenUnderlyingRollbackFails_ThrowsDomainException()
    {
        var sessionMock = new Mock<ISageSession>();
        var transactionMock = new Mock<ISageTransaction>();
        sessionMock.Setup(s => s.IsConnected).Returns(false);
        sessionMock.Setup(s => s.Connect());
        sessionMock.Setup(s => s.SetBranch(It.IsAny<string>()));
        sessionMock.Setup(s => s.BeginTransaction()).Returns(transactionMock.Object);
        transactionMock.Setup(t => t.Rollback()).Throws(new InvalidOperationException("fail"));

        var context = CreateContext(sessionMock);
        context.BeginTran();

        Action act = () => context.RollbackTran();

        act.Should().Throw<DomainException>().WithMessage("Unable to roll back the Sage SDK transaction.*");
        transactionMock.Verify(t => t.Dispose(), Times.Once);
        sessionMock.Verify(s => s.Disconnect(), Times.Once);
    }

    [Fact]
    public void BeginTran_WhenFactoryThrows_WrapsInDomainException()
    {
        var factoryMock = new Mock<ISageSessionFactory>();
        factoryMock.Setup(f => f.CreateSession()).Throws(new InvalidOperationException("factory"));
        var logger = new Mock<ILogger<DatabaseContext>>();
        var context = new DatabaseContext(
            logger.Object,
            factoryMock.Object,
            Options.Create(new SageSessionOptions()));

        Action act = () => context.BeginTran();

        act.Should().Throw<DomainException>().WithMessage("Unable to begin a Sage SDK transaction.*");
    }
}
