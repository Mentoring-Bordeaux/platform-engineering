public class StatePulumi
{
    public enum Step
    {
        InputVerification,
        GitSessionSetup,
        GitRepositoryCreation,
        FrameworkOnGitRepositoryInitialization,
        PulumiTemplateRessourceCreation,
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
