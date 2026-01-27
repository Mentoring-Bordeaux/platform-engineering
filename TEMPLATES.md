# Templates Documentation

## Overview

Templates are the foundation for project scaffolding in the Platform Engineering system. A template consists of two main components:

1. **template.yaml** - Defines the configuration schema (parameters/questions to ask users)
2. **Pulumi Program** (in `pulumi/` folder) - Infrastructure code that uses those parameters

Together, they enable users to create customized cloud resources by answering a series of guided questions.

## Template Structure

Each template is organized as follows:

```
templates/
└── {template-name}/
    ├── template.yaml          # Configuration schema
    └── pulumi/
        ├── index.ts           # Pulumi infrastructure code
        ├── Pulumi.yaml        # Pulumi stack metadata
        └── package.json       # Node.js dependencies
```

## template.yaml Schema

The `template.yaml` file defines the questions presented to users. It uses a declarative schema format with the following structure:

```yaml
schemaVersion: 1.0.0
kind: Template
name: unique-template-name # Same name as folder
displayName: Display Name for UI
description: Template description
parameters:
    # Parameters organized hierarchically
    section-name:
        question-key:
            type: text|textarea|enum|boolean|number
            required: true|false
            label: Short label
            displayName: Display name for UI
            description: Help text
            values: [for enums]
            default: default-value
resources:
    # Resources that will be provisioned
    - type: azure.swa
      name: resource-name
```

### Parameter Types

| Type       | Description            | Example                               |
| ---------- | ---------------------- | ------------------------------------- |
| `text`     | Single-line text input | Project name, API key                 |
| `textarea` | Multi-line text input  | Descriptions, comments                |
| `enum`     | Dropdown selection     | Framework choice, SKU selection       |
| `boolean`  | Yes/No toggle          | Feature flags, enable/disable options |
| `number`   | Numeric input          | Capacity, port numbers                |

### Parameter Properties

- **type** (required): The input type
- **required**: Whether the parameter must be provided (default: `false`)
- **label**: Short identifier for the parameter
- **displayName**: User-friendly name shown in the UI
- **description**: Help text explaining what the parameter is for
- **default**: Default value if not provided
- **values**: Array of options (required for `enum` type)

## Grouping Questions

Parameters can be organized into **logical groups** using nested YAML structure. Each group represents a section of related questions.

### Example: Multi-Section Template

```yaml
parameters:
    app: # Group 1: Application settings
        framework:
            type: enum
            required: true
            label: Framework
            displayName: Choose Framework
            description: Frontend framework
            values: [vanilla, react, vue, angular]
        description:
            type: textarea
            label: Description
            displayName: Application Description

    database: # Group 2: Database settings
        capacity:
            type: number
            label: Capacity
            displayName: Database Capacity (RU/s)
            default: 400

    backend: # Group 3: Backend settings (with nested groups)
        runtimeStack:
            type: enum
            required: true
            label: Runtime Stack
            values: [NODE|14-lts, DOTNETCORE|3.1, PYTHON|3.8]
        api: # Sub-group
            mainApi: # Deep nesting example
                description:
                    type: textarea
                    label: Main API Description
                enableCaching:
                    type: boolean
                    label: Enable Caching
                    default: true
            billingApi:
                description:
                    type: textarea
                    label: Billing API Description
                paymentGateway:
                    type: enum
                    label: Payment Gateway
                    values: [Stripe, PayPal, Square]
```

## Pulumi Program Integration

The Pulumi program in `pulumi/index.ts` reads template parameters using the Pulumi Config API. Parameters are accessed using **dot notation**, matching the nested structure in `template.yaml`.

### Accessing Parameters

Parameters are flattened into dot-notation keys when passed to the Pulumi program:

```typescript
import * as pulumi from "@pulumi/pulumi";

const config = new pulumi.Config();

// Access simple parameters
const framework = config.require("app.framework"); // Required parameter
const appDescription = config.get("app.description"); // Optional parameter

// Access numeric parameters
const capacity = config.getNumber("database.capacity") || 400;

// Access boolean parameters
const caching = config.getBoolean("backend.api.mainApi.enableCaching") ?? true;

// Access nested parameters
const paymentGateway =
    config.get("backend.api.billingApi.paymentGateway") || "Stripe";
```

