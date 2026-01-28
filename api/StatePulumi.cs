public class StatePulumi
{
    public enum Step
    {
        InputVerification,
        GitRepositoryCreation,
        FrameworkOnGitRepositoryInitialization,
        PulumiTemplateResourceCreation,
        StackPulumiTransferOnGitRepository,
        Success,
    }

    public enum StateStatus
    {
        InProgress,
        Success,
        Failed,
    }

    public StateStatus Status { get; set; } = StateStatus.InProgress;
    public Step CurrentStep { get; set; } = Step.InputVerification;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object>? Outputs { get; set; }
}

public class StatePulumiForFrontend
{
    public string Status { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public int StepsCompleted { get; set; } = 0;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object>? Outputs { get; set; }
}
