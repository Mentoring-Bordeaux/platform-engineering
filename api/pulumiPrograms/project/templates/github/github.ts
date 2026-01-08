import * as pulumi from "@pulumi/pulumi";
import * as github from "@pulumi/github";

/**
 * Crée un dépôt GitHub en utilisant le token et l'organisation configurés.
 * @param name Le nom du dépôt
 * @param isPrivate True si le dépôt doit être privé
 * @param description Description du dépôt (optionnel)
 * @param orgName Nom de l'organisation GitHub (optionnel)
 * @param githubToken Token GitHub PAT (obligatoire)
 * @returns L'objet Repository et l'URL du dépôt
 */
export function createGithubRepo(
    name: string,
    isPrivate: boolean,
    description: string = "Managed by Pulumi Automation API.",
    orgName?: string,
    githubToken?: string
) {
    if (!githubToken) {
        throw new Error("GitHubToken is required to create a repository");
    }
    const githubProvider = new github.Provider("github-provider", {
        token: githubToken,
        owner: orgName, 
    });

    const repository = new github.Repository(name, {
        name: name,
        description: description,
        visibility: isPrivate ? "private" : "public",
        hasIssues: true,
        hasProjects: true,
        hasWiki: false,
        autoInit: true,
    }, {
        provider: githubProvider,
        retainOnDelete: true, 
    });

 
    return {
        repo: repository,
        repoNameOutput: repository.name,
        repoUrl: repository.htmlUrl,
    };
}
