using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGitRepositoryService
{
    Task InitializeRepoWithFrameworksAsync(List<FrameworkType> frameworks, string projectName);

    Task DeleteRepositoryAsync();
}
