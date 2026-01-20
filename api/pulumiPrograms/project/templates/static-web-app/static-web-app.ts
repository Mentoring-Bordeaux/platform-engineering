import * as azure from "@pulumi/azure-native";
import * as pulumi from "@pulumi/pulumi";


export function createStaticWebApp(
    name: string,
    resourceGroupName: pulumi.Input<string>,
    location: pulumi.Input<string>,
    repositoryUrl?: pulumi.Input<string>,
    branch: pulumi.Input<string> = "main"
) {
    const staticWebApp = new azure.web.StaticSite(name, {
        resourceGroupName,
        location,
        sku: { name: "Free", tier: "Free" },
        repositoryUrl,
        branch,
    });

    return {
        staticWebApp,
        url: pulumi.interpolate`https://${staticWebApp.defaultHostname}`,
    };
}
