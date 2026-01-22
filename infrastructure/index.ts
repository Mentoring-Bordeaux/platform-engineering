import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as web from "@pulumi/azure-native/web";
import * as containerapp from "@pulumi/azure-native/app";
import * as containerregistry from "@pulumi/azure-native/containerregistry";
import * as managedidentity from "@pulumi/azure-native/managedidentity";
import * as authorization from "@pulumi/azure-native/authorization";
import * as azure_native from "@pulumi/azure-native";
import * as keyvault from "@pulumi/azure-native/keyvault";
import * as random from "@pulumi/random";

const projectPrefix = "platformeng";

const cfg = new pulumi.Config();
const pulumiAccessToken = cfg.requireSecret("PULUMI_ACCESS_TOKEN");
const githubToken = cfg.requireSecret("GithubToken");
const gitlabToken = cfg.requireSecret("GitLabToken");
const githubOrganizationName = cfg.requireSecret("GitHubOrganizationName");

const rg = new resources.ResourceGroup(`rg-${projectPrefix}-`);

const staticApp = new web.StaticSite(`stapp-${projectPrefix}-`, {
  resourceGroupName: rg.name,
  sku: { name: "Standard", tier: "Standard" },
  buildProperties: {},
});

const env = new containerapp.ManagedEnvironment(`env-${projectPrefix}-`, {
  resourceGroupName: rg.name,
});

const acr = new containerregistry.Registry(`cr${projectPrefix}`, {
  resourceGroupName: rg.name,
  sku: { name: "Basic" },
  adminUserEnabled: false,
});

const identity = new managedidentity.UserAssignedIdentity(
  `uai-${projectPrefix}`,
  {
    resourceGroupName: rg.name,
  },
);

const acrPullRoleDefinitionId =
  "/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d";

const roleAssignment = new authorization.RoleAssignment(
  `ra-acr-pull-${projectPrefix}`,
  {
    principalId: identity.principalId,
    roleDefinitionId: acrPullRoleDefinitionId,
    scope: rg.id,
    principalType: "ServicePrincipal",
  },
);
const client = authorization.getClientConfigOutput();

const kvSuffix = new random.RandomString(`kv-sfx-${projectPrefix}`, {
  length: 6,
  special: false,
  upper: false,
}).result;

// Key Vault naming: 3-24 chars, alphanum + hyphen, start with letter
const keyVaultName = pulumi.interpolate`kv${projectPrefix}${kvSuffix}`; // ex: kvplatformengabc123

const vault = new keyvault.Vault(`kv-${projectPrefix}`, {
  resourceGroupName: rg.name,
  location: rg.location,
  vaultName: keyVaultName,
  properties: {
    tenantId: client.tenantId,
    sku: { family: "A", name: "standard" },
    enableRbacAuthorization: true,
    accessPolicies: [], // RBAC => empty policies OK
  },
});


const kvSecrets: Record<string, pulumi.Input<string>> = {
  "pulumi-access-token": pulumiAccessToken,
  "github-token": githubToken,
  "gitlab-token": gitlabToken,
  "github-organization-name": githubOrganizationName,
};

const createdKvSecrets: Record<string, keyvault.Secret> = {};

for (const [secretName, secretValue] of Object.entries(kvSecrets)) {
  createdKvSecrets[secretName] = new keyvault.Secret(
    `kvsec-${projectPrefix}-${secretName}`,
    {
      resourceGroupName: rg.name,
      vaultName: vault.name,
      secretName,
      properties: { value: secretValue },
    },
    {
      // Optional: aliases if you renamed the Pulumi resource name previously
      ...(secretName === "pulumi-access-token"
        ? { aliases: [{ name: `kvsec-${projectPrefix}-pulumi-access-token` }] }
        : {}),
    },
  );
}


const kvSecretUris: Record<string, pulumi.Output<string>> = {};
for (const secretName of Object.keys(kvSecrets)) {
  const s = createdKvSecrets[secretName];
  const info = keyvault.getSecretOutput(
    { resourceGroupName: rg.name, vaultName: vault.name, secretName },
    { dependsOn: [s] },
  );
  kvSecretUris[secretName] = info.properties.secretUriWithVersion;
}

// Donner le droit à l'identité (UAI) de lire les secrets du vault (RBAC)
const keyVaultSecretsUserRoleDefinitionId =
  "/providers/Microsoft.Authorization/roleDefinitions/4633458b-17de-408a-b874-0445c86b69e6";

