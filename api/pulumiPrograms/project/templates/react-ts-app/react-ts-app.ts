import * as azure from "@pulumi/azure-native";
import * as pulumi from "@pulumi/pulumi";
import * as child_process from "child_process";
import * as fs from "fs";
import * as path from "path";

/**
 * Crée le code initial d'une application React TypeScript et le push sur un repo GitHub vide
 * @param repoUrl URL du repo GitHub vide
 * @param localFolder Dossier temporaire pour créer le projet localement
 * @param appName Nom de l'application / dossier du projet
 * @returns Pulumi Output<string> de l'URL du repo
 */
export function createReactRepoInitial(
    repoUrl: string,
    localFolder: string,
    appName: string
) {
    const projectName = appName.toLowerCase().replace(/\s+/g, "-");
    const tmpDir = path.resolve(localFolder, projectName);

    pulumi.log.info(`Initialisation du projet React TS dans ${tmpDir}`);

    if (!fs.existsSync(tmpDir) || fs.readdirSync(tmpDir).length === 0) {
        fs.mkdirSync(tmpDir, { recursive: true });
        child_process.execSync(`npx create-react-app . --template typescript`, {
            cwd: tmpDir,
            stdio: "inherit",
        });
    }
    const gitignorePath = path.join(tmpDir, ".gitignore");
    const gitignoreContent = `
node_modules
build
.env
.DS_Store
`;

    if (!fs.existsSync(gitignorePath)) {
        fs.writeFileSync(gitignorePath, gitignoreContent.trim() + "\n");
    } else {
        const existing = fs.readFileSync(gitignorePath, "utf-8");
        if (!existing.includes("node_modules")) {
            fs.appendFileSync(gitignorePath, "\nnode_modules\n");
        }
        if (!existing.includes("build")) {
            fs.appendFileSync(gitignorePath, "\nbuild\n");
        }
    }

    if (!fs.existsSync(path.join(tmpDir, ".git"))) {
        child_process.execSync(`git init`, { cwd: tmpDir, stdio: "inherit" });
        child_process.execSync(`git branch -M main`, { cwd: tmpDir, stdio: "inherit" });
        child_process.execSync(`git remote add origin ${repoUrl}`, { cwd: tmpDir, stdio: "inherit" });
    }

    try {
        child_process.execSync(`git fetch origin main`, { cwd: tmpDir, stdio: "inherit" });
        child_process.execSync(`git reset --hard origin/main`, { cwd: tmpDir, stdio: "inherit" });
        pulumi.log.info("Repos distant intégré avec succès.");
    } catch {
        pulumi.log.info("Repo distant vide ou inexistant.");
    }

    child_process.execSync(`git add .`, { cwd: tmpDir, stdio: "inherit" });
    try {
        child_process.execSync(`git commit -m "Initial commit React TS"` , {
            cwd: tmpDir,
            stdio: "inherit",
        });
    } catch {
        pulumi.log.info("Aucun changement à committer.");
    }

    child_process.execSync(`git push -u origin main --force`, {
        cwd: tmpDir,
        stdio: "inherit",
    });

    pulumi.log.info(`✅ Projet React TS pushé sur ${repoUrl} (sans node_modules)`);
}


/**
 * Déploie la Static Web App Azure à partir d'un repo GitHub
 * @param name Nom de l'application Azure
 * @param resourceGroupName Nom du Resource Group
 * @param location Région Azure
 * @param repositoryUrl URL du repo GitHub
 * @param branch Branche du repo (default: main)
 */
export function deployStaticWebAppFromRepo(
    name: string,
    resourceGroupName: pulumi.Input<string>,
    location: pulumi.Input<string>,
    repositoryUrl: pulumi.Input<string>,
    branch: pulumi.Input<string> = "main"
): { staticWebApp: azure.web.StaticSite; url: pulumi.Output<string> } {
    const staticWebApp = new azure.web.StaticSite(name, {
        resourceGroupName,
        location,
        sku: { name: "Free", tier: "Free" },
        repositoryUrl,
        branch,
        buildProperties: {
            appLocation: "/",                
            apiLocation: "",                 
            appArtifactLocation: "build",  
            appBuildCommand: "npm run build",
        },
    });

    return {
        staticWebApp,
        url: pulumi.interpolate`https://${staticWebApp.defaultHostname}`,
    };
}
