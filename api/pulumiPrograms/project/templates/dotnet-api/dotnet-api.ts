import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";
import * as appinsights from "@pulumi/azure-native/applicationinsights";
import { execSync } from "child_process";
import * as fs from "fs";
import * as path from "path";
import * as os from "os";

interface CreateDotNetApiTemplateOptions {
  tempDir: string;
  name: string;
  resourceGroupName: pulumi.Input<string>;
  location?: pulumi.Input<string>;
  githubToken?: string;
  repoUrl?: pulumi.Input<string>; // <-- si repo existant
  version?: string;
  sku?: string;
}

export function createDotNetApiTemplate(options: CreateDotNetApiTemplateOptions) {
  const {
    tempDir,
    name,
    resourceGroupName,
    location = "francecentral",
    githubToken,
    repoUrl,
    version = "10.0",
    sku = "B1",
  } = options;

  // Wrap dans un Output pour gérer repoUrl existant
  return pulumi.output(repoUrl).apply(existingRepoUrl => {
    const finalRepoUrl = existingRepoUrl;

    /* -------------------- RESOURCE INFRA -------------------- */
    const clientConfig = azure.authorization.getClientConfig();

    const kv = new azure.keyvault.Vault(`${name}-kv`, {
      resourceGroupName,
      location,
      properties: {
        sku: { family: "A", name: "standard" },
        tenantId: clientConfig.then(c => c.tenantId),
        accessPolicies: [],
        enableSoftDelete: true,
      },
    });

    const sqlServer = new azure.sql.Server(`${name}-sqlserver`, {
      resourceGroupName,
      location,
      administratorLogin: "sqladminuser",
      administratorLoginPassword: "P@ssword1234!",
      version: "12.0",
    });

    const database = new azure.sql.Database(`${name}-db`, {
      resourceGroupName,
      location,
      serverName: sqlServer.name,
      sku: { name: "S0", tier: "Standard" },
    });

    const logWorkspace = new azure.operationalinsights.Workspace(`${name}-law`, {
      resourceGroupName,
      location,
      sku: { name: "PerGB2018" },
      retentionInDays: 30,
    });

    const appServicePlan = new azure.web.AppServicePlan(`${name}-plan`, {
      resourceGroupName,
      location,
      sku: { name: sku, tier: "Basic", capacity: 1 },
      kind: "app",
    });

    const apiApp = new azure.web.WebApp(`${name}-api`, {
      resourceGroupName,
      location,
      serverFarmId: appServicePlan.id,
      siteConfig: {
        windowsFxVersion: `DOTNET|${version}`,
        appSettings: [
          { name: "WEBSITE_RUN_FROM_PACKAGE", value: "1" },
          { name: "AZURE_KEYVAULT_NAME", value: kv.name },
        ],
      },
    });

    const appInsights = new appinsights.Component(`${name}-ai`, {
      resourceGroupName,
      location,
      applicationType: "web",
      kind: "web",
      ingestionMode: "LogAnalytics",
      workspaceResourceId: logWorkspace.id,
    });

    /* -------------------- GIT & CODE -------------------- */
    const appPath = path.join(tempDir, name);
    if (fs.existsSync(appPath)) fs.rmSync(appPath, { recursive: true, force: true });
    fs.mkdirSync(appPath, { recursive: true });

    console.log("Création du projet .NET API dans :", appPath);
    execSync(`dotnet new webapi -n ${name}`, { stdio: "inherit", cwd: tempDir });

    // Git init et push initial
    execSync("git init", { cwd: appPath });
    execSync("git branch -M main", { cwd: appPath });
    execSync(`git remote add origin ${finalRepoUrl}`, { cwd: appPath });
    execSync("git add .", { cwd: appPath });
    execSync('git commit -m "Initial commit .NET API "', { cwd: appPath });

   
    execSync("git push -u origin main --force", { cwd: appPath, stdio: "inherit" });

    console.log("✅ .NET API pushé dans le repo existant");

    return {
      repoUrl: finalRepoUrl,
      apiUrl: pulumi.interpolate`https://${apiApp.defaultHostName}`,
      keyVaultName: kv.name,
      sqlDatabaseName: database.name,
      appInsightsInstrumentationKey: appInsights.instrumentationKey,
    };
  });
}
