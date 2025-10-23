import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as web from "@pulumi/azure-native/web";
import * as containerapp from "@pulumi/azure-native/app";
import * as containerregistry from "@pulumi/azure-native/containerregistry";

// 1. Configurations
const config = new pulumi.Config();
const location = config.get("azure-native:location") || "westeurope";
const projectPrefix = "platformeng"; // nom court et conforme

// 2. Resource Group
const rg = new resources.ResourceGroup(`${projectPrefix}-rg`, { location });

// 3. Static Web App
const staticApp = new web.StaticSite(`${projectPrefix}-stapp`, {
    resourceGroupName: rg.name,
    location,
    sku: { name: "Free", tier: "Free" },
    repositoryUrl: "https://github.com/Mentoring-Bordeaux/platform-engineering.git",
    branch: "main",
});

// 4. Container App Environment
const env = new containerapp.ManagedEnvironment(`${projectPrefix}-env`, {
    resourceGroupName: rg.name,
    location,
});

// 5. Container Registry
const acr = new containerregistry.Registry(`${projectPrefix}cr`, {
    resourceGroupName: rg.name,
    sku: { name: "Basic" },
    adminUserEnabled: true,
    location,
});

// 6. Récupérer les credentials de l'ACR
const credentials = containerregistry.listRegistryCredentialsOutput({
    resourceGroupName: rg.name,
    registryName: acr.name,
});

const acrUsername = credentials.apply(c => c.username!);
const acrPassword = credentials.apply(c => c.passwords![0].value!);

// 7. Container App avec secret pour ACR
const backend = new containerapp.ContainerApp(`${projectPrefix}-ca`, {
    resourceGroupName: rg.name,
    location,
    managedEnvironmentId: env.id,
    configuration: {
        ingress: {
            external: true,
            targetPort: 8080,
        },
        registries: [
            {
                server: acr.loginServer,
                username: acrUsername,
                passwordSecretRef: "acr-password",
            },
        ],
        secrets: [
            { name: "acr-password", value: acrPassword },
        ],
    },
    template: {
        containers: [
            {
                name: "api",
                image: "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest",
                resources: {
                    cpu: 0.25,
                    memory: "0.5Gi",
                },
            },
        ],
    },
});

// 8. Exports
export const staticWebUrl = staticApp.defaultHostname;
export const backendUrl = backend.latestRevisionFqdn.apply(fqdn => `http://${fqdn}`);
export const resourceGroupName = rg.name;
export const acrLoginServer = acr.loginServer;
export const acrUsernameExport = acrUsername;
