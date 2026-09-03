ARG REGISTRY=docker.repo1.uhc.com/vpay-docker

FROM ${REGISTRY}/base-images/dotnet/runtime:10.0-noble AS base
WORKDIR /app

FROM ${REGISTRY}/base-images/dotnet/sdk:10.0-noble AS build
WORKDIR /source
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