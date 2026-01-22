import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";
import * as appinsights from "@pulumi/azure-native/applicationinsights";

const config = new pulumi.Config();
const name = config.require("Name");
const location = config.get("location") || "westeurope";
const dotnetVersion = config.get("dotnetVersion") || "8.0";
const sku = config.get("sku") || "B1";
const adminPassword = config.get("adminPassword");
const adminLogin = config.get("adminLogin") || "sqladminuser";

const resourceGroup = new azure.resources.ResourceGroup(`${name}-rg`, {
    location,
});

const clientConfig = azure.authorization.getClientConfig();

// const kv = new azure.keyvault.Vault(`${name}-kv`, {
//     resourceGroupName: resourceGroup.name,
//     location,
//     properties: {
//         sku: { family: "A", name: "standard" },
//         tenantId: clientConfig.then(c => c.tenantId),
//         accessPolicies: [],
//         enableSoftDelete: true,
//     },
// });

// const sqlServer = new azure.sql.Server(`${name}-sqlserver`, {
//     resourceGroupName: resourceGroup.name,
//     location,
//     administratorLogin: adminLogin,
//     administratorLoginPassword: adminPassword,
//     version: "12.0",
// });

// const database = new azure.sql.Database(`${name}-db`, {
//     resourceGroupName: resourceGroup.name,
//     location,
//     serverName: sqlServer.name,
//     sku: { name: "S0", tier: "Standard" },
// });

// const logWorkspace = new azure.operationalinsights.Workspace(`${name}-law`, {
//     resourceGroupName: resourceGroup.name,
//     location,
//     sku: { name: "PerGB2018" },
//     retentionInDays: 30,
// });


const appServicePlan = new azure.web.AppServicePlan(`${name}-plan`, {
    resourceGroupName: resourceGroup.name,
    location,
    sku: { name: sku, tier: "Basic", capacity: 1 },
    kind: "app", // Windows
});

const apiApp = new azure.web.WebApp(`${name}-api`, {
    resourceGroupName: resourceGroup.name,
    location,
    serverFarmId: appServicePlan.id,
    siteConfig: {
        windowsFxVersion: `DOTNET|${dotnetVersion}`,
        appSettings: [
            { name: "WEBSITE_RUN_FROM_PACKAGE", value: "1" },
            // { name: "AZURE_KEYVAULT_NAME", value: kv.name },
        ],
    },
});

// const appInsights = new appinsights.Component(`${name}-ai`, {
//     resourceGroupName: resourceGroup.name,
//     location,
//     applicationType: "web",
//     kind: "web",
//     ingestionMode: "LogAnalytics",
//     workspaceResourceId: logWorkspace.id,
// });

export const apiUrl = pulumi.interpolate`https://${apiApp.defaultHostName}`;
// export const keyVaultName = kv.name;
// export const sqlDatabaseName = database.name;
// export const appInsightsInstrumentationKey = appInsights.instrumentationKey;
