# Documentation

## Introduction

Ce projet est une plateforme interne de développement (Internal Developer Platform, IDP) conçue pour automatiser la création, la configuration et le déploiement d’applications cloud modernes. Il combine un portail frontend (Nuxt 4), une API backend (Minimal API .NET 10) et une infrastructure as code (Pulumi TypeScript pour Azure). L’objectif est de simplifier l’expérience des développeurs : provisionner des ressources cloud, générer des dépôts GitHub/GitLab, et déployer des applications en quelques clics, tout en respectant les bonnes pratiques DevOps et cloud native.

La plateforme orchestre l’ensemble du cycle de vie d’un projet : du choix du template, à la création du dépôt distant, jusqu’au déploiement sur Azure. Elle s’appuie sur des outils modernes (Aspire, Pulumi, CI/CD GitHub Actions) pour garantir rapidité, sécurité et reproductibilité.

Ce document détaille l’architecture, les dépendances, les workflows d’infrastructure et les commandes utiles pour contribuer ou déployer la solution.

## Overview

The project is organized as follows:

```
platform-engineering/
├── app/                       # Frontend Nuxt 4 (Vue 3, Pinia, Nuxt UI)
│   ├── app.config.ts
│   ├── components/            # Vue components
│   ├── config/                # Type-safe config (platforms, resources)
│   ├── pages/                 # Nuxt pages (routing)
│   ├── stores/                # Pinia stores (state management)
│   ├── types/                 # TypeScript types/interfaces
│   └── utils/                 # Helpers, validation, etc.
│   └── ...
├── api/                       # .NET 10 Minimal API (Pulumi Automation)
│   ├── Program.cs             # All endpoints (minimal API style)
│   ├── pulumiPrograms/        # Pulumi programs (TS, for dynamic infra)
│   ├── *.cs                   # API logic, DTOs, services
│   └── ...
├── infrastructure/            # Pulumi TypeScript (Azure infra as code)
│   ├── index.ts               # Main Pulumi program
│   ├── Pulumi.yaml            # Stack metadata
│   ├── Pulumi.dev.yaml        # Dev stack config
│   └── ...
├── PlatformEngineering.AppHost/ # .NET Aspire orchestration (dev only)
│   └── ...
├── .github/
│   └── workflows/             # CI/CD pipelines (ci-build.yaml, cd-deploy.yaml)
├── README.md                  # Project overview
├── doc.md                     # Technical documentation (this file)
└── ...
```

**Main folders:**

- `app/` : Frontend Nuxt 4 (Vue 3, Pinia, Nuxt UI)
- `api/` : .NET 10 Minimal API, Pulumi Automation API
- `infrastructure/` : Pulumi TypeScript for Azure
- `PlatformEngineering.AppHost/` : Local orchestration (Aspire)
- `.github/workflows/` : CI/CD pipelines (GitHub Actions)

## Setup

### Prerequisites

