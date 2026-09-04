#######################################
## This Dockerfile requires BuildKit ##
#######################################

## General arguments
ARG REGISTRY_URL=centraluhg.jfrog.io
ARG REPO_PATH=glb-docker-mcr-docker-20200805-rem
ARG DOTNET_VERSION=10.0

## ***Use for dotnet 5.0 and above***
ARG DOTNET_SDK_VARIANT=noble
ARG DOTNET_RUNTIME_VARIANT=noble
ARG BASE_SDK_IMAGE=dotnet/sdk
ARG BASE_RUNTIME_IMAGE=dotnet/aspnet

## ***Use for dotnet core 3.1 and below***
# ARG DOTNET_SDK_VARIANT=bionic
# ARG DOTNET_RUNTIME_VARIANT=bionic
# ARG BASE_SDK_IMAGE=dotnet/core/sdk
# ARG BASE_RUNTIME_IMAGE=dotnet/core/aspnet

## Build Stage
FROM ${REGISTRY_URL}/${REPO_PATH}/${BASE_SDK_IMAGE}:${DOTNET_VERSION}-${DOTNET_SDK_VARIANT} AS build

## Build stage arguments
ARG CONFIG_PROFILE=Release
ARG PROJECT_DIR
ARG PROJECT_NAME

ENV PROJECT=${PROJECT_DIR}/${PROJECT_NAME}.csproj
WORKDIR /app

COPY nuget.config* ./
COPY global.json* ./
COPY *.sln ./

## Copy .csproj files into the correct file structure
SHELL ["/bin/bash", "-O", "globstar", "-c"]
RUN --mount=target=docker_build_context \
cd docker_build_context; \
cp **/*.csproj ../ --parents;
RUN rm -rf docker_build_context
SHELL ["/bin/sh", "-c"]

## Restore project
RUN --mount=type=secret,id=jf-token,env=JF_TOKEN \
    --mount=type=secret,id=jf-user,env=JF_USER \
    dotnet restore ${PROJECT}
## Copy all files if restore succeeds
COPY . ./
## Publish project without restoring
RUN dotnet publish --no-restore -c ${CONFIG_PROFILE} -o /app/out ${PROJECT}

## New stage used to reduce the size of the final image
FROM ${REGISTRY_URL}/${REPO_PATH}/${BASE_RUNTIME_IMAGE}:${DOTNET_VERSION}-${DOTNET_RUNTIME_VARIANT} AS final
## Final stage arguments
ARG PROJECT_NAME

# Change time zone to central time
RUN ln -fs /usr/share/zoneinfo/America/Chicago /etc/localtime && dpkg-reconfigure -f noninteractive tzdata

WORKDIR /app

COPY --from=build /app/out .
ENV ASPNETCORE_HTTP_PORTS=80

## Create a symlink so we can use exec form entrypoint
RUN ln -s ${PROJECT_NAME}.dll Entrypoint.dll

ENTRYPOINT [ "dotnet", "Entrypoint.dll" ]

## Optionally add image build time
ARG IMAGE_BUILD_TIME
ENV IMAGE_BUILD_TIME ${IMAGE_BUILD_TIME}
