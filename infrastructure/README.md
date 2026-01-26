# 🚀 Quick Start

This project deploys the infrastructure using **Pulumi (TypeScript)**.

## Setup

```bash
pnpm install
```
## Configuration
```bash
pulumi config set --secret PULUMI_ACCESS_TOKEN <your-pulumi-access-token>
pulumi config set --secret GithubToken <your-github-token>
pulumi config set --secret GitLabToken <your-gitlab-token>
pulumi config set --secret GitHubOrganizationName <your-github-org>
pulumi config set --secret GitLabBaseUrl <your-gitlab-url>
```

## Deploy

```bash
pulumi up
```

## Clean up

```bash
pulumi destroy
```
