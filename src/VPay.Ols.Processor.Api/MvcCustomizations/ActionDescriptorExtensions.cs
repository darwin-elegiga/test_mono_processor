using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace VPay.Ols.Processor.Api.MvcCustomizations;

public static class ActionDescriptorExtensions
{
    public static ApiVersionModel? GetApiVersion(this ActionDescriptor actionDescriptor) =>
        actionDescriptor?.Properties
            .Where(kvp => ((Type)kvp.Key) == typeof(ApiVersionModel))
            .Select(kvp => kvp.Value as ApiVersionModel)
            .FirstOrDefault();
}
