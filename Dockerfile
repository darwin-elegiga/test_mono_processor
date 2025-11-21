#######################################
## This Dockerfile requires BuildKit ##
#######################################

## General arguments
ARG REGISTRY=edgeinternal1uhg.optum.com/commpay-vpay-docker-vir/docker/base-images
ARG DOTNET_RUNTIME_VERSION=8.0.21
ARG DOTNET_SDK_VERSION=8.0.403
ARG DOTNET_SDK_VARIANT=jammy
ARG DOTNET_RUNTIME_VARIANT=jammy-db2
ARG BASE_SDK_IMAGE=dotnet/sdk
ARG BASE_RUNTIME_IMAGE=dotnet/aspnet

## Build Stage
FROM ${REGISTRY}/${BASE_SDK_IMAGE}:${DOTNET_SDK_VERSION}-${DOTNET_SDK_VARIANT} as build

## Build stage arguments
ARG CONFIG_PROFILE=Release
ARG PROJECT_DIR
ARG PROJECT_NAME

ENV PROJECT=${PROJECT_DIR}/${PROJECT_NAME}.csproj
WORKDIR /app

COPY nuget.config* ./
COPY *.sln ./

## Copy .csproj files into the correct file structure
SHELL ["/bin/bash", "-O", "globstar", "-c"]
RUN --mount=target=docker_build_context \
cd docker_build_context; \
cp **/*.csproj ../ --parents;
RUN rm -rf docker_build_context
SHELL ["/bin/sh", "-c"]

## Restore project using BuildKit secrets for NuGet authentication
RUN --mount=type=secret,id=jf-token,env=JF_TOKEN \
    --mount=type=secret,id=jf-user,env=JF_USER \
    dotnet restore ${PROJECT}
    
## Copy all files if restore succeeds
COPY . ./
## Publish project without restoring
RUN dotnet publish --no-restore -c ${CONFIG_PROFILE} -o /app/out ${PROJECT}

## New stage used to reduce the size of the final image
FROM ${REGISTRY}/${BASE_RUNTIME_IMAGE}:${DOTNET_RUNTIME_VERSION}-${DOTNET_RUNTIME_VARIANT} AS final
## Final stage arguments
ARG PROJECT_NAME

WORKDIR /app

COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:80

## Create a symlink so we can use exec form entrypoint
RUN ln -s ${PROJECT_NAME}.dll Entrypoint.dll

ENTRYPOINT [ "dotnet", "Entrypoint.dll" ]

## Optionally add image build time
ARG IMAGE_BUILD_TIME
ENV IMAGE_BUILD_TIME ${IMAGE_BUILD_TIME}
