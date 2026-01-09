import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as os from "os";

import { createStaticWebApp } from "./templates/static-web-app/static-web-app";
import { createGithubRepo } from "./templates/github/github";
import { createAppService } from "./templates/app-service/app-service";
import {
  createReactRepoInitial,
  deployStaticWebAppFromRepo,
} from "./templates/react-ts-app/react-ts-app";

const config = new pulumi.Config();
const projectName = config.require("projectName");
const resourcesConfig = config.requireObject<any[]>("resources");

const rg = new resources.ResourceGroup(`${projectName}-rg`, {
  location: "westeurope",
});

const TEMP_DIR = os.tmpdir();

const exportedUrls: Record<string, pulumi.Output<string>> = {};

const githubRepos: Record<string, pulumi.Output<string>> = {};

for (const res of resourcesConfig.filter(r => r.resourceType.toLowerCase() === "github")) {
  const repo = createGithubRepo(
    res.name,
    res.parameters?.isPrivate === "true",
    res.parameters?.description,
    res.parameters?.githubOrg,
    res.parameters?.githubToken
  );

  githubRepos[res.name] = repo.repoUrl;
  exportedUrls[`${res.name}-repo-url`] = repo.repoUrl;
}

for (const res of resourcesConfig.filter(r => r.resourceType.toLowerCase() !== "github")) {
  const type = res.resourceType.toLowerCase();
  const name = res.name;
  const params = res.parameters ?? {};

  switch (type) {
    case "static-web-app": {
      const firstRepoUrl = Object.values(githubRepos)[0];
      if (!firstRepoUrl) throw new Error("Aucun repo GitHub défini pour la Static Web App");

      const swa = firstRepoUrl.apply(repoUrl =>
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
      const firstRepoUrl = Object.values(githubRepos)[0];
      if (!firstRepoUrl) throw new Error("Aucun repo GitHub défini pour l’app React");

      const reactAppUrl = firstRepoUrl.apply(repoUrl => {
        createReactRepoInitial(repoUrl, TEMP_DIR, name);
        const reactApp = deployStaticWebAppFromRepo(
          name,
          rg.name,
          params.location ?? "westeurope",
          repoUrl,
          params.branch ?? "main"
        );

        return reactApp.url;
      });

      exportedUrls[`${name}-url`] = reactAppUrl;
      break;
    }
  }
}

for (const [key, value] of Object.entries(exportedUrls)) {
  exports[key] = value;
}
