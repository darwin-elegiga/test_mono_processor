ARG REGISTRY=docker.repo1.uhc.com/vpay-docker

FROM ${REGISTRY}/base-images/dotnet/aspnet:6.0-focal AS base
WORKDIR /app
EXPOSE 80

FROM ${REGISTRY}/base-images/dotnet/sdk:6.0-focal AS build
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
