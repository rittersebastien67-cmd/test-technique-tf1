using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using static Architecture.Tests.Solution;

namespace Architecture.Tests;

/// <summary>
/// Nothing is ever deleted: cancellation and closure replace DELETE (AD42, C8, C17).
/// Checked on the endpoints each host actually maps, so minimal APIs and controllers are both covered.
/// </summary>
public sealed class RouteTests
{
    public static TheoryData<string> ApiHosts => new("HrAdministration.Api", "Assistant.Api");

    [Theory]
    [MemberData(nameof(ApiHosts))]
    public void Host_exposes_no_delete_route(string host)
    {
        var endpoints = MappedEndpoints(host);
        Assert.NotEmpty(endpoints);

        var offending = endpoints
            .Where(endpoint => endpoint.RoutePattern.RawText?.TrimStart('/').StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true)
            .Where(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>() is not { } methods
                || methods.HttpMethods.Count == 0
                || methods.HttpMethods.Contains(HttpMethods.Delete, StringComparer.OrdinalIgnoreCase))
            .Select(endpoint => endpoint.RoutePattern.RawText);

        Assert.Empty(offending);
    }

    /// <summary>Every host declares a top-level <c>Program</c>, so the entry point is resolved by assembly.</summary>
    private static List<RouteEndpoint> MappedEndpoints(string host)
    {
        var entryPoint = LoadAssembly(host).GetType("Program", throwOnError: true)!;
        var factoryType = typeof(WebApplicationFactory<>).MakeGenericType(entryPoint);

        using var factory = (IDisposable)Activator.CreateInstance(factoryType)!;
        var services = (IServiceProvider)factoryType.GetProperty(nameof(WebApplicationFactory<object>.Services))!.GetValue(factory)!;

        return services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
    }
}
