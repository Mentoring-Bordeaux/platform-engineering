import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as web from "@pulumi/azure-native/web";


// 1. Configurations
const config = new pulumi.Config();
const location = config.get("azure-native:location") || "westeurope"; 
const projectPrefix = "plateform-engineering";

// 2. Resource Group
const rg = new resources.ResourceGroup(`${projectPrefix}-rg`, { location });

// 3. Static Web App
const staticApp = new web.StaticSite(`${projectPrefix}-static-web-app`, {
    resourceGroupName: rg.name,
    location,
    sku: { name: "free", tier: "free" },
    repositoryUrl: "https://github.com/Mentoring-Bordeaux/platform-engineering.git",
    branch: "main",
});


export const staticWebUrl = staticApp.defaultHostname;
export const resourceGroupName = rg.name;