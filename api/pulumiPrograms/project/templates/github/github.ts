import * as pulumi from "@pulumi/pulumi";
import * as github from "@pulumi/github";

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
        autoInit: false,
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
