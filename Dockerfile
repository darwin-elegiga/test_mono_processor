#######################################
## This Dockerfile requires BuildKit ##
#######################################

## General arguments
#ARG REGISTRY=docker.repo1.uhc.com/vpay-docker
ARG REGISTRY=optum-docker-auth-prod.repo1.uhc.com/chainguard
ARG DOTNET_VERSION=6.0

## ***Use for dotnet 5.0 and above***
ARG DOTNET_SDK_VARIANT=focal
ARG DOTNET_RUNTIME_VARIANT=focal
ARG BASE_SDK_IMAGE=dotnet/sdk
ARG BASE_RUNTIME_IMAGE=dotnet/aspnet

## ***Use for dotnet core 3.1 and below***
# ARG DOTNET_SDK_VARIANT=bionic
# ARG DOTNET_RUNTIME_VARIANT=bionic
# ARG BASE_SDK_IMAGE=dotnet/core/sdk
# ARG BASE_RUNTIME_IMAGE=dotnet/core/aspnet

## Utility stage for common tools
FROM optum-docker.repo1.uhc.com/vpay-docker/base-images/common-tools:v2 as tools

## Build Stage
#FROM ${REGISTRY}/base-images/${BASE_SDK_IMAGE}:${DOTNET_VERSION}-${DOTNET_SDK_VARIANT} as build
FROM optum-docker-auth-prod.repo1.uhc.com/chainguard/dotnet-sdk/dev:latest-dev as build

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

## Restore project
RUN dotnet restore ${PROJECT}
## Copy all files if restore succeeds
COPY . ./

RUN ls -la /app && whoami
USER root
RUN mkdir -p /app && chown -R $(whoami) /app

## Publish project without restoring
RUN dotnet publish --no-restore -c ${CONFIG_PROFILE} -o /app/out ${PROJECT}

## New stage used to reduce the size of the final image
#FROM ${REGISTRY}/base-images/${BASE_RUNTIME_IMAGE}:${DOTNET_VERSION}-${DOTNET_RUNTIME_VARIANT} AS final
FROM optum-docker-auth-prod.repo1.uhc.com/chainguard/aspnet-runtime/dev:6.0-latest-dev AS final
## Final stage arguments
ARG PROJECT_NAME

# Change time zone to central time
#RUN ln -fs /usr/share/zoneinfo/America/Chicago /etc/localtime && dpkg-reconfigure -f noninteractive tzdata
ENV TZ=America/Chicago

WORKDIR /app

COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:80

## Create a symlink so we can use exec form entrypoint
RUN ln -s ${PROJECT_NAME}.dll Entrypoint.dll

ENTRYPOINT [ "dotnet", "Entrypoint.dll" ]

## Optionally add image build time
ARG IMAGE_BUILD_TIME
ENV IMAGE_BUILD_TIME ${IMAGE_BUILD_TIME}
