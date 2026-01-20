import * as azure from "@pulumi/azure-native";
import * as pulumi from "@pulumi/pulumi";
import { execSync } from "child_process";
import * as fs from "fs";
import * as path from "path";
import { createGithubRepo } from "../github/github";
import { createStaticWebApp } from "../static-web-app/static-web-app";

interface CreateReactAppOptions {
  tempDir: string;
  appName: string;
  githubToken?: string;
  isPrivate?: boolean;
  orgName?: string;
  description?: string;
  resourceGroupName: pulumi.Input<string>;
  location?: pulumi.Input<string>;
  branch?: pulumi.Input<string>;
  repoUrl?: pulumi.Input<string>; // <-- nouveau paramètre
}

export function createReactStaticWebApp(options: CreateReactAppOptions) {
  const {
    tempDir,
    appName,
    githubToken,
    isPrivate = true,
    orgName,
    description,
    resourceGroupName,
    location = "westeurope",
    branch = "main",
    repoUrl,
  } = options;

  const deployedApp = pulumi.output(repoUrl).apply(existingUrl => {
    let finalRepoUrl = existingUrl;

    const appPath = path.join(tempDir, appName);
    if (fs.existsSync(appPath)) fs.rmSync(appPath, { recursive: true, force: true });
    fs.mkdirSync(appPath, { recursive: true });

    console.log("Création du projet React TS...");
    execSync(`npx create-react-app ${appName} --template typescript`, {
      cwd: tempDir,
      stdio: "inherit",
    });

    // Ajouter .gitignore
    const gitignorePath = path.join(appPath, ".gitignore");
    const gitignoreContent = `
/node_modules
/build
.DS_Store
.env*
npm-debug.log*
yarn-debug.log*
.vscode
.idea
Thumbs.db
`;
    fs.appendFileSync(gitignorePath, gitignoreContent);

    console.log("✅ .gitignore ajouté");

    // Crée la Static Web App si nécessaire
    const { staticWebApp, url } = createStaticWebApp(appName, resourceGroupName, location, finalRepoUrl, branch);

    // Git init et push initial
    execSync("git init", { cwd: appPath });
    execSync(`git branch -M ${branch}`, { cwd: appPath });
    execSync(`git remote add origin ${finalRepoUrl}`, { cwd: appPath });
    execSync("git add .", { cwd: appPath });
    execSync('git commit -m "Initial commit React TS avec CI/CD"', { cwd: appPath });
    execSync(`git push -u origin ${branch} --force`, { cwd: appPath, stdio: "inherit" });

    console.log("✅ Code React TS pushé dans le repo existant");

    return {
      repoUrl: finalRepoUrl,
      appPath,
      staticWebApp,
      url: pulumi.interpolate`https://${staticWebApp.defaultHostname}`,
    };
  });

  return deployedApp;
}