### Key Methods

- `config.require(key)` - Get required parameter (throws error if missing)
- `config.get(key)` - Get optional parameter (returns `undefined` if missing)
- `config.getNumber(key)` - Parse as number
- `config.getBoolean(key)` - Parse as boolean

## Complete Example: E-Commerce Template

### template.yaml

```yaml
schemaVersion: 1.0.0
kind: Template
name: ecommerce
displayName: E-commerce Application Template
description: Template for an e-commerce application
parameters:
    app:
        framework:
            type: enum
            required: true
            label: Framework
            displayName: Framework
            description: The framework used for the Static Web App.
            values: [vanilla, react, vue, angular, svelte, nextjs, nuxtjs]
            default: vanilla
        description:
            type: textarea
            required: false
            label: Application Description
            displayName: Application Description
            default: An e-commerce application built with a modern framework.

    capacity:
        type: number
        required: false
        label: Database Capacity (RU/s)
        displayName: Database Capacity (RU/s)
        description: The throughput capacity for the Cosmos DB.
        default: 400

    backend:
        runtimeStack:
            type: enum
            required: true
            label: Runtime Stack
            displayName: Runtime Stack
            description: The runtime stack for the backend API.
            values: [NODE|14-lts, DOTNETCORE|3.1, PYTHON|3.8, JAVA|11]
            default: NODE|14-lts
        api:
            mainApi:
                enableCaching:
                    type: boolean
                    required: false
                    label: Enable Caching
                    displayName: Enable Caching
                    default: true

resources:
    - type: azure.swa
      name: my-ecommerce-app
    - type: azure.cosmosdb
      name: my-ecommerce-db
    - type: azure.azure-app-service
      name: ecommerce-backend
```

### pulumi/index.ts

```typescript
import * as pulumi from "@pulumi/pulumi";

const config = new pulumi.Config();

// Read parameters from template
const framework = config.require("app.framework");
const appDescription = config.get("app.description");
const capacity = config.getNumber("capacity") || 400;
const runtimeStack = config.require("backend.runtimeStack");
const enableCaching =
    config.getBoolean("backend.api.mainApi.enableCaching") ?? true;

pulumi.log.info(`Creating e-commerce app with ${framework} framework`);
pulumi.log.info(`Database capacity: ${capacity} RU/s`);
pulumi.log.info(`Caching enabled: ${enableCaching}`);

// Create resources based on parameters
export const ecommerceConfig = {
    app: { framework, description: appDescription },
    database: { capacity },
    backend: { runtimeStack, cachingEnabled: enableCaching },
};
```

## Best Practices

### 1. **Use Meaningful Names**

- `label`: Short, technical identifier
- `displayName`: User-friendly name for the UI
- `description`: Explain what the parameter does

### 2. **Provide Defaults**

```yaml
dotnetVersion:
    type: enum
    label: .NET Version
    values: ["10.0", "8.0", "7.0"]
    default: "10.0" # Smart defaults improve UX
```

### 3. **Mark Required vs Optional**

```yaml
required: true   # Critical for infrastructure
required: false  # Nice-to-have customizations
```

### 4. **Organize Logically**

For big projects, group related questions into sections and sub-sections for clarity.

## Creating a New Template

### Step 1: Create template directory

```bash
mkdir -p api/pulumiPrograms/templates/{template-name}/pulumi
```

### Step 2: Create template.yaml

Define your parameters and resources following the schema above.

### Step 3: Create Pulumi program

See https://www.pulumi.com/docs/iac/get-started/ for setting up a Pulumi TypeScript project.

### Step 4: Implement index.ts

Read parameters using `config` API and create resources.

## Integration with API

The .NET API automatically discovers templates from the `pulumiPrograms/templates/` directory. When a user creates a project:

1. API loads the `template.yaml` to determine available templates
2. Frontend presents the template questions to the user
3. User answers the questions
4. API executes the Pulumi program with answers as config
5. Infrastructure is provisioned based on the Pulumi code

---

For more information on Pulumi Automation API, see the [Pulumi Documentation](https://www.pulumi.com/docs/).
