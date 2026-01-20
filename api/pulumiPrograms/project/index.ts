import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as os from "os";
import { createStaticWebApp } from "./templates/static-web-app/static-web-app";
import { createGithubRepo } from "./templates/github/github";
import { createAppService } from "./templates/app-service/app-service";
import { createDotNetApiTemplate } from "./templates/dotnet-api/dotnet-api";
import { createReactStaticWebApp } from "./templates/react-ts-app/react-ts-app";

const config = new pulumi.Config();
const projectName = config.require("projectName");
const resourcesConfig = config.requireObject<any[]>("resources");

const rg = new resources.ResourceGroup(`rg-${projectName}`, {
  location: "westeurope",
});

const TEMP_DIR = os.tmpdir();
const exportedUrls: Record<string, pulumi.Output<string>> = {};

for (const res of resourcesConfig) {
  const type = res.resourceType.toLowerCase();
  const name = res.name;
  const params = res.parameters ?? {};

  switch (type) {
    case "github": {
      const githubRepo = createGithubRepo(
        params.repoName ?? name,
        params?.isPrivate === "true",
        params?.description,
        params?.githubOrg,
        params?.githubToken
      );
      exportedUrls[`${params.repoName ?? name}-repo-url`] = githubRepo.repoUrl;
      break;
    }

    case "static-web-app": {
      const githubRepoUrl =
        exportedUrls[`${params.repoName}-repo-url`] ?? params.repoUrl;
      if (!params.githubToken && !githubRepoUrl) {
        throw new Error(
          "Pour Static Web App, il faut repoUrl ou une ressource GitHub (repoName)."
        );
      }
      const swa = createStaticWebApp(
        name,
        rg.name,
        params.location ?? "westeurope",
        githubRepoUrl ?? "",
        params.branch ?? "main"
      );
      exportedUrls[`${name}-url`] = swa.url;
      break;
    }

    case "app-service": {
      const app = createAppService(name, rg.name, params);
      exportedUrls[`${name}-url`] = app.url;
      break;
    }

    case "react-ts-app": {
      const repoUrlExisting = exportedUrls[`${params.repoName}-repo-url`];
      if (!params.githubToken && !repoUrlExisting) {
        throw new Error(
          "Pour React TS App, il faut repoUrl ou une ressource GitHub (repoName)."
        );
      }

      const reactApp = createReactStaticWebApp({
        tempDir: TEMP_DIR,
        appName: name,
        githubToken: params.githubToken,
        isPrivate: params?.isPrivate === "true",
        orgName: params.githubOrg,
        description: params?.description,
        resourceGroupName: rg.name,
        location: params.location ?? "westeurope",
        branch: params.branch ?? "main",
        repoUrl: repoUrlExisting, // <-- URL récupérée automatiquement
      });

      exportedUrls[`${name}-url`] = reactApp.url;
      exportedUrls[`${name}-repo-url`] = repoUrlExisting ?? params.repoName;
      break;
    }

    case "dotnet-api": {
      const repoUrlExisting =
        exportedUrls[`${params.repoName}-repo-url`] ?? params.repoUrl;
      if (!repoUrlExisting) {
        throw new Error(
          "Pour .NET API, il faut repoUrl ou une ressource GitHub (repoName)."
        );
      }

      const version = params.version ?? "10.0";
      const sku = params.sku ?? "B1";

      const apiOutputs = createDotNetApiTemplate({
        tempDir: TEMP_DIR,
        name,
        resourceGroupName: rg.name,
        location: params.location ?? "francecentral",
        repoUrl: repoUrlExisting,
        version,
        sku,
      });

      exportedUrls[`${name}-url`] = apiOutputs.apiUrl;
      exportedUrls[`${name}-repo-url`] = repoUrlExisting ?? params.repoName;
      break;
    }

    default:
      console.warn(`Type de ressource inconnu: ${type}`);
  }
}

for (const [key, value] of Object.entries(exportedUrls)) {
  exports[key] = value;
}
