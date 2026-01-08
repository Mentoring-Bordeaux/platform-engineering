import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native/web";

/**
 * Crée un App Service Linux dans un Resource Group existant.
 * @param appName Le nom de l'App Service
 * @param resourceGroupName Le nom du Resource Group existant (Input<string> accepté)
 * @param params Les paramètres supplémentaires (sku, runtimeStack, location)
 */
export function createAppService(
    appName: string,
    resourceGroupName: pulumi.Input<string>,
    params: { sku: string; runtimeStack: "DOTNET" | "NODE" | "PYTHON"; location?: string }
) {
    const location = params.location ?? "westeurope";
    const skuName = params.sku;
    const runtimeStack = params.runtimeStack;

    const plan = new azure.AppServicePlan(`${appName}-plan`, {
        resourceGroupName: resourceGroupName,
        location: location,
        sku: {
            name: skuName,
            tier: skuName.startsWith("B") ? "Basic" : "Standard",
            size: skuName,
            capacity: 1,
        },
        reserved: true,
    });

    const linuxFx: pulumi.Input<string> = (() => {
        switch (runtimeStack) {
            case "DOTNET": return "DOTNETCORE:10.0";
            case "NODE": return "NODE:18-lts";
            case "PYTHON": return "PYTHON:3.11";
            default: throw new Error(`runtimeStack invalide: ${runtimeStack}`);
        }
    })();

    const webApp = new azure.WebApp(appName, {
        resourceGroupName: resourceGroupName,
        kind: "app,linux",
        location: location,
        serverFarmId: plan.id, 
        siteConfig: {
            appSettings: [{ name: "WEBSITE_RUN_FROM_PACKAGE", value: "1" }],
            linuxFxVersion: linuxFx,
        },
    });

    return {
        webApp,
        url: pulumi.interpolate`https://${webApp.defaultHostName}`,
    };
}