const kvRoleAssignmentName = new random.RandomUuid(
  `ra-kv-guid-${projectPrefix}`,
).result;

const roleAssignmentKvSecretsUser = new authorization.RoleAssignment(
  `ra-kv-secrets-${projectPrefix}`,
  {
    roleAssignmentName: kvRoleAssignmentName,
    principalId: identity.principalId,
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId,
    scope: vault.id,
    principalType: "ServicePrincipal",
  },
);

const backend = new containerapp.ContainerApp(
  `ca-${projectPrefix}-`,
  {
    resourceGroupName: rg.name,
    managedEnvironmentId: env.id,
    identity: {
      type: "SystemAssigned, UserAssigned",
      userAssignedIdentities: identity.id.apply(id => ({
        [id]: {},
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

      secrets: [
        {
          name: "pulumi-access-token",
          identity: identity.id,
          keyVaultUrl: kvSecretUris["pulumi-access-token"],
        },
        {
          name: "github-token",
          identity: identity.id,
          keyVaultUrl: kvSecretUris["github-token"],
        },
        {
          name: "gitlab-token",
          identity: identity.id,
          keyVaultUrl: kvSecretUris["gitlab-token"],
        },
        {
          name: "github-organization-name",
          identity: identity.id,
          keyVaultUrl: kvSecretUris["github-organization-name"],
        },
      ],
    },
    template: {
      scale: {
        minReplicas: 1,
        maxReplicas: 1,
      },
      containers: [
        {
          name: "api",
          image: "mcr.microsoft.com/dotnet/aspnet:10.0.0-rc.2",
          resources: {
            cpu: 1,
            memory: "2Gi",
          },
          env: [
            {
              name: "PULUMI_ACCESS_TOKEN",
              secretRef: "pulumi-access-token",
            },
            {
              name: "GitHubToken",
              secretRef: "github-token",
            },
            {
              name: "GitLabToken",
              secretRef: "gitlab-token",
            },
            {
              name: "GitHubOrganizationName",
              secretRef: "github-organization-name",
            },
            {
              name: "ARM_USE_MSI",
              value: "true"
            },
            {
              name: "ARM_SUBSCRIPTION_ID",
              value: client.subscriptionId
            },
            {
              name: "NuxtAppUrl",
              value: staticApp.defaultHostname
            },
          ],
        },
      ],
    },
  },
  {
    dependsOn: [roleAssignment, // ACR pull
      roleAssignmentKvSecretsUser, // UAI can read secrets
    ]
  },
);

// --- RBAC pour que Pulumi (MSI System Assigned) puisse créer/lire des Resource Groups ---
// Contributor role
const contributorRoleDefinitionId =
  "/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c";

// Scope subscription (obligatoire si tes programmes créent des RG dynamiques)
const subscriptionScope = pulumi.interpolate`/subscriptions/${client.subscriptionId}`;

// RoleAssignmentName doit être un GUID
const raSystemContributorName = new random.RandomUuid(
  `ra-ca-system-contrib-guid-${projectPrefix}`,
).result;

const roleAssignmentSystemContributor = new authorization.RoleAssignment(
  `ra-ca-system-contrib-${projectPrefix}`,
  {
    roleAssignmentName: raSystemContributorName,

    // principalId de la System Assigned Identity de la Container App
    principalId: backend.identity.apply((i) => i?.principalId!),

    roleDefinitionId: contributorRoleDefinitionId,
    scope: subscriptionScope,
    principalType: "ServicePrincipal",
  },
  { dependsOn: [backend] },
);


const staticWebAppSecrets = web.listStaticSiteSecretsOutput({
  name: staticApp.name,
  resourceGroupName: rg.name,
});

const staticWebAppDeploymentToken = staticWebAppSecrets.apply(
  (secrets) => (secrets.properties ? secrets.properties["apiKey"] : undefined),
);

const staticSiteLinkedBackend = new azure_native.web.StaticSiteLinkedBackend(
  "staticSiteLinkedBackend",
  {
    backendResourceId: backend.id,
    linkedBackendName: "api",
    name: staticApp.name,
    region: staticApp.location,
    resourceGroupName: rg.name,
  },
);

export const staticWebUrl = staticApp.defaultHostname;
export const staticWebAppName = staticApp.name;
export const backendUrl = backend.latestRevisionFqdn.apply(
  (fqdn) => `https://${fqdn}`,
);
export const resourceGroupName = rg.name;
export const containerRegistryName = acr.name;
export const containerAppName = backend.name;
export const acrServer = acr.loginServer;
export const keyVault = vault.name;