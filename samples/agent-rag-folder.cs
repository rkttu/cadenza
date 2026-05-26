#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.12

// Tiny RAG-over-a-folder agent. The LLM gets `search_docs` and `read_doc`
// tools that walk a documents directory (override with DOCS_DIR env var)
// and let the model decide when to retrieve. No embedding store — this is
// a "good enough for a few hundred markdown files" pattern.
//
//   export OPENAI_BASE_URL=http://localhost:8080/v1
//   export OPENAI_API_KEY=any-non-empty-string
//   curl $OPENAI_BASE_URL/chat/completions \
//     -H "Authorization: Bearer $OPENAI_API_KEY" \
//     -H 'Content-Type: application/json' \
//     -d '{"model":"cadenza-agent","messages":[{"role":"user","content":"summarize the readme"}]}'

var docsDir = Env.Get("DOCS_DIR") ?? "./docs";

SystemPrompt($"""
    You are a documentation assistant. Use `search_docs` to find files
    relevant to the user's question, then `read_doc` to load full text
    before answering. Cite filenames in your answer.
    Documents root: {Path.GetFullPath(docsDir)}
    """);

Tool("search_docs", "Find documents whose contents contain the query string. Returns up to 10 matching paths.",
    (string query) =>
    {
        var hits = new List<string>();
        foreach (var file in Glob(Path.Combine(docsDir, "**/*.md")).Take(500))
        {
            try
            {
                if (File.ReadAllText(file).Contains(query, StringComparison.OrdinalIgnoreCase))
                    hits.Add(Path.GetRelativePath(docsDir, file));
                if (hits.Count >= 10) break;
            }
            catch { /* skip unreadable files */ }
        }
        return hits.ToArray();
    });

Tool("read_doc", "Load a document by its path (relative to docs root).",
    (string path) => ReadText(Path.Combine(docsDir, path)));

UseOllama("llama3.2");

await Run();
