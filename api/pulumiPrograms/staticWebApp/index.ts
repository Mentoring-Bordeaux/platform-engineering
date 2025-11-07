import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";

const projectPrefix = "staticweb"; 


const resourceGroup = new azure.resources.ResourceGroup(`rg-${projectPrefix}-`, {
    location: "westeurope",
});

const storageAccount = new azure.storage.StorageAccount(`st${projectPrefix}`, {
    resourceGroupName: resourceGroup.name,
    sku: { name: azure.storage.SkuName.Standard_LRS },
    kind: azure.storage.Kind.StorageV2,
    enableHttpsTrafficOnly: true,
});

const staticWebsite = new azure.storage.StorageAccountStaticWebsite(`stw-${projectPrefix}-`, {
    accountName: storageAccount.name,
    resourceGroupName: resourceGroup.name,
    indexDocument: "index.html",
});

const indexFile = new azure.storage.Blob("index.html", {
    resourceGroupName: resourceGroup.name,
    accountName: storageAccount.name,
    containerName: "$web",
    source: new pulumi.asset.FileAsset("www/index.html"),
    contentType: "text/html",
});
export const staticWebsiteUrl = storageAccount.primaryEndpoints.apply(pe => pe.web);
