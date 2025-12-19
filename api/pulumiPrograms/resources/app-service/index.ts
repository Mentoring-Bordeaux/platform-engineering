import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";


const config = new pulumi.Config();
const location = config.get("location") || "westeurope";
const appName = config.require("name"); 
const skuName = config.require("sku");
const runtimeStack = config.require("runtimeStack"); 


const rg = new azure.resources.ResourceGroup(`${appName}-rg`, {
    resourceGroupName: `${appName}-rg`,
    location: location,
});


const plan = new azure.web.AppServicePlan(`${appName}-plan`, {
    resourceGroupName: rg.name,
    //name: `${appName}-plan`,
    sku: {
        name: skuName,
        tier: skuName.startsWith("B") ? "Basic" : "Standard",
        size: skuName,
        capacity: 1,
    },
    reserved: true,
});

let linuxFx: string;
switch(runtimeStack) {
    case "DOTNET":
        linuxFx = "DOTNETCORE:10.0";  
        break;
    case "NODE":
        linuxFx = "NODE:18-lts";     
        break;
    case "PYTHON":
        linuxFx = "PYTHON:3.11";      
        break;
    default:
        throw new Error(`runtimeStack invalide: ${runtimeStack}`);
}

const webApp = new azure.web.WebApp(appName, {
    resourceGroupName: rg.name,
    //name: appName,
    kind: "app,linux", 
    serverFarmId: plan.id,
    siteConfig: {
        appSettings: [{ name: "WEBSITE_RUN_FROM_PACKAGE", value: "1" }],
        linuxFxVersion: linuxFx,
    },
});



export const url = pulumi.interpolate`https://${webApp.defaultHostName}`;
