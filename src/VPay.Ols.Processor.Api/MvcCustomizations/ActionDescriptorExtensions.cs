using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace VPay.Ols.Processor.Api.MvcCustomizations;

public static class ActionDescriptorExtensions
{
    public static ApiVersionModel? GetApiVersion(this ActionDescriptor actionDescriptor) =>
        actionDescriptor?.GetApiVersionMetadata().Map(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);
}
