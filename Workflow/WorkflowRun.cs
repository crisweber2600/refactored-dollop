namespace refactored_dollop.Workflow;

public class WorkflowRun
{
    public Guid Id { get; set; }
    public WorkflowStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<WorkflowRunLog> Logs { get; set; } = new List<WorkflowRunLog>();
}

public enum WorkflowStatus
{
    Started,
    Validated,
    Committed,
    Reverted
}
