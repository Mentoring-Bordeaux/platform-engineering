import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";

const config = new pulumi.Config();
const name = config.require("Name");
const location = config.get("location") || "westeurope";
const adminLogin = config.require("adminLogin");
const adminPassword = config.require("adminPassword");
const sku = config.get("sku") || "GP_Gen5_2";

const formattedName = name.toLowerCase().replace(/[^a-z0-9-]/g, "");

const rg = new azure.resources.ResourceGroup(`rg-${formattedName}`, {
    location,
});

const server = new azure.sql.Server(`sql-${formattedName}`, {
    resourceGroupName: rg.name,
    administratorLogin: adminLogin,
    administratorLoginPassword: adminPassword,
    location,
    version: "12.0",
});

const db = new azure.sql.Database(`db-${formattedName}`, {
    resourceGroupName: rg.name,
    serverName: server.name,
    sku: {
        name: sku,
    },
});

export const resourceGroup = rg.name;
export const serverName = server.name;
export const databaseName = db.name;
