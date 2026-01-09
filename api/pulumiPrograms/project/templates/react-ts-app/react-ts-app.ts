import * as azure from "@pulumi/azure-native";
import * as pulumi from "@pulumi/pulumi";
import { execSync } from "child_process";
import * as fs from "fs";
import * as path from "path";


export function createReactRepoInitial(
  repoUrl: string,
  tempDir: string,
  appName: string
) {
  const appPath = path.join(tempDir, appName);

  console.log("Création du projet React TS dans :", appPath);

  if (fs.existsSync(appPath)) {
    fs.rmSync(appPath, { recursive: true, force: true });
  }

  execSync(
    `npx create-react-app ${appName} --template typescript`,
    { stdio: "inherit", cwd: tempDir }
  );

 const gitignorePath = path.join(appPath, ".gitignore");

  const gitignoreContent = `
# dependencies
/node_modules

# production
/build

# misc
.DS_Store
.env
.env.local
.env.development.local
.env.test.local
.env.production.local

# logs
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# IDE
.vscode
.idea

# OS
Thumbs.db
`;

  fs.appendFileSync(gitignorePath, "\n" + gitignoreContent);

  console.log("✅ .gitignore ajouté");

  
  execSync("git init", { cwd: appPath });
  execSync("git branch -M main", { cwd: appPath });

  execSync(`git remote add origin ${repoUrl}`, { cwd: appPath });

  execSync("git add .", { cwd: appPath });
  execSync('git commit -m "Initial commit React TS"', { cwd: appPath });

  execSync("git push -u origin main --force", {
    cwd: appPath,
    stdio: "inherit",
  });

  console.log("✅ Repo React TS pushé vers GitHub avec succès");
}


export function deployStaticWebAppFromRepo(
    name: string,
    resourceGroupName: pulumi.Input<string>,
    location: pulumi.Input<string>,
    repositoryUrl: pulumi.Input<string>,
    branch: pulumi.Input<string> = "main"
): { staticWebApp: azure.web.StaticSite; url: pulumi.Output<string> } {
    const staticWebApp = new azure.web.StaticSite(name, {
        resourceGroupName,
        location,
        sku: { name: "Free", tier: "Free" },
        repositoryUrl,
        branch,
        buildProperties: {
            appLocation: "/",
            apiLocation: "",
            appArtifactLocation: "build",
            appBuildCommand: "npm run build",
        },
    });

    return {
        staticWebApp,
        url: pulumi.interpolate`https://${staticWebApp.defaultHostname}`,
    };
}
