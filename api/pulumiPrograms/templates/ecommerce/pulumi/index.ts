import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";

// --- Configuration ---
const config = new pulumi.Config();

const rawProjectName = config.require("ProjectName");
const projectName = rawProjectName.toLowerCase().replace(/[^a-z0-9]/g, "");

const locationWeb = config.get("locationWeb") || "westeurope";
const locationDb = config.get("locationDb") || "francecentral";

const appFramework = config.get("app:framework") || "React";
const appDescription = config.get("app:description");
const backendStack = (config.get("backend:runtimeStack") || "NODE|14-lts").trim().toUpperCase();
const mainApiDescription = config.get("backend:api:mainApi:description");
const mainApiCaching = true;
const billingApiDescription = config.get("backend:api:billingApi:description");
const billingApiPaymentGateway = "Stripe";

const resourceGroup = new azure.resources.ResourceGroup(`${projectName}-rg`, {
    location: locationWeb,
});

const frontendApp = new azure.web.StaticSite(`${projectName}-app`, {
    resourceGroupName: resourceGroup.name,
    location: locationWeb,
    buildProperties: {
        appLocation: `frontend/${appFramework}`,
        apiLocation: "",
        outputLocation: "build",
    },
    sku: { name: "Free", tier: "Free" },
});

const adminPanel = new azure.web.StaticSite(`${projectName}-admin`, {
    resourceGroupName: resourceGroup.name,
    location: locationWeb,
    buildProperties: {
        appLocation: "admin",
        apiLocation: "",
        outputLocation: "publish",
    },
    sku: { name: "Free", tier: "Free" },
});

const cosmosDb = new azure.cosmosdb.DatabaseAccount(`${projectName}-db`, {
    resourceGroupName: resourceGroup.name,
    location: locationDb,
    databaseAccountOfferType: "Standard",
    locations: [{ locationName: locationDb, failoverPriority: 0 }],
    consistencyPolicy: { defaultConsistencyLevel: "Session" },
    capabilities: [{ name: "EnableServerless" }],
    kind: "GlobalDocumentDB",
});

const backendPlan = new azure.web.AppServicePlan(`${projectName}-plan`, {
    resourceGroupName: resourceGroup.name,
    location: locationWeb,
    kind: "Linux",
    reserved: true,
    sku: { name: "F1", tier: "Free", capacity: 1 },
});

const mainApi = new azure.web.WebApp(`${projectName}-backend-api`, {
    resourceGroupName: resourceGroup.name,
    location: locationWeb,
    serverFarmId: backendPlan.id,
    siteConfig: {
        linuxFxVersion: backendStack,
        appSettings: [
            { name: "APP_DESCRIPTION", value: mainApiDescription },
            { name: "ENABLE_CACHING", value: mainApiCaching.toString() },
            { name: "COSMOS_DB_ACCOUNT", value: cosmosDb.name },
        ],
    },
});

const billingApi = new azure.web.WebApp(`${projectName}-billing-api`, {
    resourceGroupName: resourceGroup.name,
    location: locationWeb,
    serverFarmId: backendPlan.id,
    siteConfig: {
        linuxFxVersion: backendStack,
        appSettings: [
            { name: "BILLING_DESCRIPTION", value: billingApiDescription },
            { name: "PAYMENT_GATEWAY", value: billingApiPaymentGateway },
        ],
    },
});
export const frontendStaticAppName = frontendApp.name;
export const frontendStaticAppUrl = frontendApp.defaultHostname;
export const adminUrl = adminPanel.defaultHostname;
export const mainApiUrl = mainApi.defaultHostName;
export const billingApiUrl = billingApi.defaultHostName;
export const cosmosDbName = cosmosDb.name;
export const resourceGroupName = resourceGroup.name;
