import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";

import { createStaticWebApp } from "./templates/static-web-app/static-web-app";
import { createGithubRepo } from "./templates/github/github";
import { createAppService } from "./templates/app-service/app-service";
import { createReactRepoInitial, deployStaticWebAppFromRepo } from "./templates/react-ts-app/react-ts-app";

const config = new pulumi.Config();
const projectName = config.require("projectName");
const resourcesConfig = config.requireObject<any[]>("resources");


const rg = new resources.ResourceGroup(`${projectName}-rg`, { location: "francecentral" });

const githubRepos: Record<string, pulumi.Output<string>> = {};

for (const res of resourcesConfig) {
    if (res.resourceType.toLowerCase() === "github") {
        const repo = createGithubRepo(
            res.name,
            res.parameters?.isPrivate === "true",
            res.parameters?.description,
            res.parameters?.githubOrg,
            res.parameters?.githubToken
        );
        githubRepos[res.name] = repo.repoUrl;
    }
}


const exportedUrls: Record<string, pulumi.Output<string>> = {};

for (const res of resourcesConfig) {
    const type = res.resourceType.toLowerCase();
    const name = res.name;
    const params = res.parameters ?? {};

    switch (type) {
        case "static-web-app": {
            const repoName = params.githubRepoName;
            const repoOutput = repoName ? githubRepos[repoName] : undefined;

            if (!repoOutput) break;

            const swa = repoOutput.apply(repoUrl =>
                createStaticWebApp(
                    name,
                    rg.name,
                    params.location ?? "westeurope",
                    repoUrl,
                    params.branch ?? "main"
                )
            );

            exportedUrls[`${name}-url`] = swa.apply(s => s.url);
            break;
        }

        case "app-service": {
            const app = createAppService(name, rg.name, params);
            exportedUrls[`${name}-url`] = app.url;
            break;
        }

        case "react-ts-app": {
            const repoName = params.githubRepoName;
            console.log("Repo Name:", repoName);
            const repoOutput = repoName ? githubRepos[repoName] : undefined;
            console.log("Repo Output:", repoOutput);
            if (!repoOutput) break;

            const reactAppUrl = repoOutput.apply(repoUrl => {
                createReactRepoInitial(repoUrl, ".react-temp", name);
                console.log("Repo URL:", repoUrl);
                const reactApp = deployStaticWebAppFromRepo(
                    name,
                    rg.name,
                    params.location ?? "westeurope",
                    repoUrl,
                    params.branch ?? "main"
                );
                console.log("React App URL:", reactApp.url);
                return reactApp.url;
            });

            exportedUrls[`${name}-url`] = reactAppUrl;
            break;
        }
    }
}

for (const [key, value] of Object.entries(exportedUrls)) {
    (global as any)[key] = value;
}
