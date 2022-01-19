namespace VPay.Ols.Processor.Api.Models;

public sealed class AboutInfo
{
    private AboutInfo(
        string gitRevision,
        string buildTime,
        string? environment,
        string versionInfo,
        string dataCenter)
    {
        GitRevision = gitRevision;
        BuildTime = buildTime;
        Environment = environment;
        VersionInfo = versionInfo;
        DataCenter = dataCenter;
    }

    public string BuildName => "OLS Processor";
    public string GitRevision { get; }
    public string BuildTime { get; }
    public string? Environment { get; }
    public string VersionInfo { get; }
    public string DataCenter { get; }

    public static AboutInfo GetBuildAboutInfo() => new(
        gitRevision: System.Environment.GetEnvironmentVariable("RELEASE_COMMIT_SHA") ?? "[[GitRevision]]",
        buildTime: System.Environment.GetEnvironmentVariable("IMAGE_BUILD_TIME") ?? "[[BuildTime]]",
        environment: System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        versionInfo: System.Environment.GetEnvironmentVariable("RELEASE_TAG") ?? "[[VersionInfo]]",
        dataCenter: System.Environment.GetEnvironmentVariable("DEPLOYMENT_DATACENTER") ?? "[[DataCenter]]");
}
