#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.15

using System.Text.Json.Serialization;

// Fetch GitHub repo metadata as typed JSON. Demonstrates:
//   - Http.GetJson with a source-generated JsonSerializerContext (AOT-clean)
//   - HttpClient.DefaultRequestHeaders via the shared Http.Client singleton
//   - Top-level async (await is legal at the script root)

Http.Client.DefaultRequestHeaders.UserAgent.ParseAdd("cadenza-sample/1.0");

var repo = await Http.GetJson<Repo>(
    "https://api.github.com/repos/dotnet/runtime",
    JsonCtx.Default);

WriteLine($"{repo.full_name}");
WriteLine($"  stars       : {repo.stargazers_count:N0}");
WriteLine($"  forks       : {repo.forks_count:N0}");
WriteLine($"  open issues : {repo.open_issues_count:N0}");
WriteLine($"  description : {repo.description}");

record Repo(
    string full_name,
    string? description,
    int stargazers_count,
    int forks_count,
    int open_issues_count);

[JsonSerializable(typeof(Repo))]
partial class JsonCtx : JsonSerializerContext { }
