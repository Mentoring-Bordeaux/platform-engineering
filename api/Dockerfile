# Base Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-rc.2 AS base
WORKDIR /app

# Install Pulumi CLI and Node.js
USER root
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean
RUN curl -fsSL https://get.pulumi.com | sh
ENV PATH="/root/.pulumi/bin:${PATH}"
RUN cp /root/.pulumi/bin/pulumi /usr/local/bin/pulumi

# Create a non-root user to run the application
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=5064 
EXPOSE 5064

# Install packages for running Pulumi
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
WORKDIR "/src/."
RUN dotnet build "./api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish Stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final Stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=deps /pulumiPrograms/node_modules /pulumiPrograms/node_modules
ENTRYPOINT ["dotnet", "api.dll"]