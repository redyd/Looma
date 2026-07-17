// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Net.Http.Json;
using Looma.Domain.Core;
using Looma.Domain.IServices;

namespace Looma.Infrastructure.Services;

public sealed class ReportService(HttpClient httpClient, ApiSettings apiSettings) : IReportService
{
    private sealed record ReportRequest(string Content);

    public async Task<Result> SubmitAsync(ReportType type, string content)
    {
        var route = type == ReportType.Suggestion ? "suggestion" : "bug";

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"{apiSettings.BaseUrl}/api/report/{route}",
                new ReportRequest(content));

            if (!response.IsSuccessStatusCode)
                return Result.Failure($"{(int)response.StatusCode} {response.ReasonPhrase}");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
