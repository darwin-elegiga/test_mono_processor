ARG REGISTRY=docker.repo1.uhc.com/vpay-docker
ARG DOTNET_VERSION=6.0

ARG DOTNET_SDK_VARIANT=focal
ARG DOTNET_RUNTIME_VARIANT=focal
ARG BASE_SDK_IMAGE=dotnet/sdk
ARG BASE_RUNTIME_IMAGE=dotnet/aspnet

## Build Stage
FROM ${REGISTRY}/base-images/${BASE_SDK_IMAGE}:${DOTNET_VERSION}-${DOTNET_SDK_VARIANT} as build

WORKDIR /app
EXPOSE 80

FROM ${REGISTRY}/base-images/${BASE_SDK_IMAGE}:${DOTNET_VERSION}-${DOTNET_SDK_VARIANT} as build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["src/VPay.Ols.Processor.Api/VPay.Ols.Processor.Api.csproj", "src/VPay.Ols.Processor.Api/"]
RUN dotnet restore "src/VPay.Ols.Processor.Api/VPay.Ols.Processor.Api.csproj"
COPY . .
WORKDIR "/src/src/VPay.Ols.Processor.Api"
RUN dotnet build "VPay.Ols.Processor.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VPay.Ols.Processor.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VPay.Ols.Processor.Api.dll"]
