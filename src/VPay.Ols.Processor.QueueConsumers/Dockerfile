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
WORKDIR /app

COPY ["src/VPay.Ols.Processor.QueueConsumers/VPay.Ols.Processor.QueueConsumers.csproj", "src/VPay.Ols.Processor.QueueConsumers/"]
RUN dotnet restore "src/VPay.Ols.Processor.QueueConsumers/VPay.Ols.Processor.QueueConsumers.csproj"
COPY . .
WORKDIR "/src/source/VPay.Ols.Processor.QueueConsumers"
RUN dotnet build "VPay.Ols.Processor.QueueConsumers.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "VPay.Ols.Processor.QueueConsumers.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "VPay.Ols.Processor.QueueConsumers.dll"]
