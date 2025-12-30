
# Build this Dockerfile using the "api" directory as the context

#######################################
## This Dockerfile requires BuildKit ##
#######################################

## Registry configuration
ARG REGISTRY_URL=centraluhg.jfrog.io
ARG REPO_PATH=glb-docker-mcr-docker-20200805-rem

## .NET SDK and Runtime versions
ARG DOTNET_SDK_VERSION=8.0.404
ARG DOTNET_RUNTIME_VERSION=8.0.22

## Variants - Use jammy for .NET 5.0+
ARG DOTNET_SDK_VARIANT=jammy

## Base image names
ARG BASE_SDK_IMAGE=dotnet/sdk
ARG BASE_RUNTIME_IMAGE=dotnet/aspnet

# ------------ Build Stage ------------
FROM ${REGISTRY_URL}/${REPO_PATH}/${BASE_SDK_IMAGE}:${DOTNET_SDK_VERSION}-${DOTNET_SDK_VARIANT} AS build
WORKDIR /app

ARG CONFIG_PROFILE=Release
ARG PROJECT_DIR
ARG PROJECT_NAME

ENV PROJECT=${PROJECT_DIR}/${PROJECT_NAME}.csproj

COPY nuget.config* ./
COPY *.sln ./

# Copy .csproj files into structure
SHELL ["/bin/bash", "-O", "globstar", "-c"]
RUN --mount=target=docker_build_context \
    cd docker_build_context && cp **/*.csproj ../ --parents
RUN rm -rf docker_build_context
SHELL ["/bin/sh", "-c"]

# Restore with secrets for NuGet authentication
RUN --mount=type=secret,id=jf-token,env=JF_TOKEN \
    --mount=type=secret,id=jf-user,env=JF_USER \
    dotnet restore ${PROJECT}

COPY . ./
RUN dotnet publish --no-restore -c ${CONFIG_PROFILE} -o /app/out ${PROJECT}

# ------------ Runtime Stage ------------
FROM ${REGISTRY_URL}/${REPO_PATH}/${BASE_RUNTIME_IMAGE}:${DOTNET_RUNTIME_VERSION} AS final
ARG PROJECT_NAME

WORKDIR /app

# Create vpay group/user for Ubuntu/Jammy variant
RUN groupadd -g 1000 vpay || true && \
    useradd -u 1000 -g vpay -d /home/vpay -m vpay || true

# Copy published output from build stage with correct ownership
COPY --from=build --chown=vpay:vpay /app/out .

# Use port 8080 for app
ENV ASPNETCORE_URLS=http://+:80

# Symlink for flexible entrypoint
RUN ln -s ${PROJECT_NAME}.dll Entrypoint.dll

# Use non-root user
USER vpay

ENTRYPOINT [ "dotnet", "Entrypoint.dll" ]

ARG IMAGE_BUILD_TIME
ENV IMAGE_BUILD_TIME=${IMAGE_BUILD_TIME}
