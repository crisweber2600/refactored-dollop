using System;
using System.Linq;
using FluentAssertions;
using MetricsPipeline.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "RevertInMemoryDatabase")]
public class RevertInMemorySteps
{
    private readonly SummaryDbContext _db;

    public RevertInMemorySteps(SummaryDbContext db)
    {
        _db = db;
    }

    [Given("a new in-memory SummaryDbContext")]
    public void GivenContext()
    {
        _db.Database.EnsureCreated();
    }

    [When("the context is inspected")]
    public void WhenInspected()
    {
        // no-op, inspection occurs in assertions
    }

    [Then("migrations should not run")]
    public void ThenMigrationsSkipped()
    {
        _db.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.InMemory");
    }

    [Then("the database is empty")]
    public void ThenSeededExists()
    {
        _db.Summaries.Count().Should().Be(0);
    }
}
