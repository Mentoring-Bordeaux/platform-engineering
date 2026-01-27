import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";

const config = new pulumi.Config();

const location = "westeurope";
const rgName = config.get("ProjectName");

const aiApiKey = config.requireSecret("apiKey");



const resourceGroup = new azure.resources.ResourceGroup("chatbot-rg", {
    resourceGroupName: rgName,
    location,
});

const storageAccount = new azure.storage.StorageAccount("chatbotsa", {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
    sku: { name: "Standard_LRS" },
    kind: "StorageV2",
});


const appServicePlan = new azure.web.AppServicePlan("chatbot-plan", {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
    kind: "FunctionApp",
    sku: { name: "Y1", tier: "Dynamic" }, 
});

const functionApp = new azure.web.WebApp("chatbot-func", {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
    serverFarmId: appServicePlan.id,
    kind: "FunctionApp",
    siteConfig: {
        appSettings: [
            { name: "FUNCTIONS_EXTENSION_VERSION", value: "~4" },
            { name: "WEBSITE_RUN_FROM_PACKAGE", value: "1" },
            { name: "AzureWebJobsStorage", value: storageAccount.primaryEndpoints.apply(e => e.blob) },
            { name: "AI_API_KEY", value: aiApiKey.apply(k => k) },
        ],
    },
});

const staticWebApp = new azure.web.StaticSite("chatbot-frontend", {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
    sku: { name: "Free" },
    buildProperties: {},
    
});



export const resourceGroupName = resourceGroup.name;
export const functionAppUrl = functionApp.defaultHostName.apply(h => h);
export const staticWebAppUrl = staticWebApp.defaultHostname.apply(h => h);
