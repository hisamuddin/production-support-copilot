using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Infrastructure.Persistence;
using ProductionSupportCopilot.Infrastructure.Retrieval;

namespace ProductionSupportCopilot.UnitTests.Retrieval;

public sealed class PostgresDocumentSearchServiceTests
{
    private readonly PostgresDocumentSearchService _sut;

    public PostgresDocumentSearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<SupportCopilotDbContext>().Options;
        _sut = new PostgresDocumentSearchService(new SupportCopilotDbContext(options));
    }

    [Fact]
    public async Task SearchAsync_WithEmptyTenant_RejectsRequestBeforeDatabaseAccess()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SearchAsync(Guid.Empty, "consumer lag"));
    }

    [Fact]
    public async Task SearchAsync_WithBlankQuery_RejectsRequestBeforeDatabaseAccess()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SearchAsync(Guid.NewGuid(), "  "));
    }

    [Fact]
    public async Task SearchAsync_WithExcessiveLimit_RejectsRequestBeforeDatabaseAccess()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.SearchAsync(Guid.NewGuid(), "consumer lag", 21));
    }
}
