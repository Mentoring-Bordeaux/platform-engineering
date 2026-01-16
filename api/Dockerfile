# Base Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-rc.2 AS base
WORKDIR /app

# Install Pulumi CLI and Node.js (runtime requirements)
USER root
RUN apt-get update && apt-get install -y curl ca-certificates \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean

# Enable pnpm via corepack (Pulumi programs use pnpm)
RUN corepack enable && corepack prepare pnpm@9.0.0 --activate

RUN curl -fsSL https://get.pulumi.com | sh
ENV PATH="/root/.pulumi/bin:${PATH}"
RUN cp /root/.pulumi/bin/pulumi /usr/local/bin/pulumi

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
COPY ./pulumiPrograms/package.json ./pulumiPrograms/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

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

# Final Stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Pulumi programs where PulumiService expects them
COPY --from=build /src/pulumiPrograms /app/pulumiPrograms

# node_modules must be inside /app/pulumiPrograms for `pulumi install` / runtime
COPY --from=deps /pulumiPrograms/node_modules /app/pulumiPrograms/node_modules

ENTRYPOINT ["dotnet", "api.dll"]