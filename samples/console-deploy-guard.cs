#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.13

// Pre-flight script for a deploy: refuses to proceed unless the current git
// branch is "main" and the working tree is clean. Then runs build + publish.
//
// Demonstrates: Capture for reading git state, Run for executing build steps,
// Env.Exit for failure paths, Sh.Capture's automatic non-zero throw.

var branch = Capture("git rev-parse --abbrev-ref HEAD").Trim();
if (branch != "main")
{
    WriteLine($"Refusing to deploy from branch '{branch}'. Switch to main first.");
    Env.Exit(1);
}

var dirty = Capture("git status --porcelain").Trim();
if (dirty.Length > 0)
{
    WriteLine("Working tree has uncommitted changes:");
    WriteLine(dirty);
    Env.Exit(1);
}

WriteLine($"Deploying from main @ {Capture("git rev-parse --short HEAD").Trim()}...");

if (Run("dotnet build -c Release") != 0) Env.Exit(2);
if (Run("dotnet publish -c Release -o ./dist") != 0) Env.Exit(3);

WriteLine("Deploy artifacts written to ./dist");
