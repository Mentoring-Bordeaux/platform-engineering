public class RequestReceivedResponse
{
    public string ProjectName { get; set; } = string.Empty;
    public int IdProjectCreation { get; set; } = 0;

    public RequestReceivedResponse(string projectName, int idProjectCreation)
    {
        ProjectName = projectName;
        IdProjectCreation = idProjectCreation;
    }
}
