import * as pulumi from "@pulumi/pulumi";

const config = new pulumi.Config();

// Get template parameters from config
const framework = config.require("framework");
const dotnetVersion = config.get("dotnetVersion") || "10.0";
const sku = config.get("sku") || "B1";
const adminLogin = config.get("admin.login");
const adminPassword = config.getSecret("admin.password");

// Log parameter values (dummy logic)
pulumi.log.info(`Framework: ${framework}`);
pulumi.log.info(`.NET Version: ${dotnetVersion}`);
pulumi.log.info(`App Service Plan SKU: ${sku}`);
pulumi.log.info(`SQL Admin Login: ${adminLogin}`);
pulumi.log.info(`SQL Admin Password is set: ${adminPassword !== undefined}`);

// Placeholder: In a real scenario, this would create Azure resources
// For now, we just output the configuration
export const avanadeConfig = {
    framework,
    dotnetVersion,
    sku,
    admin: {
        login: adminLogin,
        password: adminPassword,
    },
};
