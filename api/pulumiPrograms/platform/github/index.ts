import * as pulumi from "@pulumi/pulumi";
import * as github from "@pulumi/github";

const config = new pulumi.Config();


// Receive configuration values from the Pulumi stack configuration.
const githubToken = config.require("githubToken");
const repoName = config.require("Name");
const description = config.get("description") || "Managed by Pulumi Automation API.";
const isPrivate = config.requireBoolean("isPrivate");
const orgName = config.get("orgName"); // Optional: organization name

// Log the received configuration for debugging purposes.
let githubProvider;

if (orgName) {
    // Configure the GitHub provider with the provided PAT.
    githubProvider = new github.Provider("github-provider", {
        token: githubToken,
        owner: orgName,
    });
}
else {
    // Configure the GitHub provider with the provided PAT.
    githubProvider = new github.Provider("github-provider", {
        token: githubToken,
    });
}

// Create the GitHub Repository resource.
const repository = new github.Repository(repoName, {
    name: repoName,
    description: description,
    // The Pulumi GitHub provider uses `visibility` to set access.
    visibility: isPrivate ? "private" : "public",
    // Set some sensible defaults for the new repo
    hasIssues: true,
    hasProjects: true,
    hasWiki: false, 
    autoInit: true, // Initialize with a README
}, { provider: githubProvider,
    retainOnDelete: true // Retain the repository on deletion of the Pulumi stack

});

// Export the repository details required by the C# application's `result.Outputs`.
// These names must match the outputs expected in your C# code: `repoName` and `repoUrl`.
export const repoNameOutput = repository.name;
export const repoUrl = repository.htmlUrl; // The URL to view the repository on GitHub
