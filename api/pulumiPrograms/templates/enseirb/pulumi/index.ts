import * as pulumi from "@pulumi/pulumi";

const config = new pulumi.Config();

// Get template parameters
// Note: Parameters are flattened with dot notation (e.g., "app.framework", "backend.runtimeStack")
const framework = config.require("app.framework");
const appDescription = config.require("app.description");
const capacity = config.getNumber("capacity") || 400;
const adminPanelDescription = config.require("admin.description");
const runtimeStack = config.require("backend.runtimeStack");
const mainApiDescription = config.require("backend.api.mainApi.description");
const mainApiCaching =
    config.getBoolean("backend.api.mainApi.enableCaching") ?? true;
const billingApiDescription = config.require(
    "backend.api.billingApi.description",
);
const paymentGateway =
    config.get("backend.api.billingApi.paymentGateway") || "Stripe";

pulumi.log.info(`Capacity parameter value: ${capacity}`);
pulumi.log.info(`Main API caching enabled: ${mainApiCaching}`);
pulumi.log.info(`Payment gateway: ${paymentGateway}`);

// Placeholder: In a real scenario, this would create Azure resources
// For now, we just output the configuration
export const ecommerceConfig = {
    app: {
        framework: framework,
        description: appDescription,
    },
    database: {
        capacity: capacity,
    },
    admin: {
        description: adminPanelDescription,
    },
    backend: {
        runtimeStack: runtimeStack,
        api: {
            mainApi: {
                description: mainApiDescription,
                cachingEnabled: mainApiCaching,
            },
            billingApi: {
                description: billingApiDescription,
                paymentGateway: paymentGateway,
            },
        },
    },
};

pulumi.log.info(`E-commerce template configured with framework: ${framework}`);
