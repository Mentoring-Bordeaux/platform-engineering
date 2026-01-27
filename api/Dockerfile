# Base Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-rc.2 AS base
WORKDIR /app

# Install Pulumi CLI and Node.js and Azure CLI (runtime requirements)
USER root
RUN apt-get update && apt-get install -y \
      curl ca-certificates gnupg apt-transport-https \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && . /etc/os-release \
    && curl -sL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /etc/apt/trusted.gpg.d/microsoft.gpg \
    && echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ ${VERSION_CODENAME} main" \
        > /etc/apt/sources.list.d/azure-cli.list \
    && apt-get update \
    && apt-get install -y azure-cli \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Enable pnpm via corepack (Pulumi programs use pnpm)
RUN corepack enable && corepack prepare pnpm@9.0.0 --activate

RUN curl -fsSL https://get.pulumi.com | sh
ENV PATH="/usr/local/bin:${PATH}"

# The install script places Pulumi and bundled language hosts under /root/.pulumi/bin.
# Copy them to a non-root-accessible location so the runtime user can execute
# pulumi-language-nodejs (required for TypeScript Pulumi programs).
RUN cp /root/.pulumi/bin/pulumi* /usr/local/bin/ \
    && chmod 755 /usr/local/bin/pulumi*

# Use a writable Pulumi home for the non-root runtime user.
# This is required so Pulumi can install/find plugins like pulumi-language-nodejs.
ENV PULUMI_HOME=/app/.pulumi
ENV HOME=/app
RUN mkdir -p /app/.pulumi && chown -R ${APP_UID}:0 /app/.pulumi

# Ensure app user can write under /app (Pulumi writes stack files there)
RUN chown -R ${APP_UID}:0 /app

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=5064
EXPOSE 5064


# Install packages for Pulumi programs
FROM node:lts-alpine AS deps
WORKDIR /pulumiPrograms
RUN corepack enable

# Install root pulumiPrograms dependencies
COPY ./pulumiPrograms/package.json ./pulumiPrograms/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

WORKDIR /pulumiPrograms
COPY ./pulumiPrograms ./
RUN find . -name "package.json" -not -path "./node_modules/*" -exec sh -c 'cd "$(dirname "{}")" && pnpm install --frozen-lockfile' \;

# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0.100-rc.2 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["api.csproj", "."]
RUN dotnet restore "./api.csproj"
COPY . .
RUN dotnet build "./api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish Stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# SDK Stage (only to provide dotnet SDK files at runtime)
FROM mcr.microsoft.com/dotnet/sdk:10.0.100-rc.2 AS sdk

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root

# Copy SDK into runtime image
COPY --from=sdk /usr/share/dotnet /usr/share/dotnet

RUN test -d /usr/share/dotnet/sdk \
    && ls -la /usr/share/dotnet/sdk \
    && /usr/share/dotnet/dotnet --list-sdks

# Ensure dotnet host can discover SDKs
RUN mkdir -p /etc/dotnet \
    && echo "/usr/share/dotnet" > /etc/dotnet/install_location \
    && ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet

# Make discovery explicit
ENV DOTNET_ROOT=/usr/share/dotnet \
    PATH="/usr/share/dotnet:${PATH}" \
    DOTNET_CLI_HOME=/app/.dotnet \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

RUN mkdir -p /app/.dotnet \
    && chown -R ${APP_UID}:0 /app/.dotnet

USER ${APP_UID}

# Copy all pulumiPrograms content (including any node_modules) recursively
COPY --from=deps /pulumiPrograms /app/pulumiPrograms
# COPY runs as root and produces root-owned files; fix ownership so the runtime
# non-root user can write temp files (pnpm creates _tmp_* files in the project dir).
USER root
RUN chown -R ${APP_UID}:0 /app/pulumiPrograms
USER ${APP_UID}

ENTRYPOINT ["dotnet", "api.dll"]