import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";

const config = new pulumi.Config();
const projectPrefix = config.require("appName");
const location = config.get("location") || "westeurope";
const skuName = config.get("skuName") || "Free";
const skuTier = config.get("skuTier") || "Free";

const projectPrefixFormated = projectPrefix.toLowerCase()

const resourceGroup = new azure.resources.ResourceGroup(`rg-${projectPrefixFormated}-`, {
    location: location,
});

const staticWebApp = new azure.web.StaticSite(`stapp-${projectPrefixFormated}-`, {
    resourceGroupName: resourceGroup.name,
    sku: { name: skuName, tier: skuTier },
    buildProperties: {}
});

export const url = pulumi.interpolate`https://${staticWebApp.defaultHostname}`;