- [Pulumi CLI](https://www.pulumi.com/docs/get-started/install/)
- [Node.js](https://nodejs.org/) (v22 or higher)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [pnpm](https://pnpm.io/)

### Install Dependencies

```bash
cd infrastructure
pnpm install
```

# Infrastructure As code

### Pulumi Configuration

```bash
pulumi config set --secret PULUMI_ACCESS_TOKEN <your-pulumi-access-token>
pulumi config set --secret GithubToken <your-github-token>
pulumi config set --secret GitLabToken <your-gitlab-token>
pulumi config set --secret GitHubOrganizationName <your-github-org>
pulumi config set --secret GitLabBaseUrl <your-gitlab-url>
```

## Provisioning

### Deploy to Development

Déploiement via pipeline CI/CD :

Lancer manuellement le pipeline `infrastructure.yaml` (dans Azure DevOps, GitHub Actions, etc.) pour provisionner l'infrastructure.

Ce pipeline va :

1. Prévisualiser les changements d'infrastructure
2. Demander confirmation (si configuré)
3. Appliquer les changements à votre abonnement Azure

### Destroy Infrastructure

To clean up all resources:

```bash
pulumi destroy
```

## Infrastructure Stack

The Pulumi program provisioning the following Azure resources:

### Static Web App (Frontend)

- Hosts the Nuxt 4 frontend

### Container App (API)

- Hosts the .NET 10 API

### Container Registry (ACR)

- Stores Docker images

### Azure Key Vault

- Stores secrets and certificates

### Managed Identity

- Provides secure access to resources

## Infrastructure Outputs

The Pulumi program exports the following outputs after deployment:

- **staticWebUrl** : Hostname of the Static Web App (frontend URL)
- **staticWebAppName** : Name of the Static Web App resource
- **backendUrl** : Public URL of the Container App (API endpoint)
- **resourceGroupName** : Name of the Azure Resource Group
- **containerRegistryName** : Name of the Azure Container Registry (ACR)
- **containerAppName** : Name of the Azure Container App (API)
- **acrServer** : Login server URL for the ACR
- **keyVault** : Name of the Azure Key Vault

# CI/CD Pipelines

Le projet utilise **GitHub Actions** pour automatiser la construction, les tests, le déploiement et la gestion de l’infrastructure.

### Secrets requis dans GitHub Actions

Pour que les pipelines CI/CD fonctionnent correctement, les secrets suivants doivent être définis dans les paramètres du repository GitHub (Settings > Secrets and variables > Actions):

- `AZURE_CLIENT_ID` : ID de l’application Azure AD (Service Principal)
- `AZURE_TENANT_ID` : ID du tenant Azure AD
- `AZURE_SUBSCRIPTION_ID` : ID de l’abonnement Azure

Ces secrets sont utilisés pour l’authentification Azure lors du déploiement de l’infrastructure, du backend (API) et du frontend.

### Principaux workflows

- **[.github/workflows/ci-build.yaml](.github/workflows/ci-build.yaml)**
  - Build, test et push des images Docker pour l’API
  - Build et upload du frontend Nuxt
  - Télécharge les outputs d’infrastructure pour configurer les builds

- **[.github/workflows/cd-deploy.yaml](.github/workflows/cd-deploy.yaml)**
  - Déploie l’API sur Azure Container Apps
  - Déploie le frontend sur Azure Static Web Apps
  - Utilise les outputs d’infrastructure générés par Pulumi

### Déclencheurs

- Sur push/pull request sur `main`
- Manuellement (`workflow_dispatch`)
- Après succès du workflow `ci-build`

---

# Backend (.NET 10 API)

L’API est développée en .NET 10 (Minimal API) et s’exécute dans un conteneur Docker.

### Structure principale du backend

- **Program.cs** :
  - Point d’entrée de l’API. Définit tous les endpoints REST (minimal API), configure CORS, la documentation OpenAPI, et orchestre le cycle de création de projet.
  - Gère la logique de routage : `/api/create-project` pour provisionner un projet, `/api/templates` pour lister les templates disponibles.
  - Valide les requêtes et injecte les services nécessaires (dont PulumiService).

- **PulumiService.cs** :
  - Service central pour l’exécution des programmes Pulumi côté backend.
  - Gère la création des dépôts (GitHub/GitLab), l’initialisation des templates, et le provisionnement des ressources cloud via Pulumi Automation API.
  - Assure la gestion des paramètres, l’installation des dépendances, l’exécution et le nettoyage des stacks Pulumi.
  - Fournit des méthodes pour exécuter les programmes Pulumi selon la plateforme ou le template choisi, et pour pousser le code dans le dépôt distant.

### Endpoints principaux

Les endpoints exposés par l’API sont :

- **POST /api/create-project** :
  - Crée un nouveau projet en provisionnant le dépôt distant (GitHub/GitLab) et l’infrastructure cloud associée via Pulumi.
  - Reçoit un objet JSON avec les paramètres du projet, la plateforme cible et le template.
  - Retourne le résultat des actions (URL du repo, endpoints, etc.).

- **GET /api/templates** :
  - Retourne la liste des templates de projet disponibles (dotnet-api, ecommerce, enseirb, etc.).
  - Permet au frontend d’afficher les options de templates lors de la création d’un projet.

> Ces endpoints sont également accessibles sans le préfixe `/api/` pour compatibilité avec Azure Static Web Apps (ex : `/create-project`, `/templates`).

### Variables d’environnement nécessaires (backend)

Pour fonctionner correctement, l’API nécessite les variables d’environnement suivantes (à placer dans `api/.env`) :

- `GITHUB_TOKEN` : Jeton d’accès personnel GitHub pour créer des dépôts.
- `GITHUB_ORGANIZATION_NAME` : Nom de l’organisation GitHub cible.
- `GITLAB_TOKEN` : Jeton d’accès personnel GitLab pour créer des dépôts.
- `GITLAB_BASE_URL` : URL de base de l’instance GitLab (ex : https://gitlab.com).

Ces variables sont utilisées pour l’authentification et la configuration des plateformes lors de la création de projet. Elles doivent être renseignées avant de lancer l’API.

> Voir aussi le fichier d’exemple `api/.env.example` pour le format attendu.

### Exécution locale

**Prérequis** :

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/)
- Variables d’environnement dans `api/.env` (voir `api/.env.example`)

**Lancer en local (hors Docker)** :

```bash
cd api
dotnet run
# API accessible sur http://localhost:5064
```

**Lancer en local avec Docker** :

```bash
cd api
docker build -t platform-engineering-api .
docker run --env-file .env -p 5064:5064 platform-engineering-api
```

### Exécution dans le cloud (via pipelines)

- L’image Docker est construite et poussée dans Azure Container Registry par le workflow [`ci-build.yaml`](.github/workflows/ci-build.yaml).
- Le déploiement sur Azure Container Apps est automatisé par le pipeline [`cd-deploy.yaml`](.github/workflows/cd-deploy.yaml).

### Dockerfile Backend (api/)

Le Dockerfile du backend (dossier api/) prépare un environnement complet pour exécuter l’API .NET et provisionner de l’infrastructure cloud via Pulumi. Voici les principales dépendances installées et leurs raisons :

1. **.NET ASP.NET 10.0** : Image de base pour exécuter l’API .NET.
2. **Pulumi CLI** : Permet à l’API de provisionner dynamiquement des ressources cloud via Pulumi Automation API.
3. **Node.js** : Requis pour exécuter les programmes Pulumi écrits en TypeScript/JavaScript (présents dans pulumiPrograms/).
4. **pnpm** : Gestionnaire de paquets rapide utilisé pour installer les dépendances Node.js des programmes Pulumi.
5. **Azure CLI** : Permet à Pulumi et à l’API d’interagir avec Azure (création de ressources cloud).
6. **Copie du SDK .NET dans l’image finale** : Permet d’exécuter des commandes dotnet SDK à l’exécution (utile pour Pulumi ou d’autres outils .NET).
7. **Configuration des permissions et variables d’environnement** : Pour que l’utilisateur non-root puisse écrire dans /app, /app/.pulumi, /app/.dotnet (nécessaire pour Pulumi et .NET).

Voir le Dockerfile dans api/Dockerfile pour le détail des étapes.

## Pulumi Automation API

La .NET API utilise **Pulumi Automation API** pour provisionner dynamiquement l’infrastructure et les plateformes de code (GitHub, GitLab, etc.).

### Fonctionnement

1. L’utilisateur choisit une plateforme (GitHub, GitLab) et un template de projet via le frontend.
2. L’API orchestre l’exécution d’un programme Pulumi spécifique (dans `api/pulumiPrograms/`) pour créer le dépôt distant, initialiser le code, et provisionner l’infrastructure cloud associée.
3. Les paramètres utilisateur (nom du projet, options, secrets…) sont injectés dans le programme Pulumi.
4. L’API exécute le cycle complet Pulumi (Up/Destroy) et retourne les informations utiles (URL du repo, endpoints, etc.).

### Arborescence des pulumiPrograms

Chaque sous-dossier de `api/pulumiPrograms/` correspond à une plateforme, un type de ressource ou un template provisionnable :

```
api/
└── pulumiPrograms/
    ├── platforms/             # Provisionnement de dépôts GitHub/GitLab
    │   ├── github/
    │   │   └── index.ts
    │   └── gitlab/
    │       └── index.ts
    ├── templates/          # Templates de projets (dotnet-api, ecommerce, etc.)
    │   ├── dotnet-api/
    │   ├── ecommerce/
    │   └── enseirb/
    └── ...                 # Autres ressources ou templates
```

### Templates de projet

Des templates de code (ex : dotnet-api, ecommerce, enseirb…) sont stockés dans `api/pulumiPrograms/templates/` et copiés dans le dépôt distant lors de la création du projet.

---

# Frontend (Nuxt 4)

Le frontend est développé avec Nuxt 4 (Vue 3, Pinia, Nuxt UI).

### Exécution locale

**Prérequis** :

- [Node.js 22+](https://nodejs.org/)
- [pnpm](https://pnpm.io/)

**Lancer en local** :

```bash
cd app
pnpm install
pnpm dev
# Frontend accessible sur http://localhost:3000
```

> L’URL de l’API doit être configurée via la variable d’environnement `NUXT_API_URL` (voir `app/.env` ou config Aspire).

### Exécution dans le cloud (via pipelines)

- Le build Nuxt est généré et uploadé comme artefact par le workflow CI/CD.
- Le déploiement sur Azure Static Web Apps est automatisé par le pipeline [`cd-deploy.yaml`](.github/workflows/cd-deploy.yaml).

---

# Résumé des commandes utiles

| Action                        | Commande locale                      | Pipeline CI/CD               |
| ----------------------------- | ------------------------------------ | ---------------------------- |
| Lancer tout (Aspire)          | `aspire run`                         | -                            |
| Lancer API (.NET)             | `cd api && dotnet run`               | Build/test dans `ci-build`   |
| Lancer API (Docker)           | `docker build/run` dans `api/`       | Build/push dans `ci-build`   |
| Lancer Frontend (Nuxt)        | `cd app && pnpm install && pnpm dev` | Build/upload dans `ci-build` |
| Déployer infra (Pulumi)       | `cd infrastructure && pulumi up`     | Pipeline `infrastructure`    |
| Déployer API/Frontend (Azure) | -                                    | `cd-deploy.yaml`             |

---
