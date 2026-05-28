#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.15

// Interactive project bootstrapper. Demonstrates each Prompt.* helper.
// In CI / non-interactive shells, set CADENZA_PROMPT_<QUESTION_KEY> to
// inject answers (see Prompt's XML doc for the key derivation rules).

var name = Prompt.Text("Project name", defaultValue: "my-app");
var lang = Prompt.Select("Language", new[] { "C#", "F#", "VB" });
var withGit = Prompt.Confirm("Initialize git repository?", defaultValue: true);

WriteLine();
WriteLine("Configuration");
WriteLine($"  name : {name}");
WriteLine($"  lang : {new[] { "C#", "F#", "VB" }[lang]}");
WriteLine($"  git  : {(withGit ? "yes" : "no")}");

if (!Prompt.Confirm("Create project with these settings?", defaultValue: true))
{
    WriteLine("Aborted.");
    Env.Exit(0);
}

MakeDir(name);
WriteText(Path.Combine(name, "README.md"), $"# {name}\n");
if (withGit)
    Run($"git init {name}", throwOnError: true);

WriteLine($"Done. Project ready at ./{name}");
