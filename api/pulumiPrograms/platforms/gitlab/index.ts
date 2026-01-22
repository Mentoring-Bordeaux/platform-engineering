import * as pulumi from "@pulumi/pulumi";
import * as gitlab from "@pulumi/gitlab";

const config = new pulumi.Config();

// Required config
const gitlabToken = config.require("gitlabToken");
const projectName = config.require("Name");

// Optional config
const description = config.get("description") ?? "Managed by Pulumi Automation API.";
const isPrivate = config.getBoolean("isPrivate") ?? true;

// Create under a specific group/namespace if provided (numeric ID).
const namespaceId = config.getNumber("namespaceId");

// Optional for self-hosted GitLab. Example: https://gitlab.example.com/api/v4
const baseUrl = config.get("gitlabBaseUrl");

const providerArgs: gitlab.ProviderArgs = {
  token: gitlabToken,
  ...(baseUrl ? { baseUrl } : {}),
};

const provider = new gitlab.Provider("gitlab-provider", providerArgs);

const project = new gitlab.Project(
  projectName,
  {
    name: projectName,
    description,
    visibilityLevel: isPrivate ? "private" : "public",
    initializeWithReadme: true,
    ...(namespaceId !== undefined ? { namespaceId } : {}),
  },
  {
    provider,
    retainOnDelete: true,
  }
);

export const repoNameOutput = project.name;
export const repoUrl = project.webUrl;
