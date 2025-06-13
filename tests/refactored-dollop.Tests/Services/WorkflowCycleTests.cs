using Microsoft.EntityFrameworkCore;
using refactored_dollop.Data;
using refactored_dollop.Repositories;
using refactored_dollop.Services;
using refactored_dollop.Workflow;
using Xunit;
using System.Threading.Tasks;

public class WorkflowCycleTests
{
    [Fact]
    public async Task FullCycle_ShouldReachCommitted()
    {
        var options = new DbContextOptionsBuilder<WorkflowContext>()
            .UseInMemoryDatabase("workflow_db")
            .Options;
        var context = new WorkflowContext(options);

        var workflowRepo = new WorkflowRepository(context);
        var uow = new UnitOfWork(context, workflowRepo);

        var start = new StartWorkflowService(uow);
        var validate = new ValidateWorkflowService(uow);
        var commit = new CommitWorkflowService(uow);
        var revert = new RevertWorkflowService(uow);

        var run = await start.StartAsync();
        await validate.ValidateAsync(run.Id);
        await commit.CommitAsync(run.Id);

        var committed = await workflowRepo.GetAsync(run.Id);
        Assert.Equal(WorkflowStatus.Committed, committed!.Status);

        await revert.RevertAsync(run.Id);
        var reverted = await workflowRepo.GetAsync(run.Id);
        Assert.Equal(WorkflowStatus.Reverted, reverted!.Status);
    }
}
