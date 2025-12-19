import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";

const config = new pulumi.Config();
const rawName = config.require("name");
const appName = rawName.toLowerCase().replace(/[^a-z0-9-]/g, "");
const location = config.get("location") || "westeurope";
const containerImage = config.require("containerImage"); 

const resourceGroup = new azure.resources.ResourceGroup(`rg-${appName}`, {
    location: location,
});


const containerEnv = new azure.app.ManagedEnvironment(`env-${appName}`, {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
});


const containerApp = new azure.app.ContainerApp(`ca-${appName}`, {
    resourceGroupName: resourceGroup.name,
    managedEnvironmentId: containerEnv.id,
    configuration: {
        ingress: {
            external: true,
            targetPort: 80,
            traffic: [{ latestRevision: true, weight: 100 }],
        },
    },
    template: {
        containers: [
            {
                name: appName,
                image: containerImage,
                resources: {
                    cpu: 0.5,
                    memory: "1Gi",
                },
            },
        ],
        scale: {
            minReplicas: 1,
            maxReplicas: 3,
        },
    },
});

export const containerAppUrl = pulumi.interpolate`https://${containerApp.configuration.apply(c => c?.ingress?.fqdn)}`;
