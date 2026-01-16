import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as web from "@pulumi/azure-native/web";
import * as containerapp from "@pulumi/azure-native/app";
import * as containerregistry from "@pulumi/azure-native/containerregistry";
import * as managedidentity from "@pulumi/azure-native/managedidentity";
import * as authorization from "@pulumi/azure-native/authorization";
import * as azure_native from "@pulumi/azure-native";


const projectPrefix = "platformeng"; 

const rg = new resources.ResourceGroup(`rg-${projectPrefix}-`);

const staticApp = new web.StaticSite(`stapp-${projectPrefix}-`, {
    resourceGroupName: rg.name,
    sku: { name: "Standard", tier: "Standard" },
    buildProperties: {}
});

const env = new containerapp.ManagedEnvironment(`env-${projectPrefix}-`, {
    resourceGroupName: rg.name,
});

const acr = new containerregistry.Registry(`cr${projectPrefix}`, {
    resourceGroupName: rg.name,
    sku: { name: "Basic" },
    adminUserEnabled: false,
});

const identity = new managedidentity.UserAssignedIdentity(`uai-${projectPrefix}`, {
    resourceGroupName: rg.name,
});

const acrPullRoleDefinitionId ="/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d";

const roleAssignment = new authorization.RoleAssignment(`ra-acr-pull-${projectPrefix}`, {
  principalId: identity.principalId,
  roleDefinitionId: acrPullRoleDefinitionId,
  scope: rg.id,
  principalType: "ServicePrincipal", 
});


const backend = new containerapp.ContainerApp(`ca-${projectPrefix}-`, {
    resourceGroupName: rg.name,
    managedEnvironmentId: env.id,
    identity: {
        type: "UserAssigned",
        userAssignedIdentities: identity.id.apply(id => ({
            [id]: {}
        })) as any,
    },

    configuration: {
        ingress: {
            external: true,
            targetPort: 5064,
        },
        registries: [
            {
                server: acr.loginServer,
                identity: identity.id,
            },
        ],
    },
    template: {
        containers: [
            {
                name: "api",
                image: "mcr.microsoft.com/dotnet/aspnet:10.0.0-rc.2",
                resources: {
                    cpu: 0.25,
                    memory: "0.5Gi",
                },
            },
        ],
    },
}, { dependsOn: [roleAssignment] });
 
const staticWebAppSecrets = web.listStaticSiteSecretsOutput({
    name: staticApp.name,
    resourceGroupName: rg.name,
});

export const staticWebAppDeploymentToken = staticWebAppSecrets.apply(secrets =>
    secrets.properties ? secrets.properties["apiKey"] : undefined
);

const staticSiteLinkedBackend = new azure_native.web.StaticSiteLinkedBackend("staticSiteLinkedBackend", {
    backendResourceId: backend.id,
    linkedBackendName: "api",
    name: staticApp.name,
    region: staticApp.location,
    resourceGroupName: rg.name,
});

export const staticWebUrl = staticApp.defaultHostname;
export const staticWebAppName = staticApp.name;
export const backendUrl = backend.latestRevisionFqdn.apply(fqdn => `https://${fqdn}`);
export const resourceGroupName = rg.name;
export const containerRegistryName = acr.name;
export const containerAppName = backend.name;
export const acrServer = acr.loginServer;