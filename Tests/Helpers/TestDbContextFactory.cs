using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ParrotsAPI2;
using ParrotsAPI2.Data;

namespace parrotsAPI2.Tests.Helpers;

public static class TestDbContextFactory
{
    public static DataContext Create()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new DataContext(options);
    }

    public static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }
}
