public class CreateRepoRequest
{
    // Jeton d'accès personnel de l'utilisateur
    public required string Pat { get; set; }
    // Nom souhaité pour le nouveau dépôt
    public required string RepoName { get; set; }
    // Description optionnelle
    public string? Description { get; set; }
    // Optionnel : si le dépôt doit être privé. Par défaut à true.
    public bool Private { get; set; } = true;
}

