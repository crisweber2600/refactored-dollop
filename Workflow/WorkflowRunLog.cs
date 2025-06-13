namespace refactored_dollop.Workflow;

public class WorkflowRunLog
{
    public int Id { get; set; }
    public Guid WorkflowRunId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public WorkflowRun? WorkflowRun { get; set; }
}
