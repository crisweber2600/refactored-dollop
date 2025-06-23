using ExampleData;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using System.Collections.Generic;

namespace ExampleLib.BDDTests;

public class Foo {}
public class Bar {}
public class Car {}

public class Composite
{
    public Foo Foo { get; set; } = new();
    public Bar Bar { get; set; } = new();
    public Car Car { get; set; } = new();
}

[Binding]
public class ValidationPlanFactorySteps
{
    private IReadOnlyList<ValidationPlan>? _plans;

    [When("creating validation plans for a composite type")]
    public void WhenCreatingPlans()
    {
        _plans = ValidationPlanFactory.CreatePlans<Composite, YourDbContext>("DataSource=:memory:");
    }

    [Then("(\\d+) validation plans should be created")]
    public void ThenPlanCount(int count)
    {
        if (_plans == null || _plans.Count != count)
            throw new Exception($"Expected {count} plans but was {_plans?.Count}");
    }

    [Then("each plan should use Count strategy")]
    public void ThenPlansUseCount()
    {
        if (_plans == null) throw new Exception("Plans were not created");
        foreach (var plan in _plans)
        {
            if (plan.Strategy != ValidationStrategy.Count)
                throw new Exception("Plan did not use Count strategy");
        }
    }
}
