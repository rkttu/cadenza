#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.11

using System.Collections.Concurrent;

// In-memory todo API. Demonstrates:
//   - Get / Post / Put / Delete tier 1 bare names with parameterized routes
//   - JSON binding for record bodies and query parameters
//   - state held in a process-local ConcurrentDictionary (illustration only — replace with a real store in production)
//
// Try it:
//   curl -X POST  -H "Content-Type: application/json" -d '{"title":"buy milk"}' http://localhost:5000/todos
//   curl          http://localhost:5000/todos
//   curl -X PUT   -H "Content-Type: application/json" -d '{"title":"buy milk","done":true}' http://localhost:5000/todos/1
//   curl -X DELETE http://localhost:5000/todos/1

var store = new ConcurrentDictionary<int, Todo>();
var nextId = 0;

Get("/todos", () => store.Values.OrderBy(t => t.Id).ToArray());

Get("/todos/{id:int}", (int id) =>
    store.TryGetValue(id, out var t) ? Results.Ok(t) : Results.NotFound());

Post("/todos", (TodoInput input) =>
{
    var id = Interlocked.Increment(ref nextId);
    var todo = new Todo(id, input.Title, Done: false);
    store[id] = todo;
    return Results.Created($"/todos/{id}", todo);
});

Put("/todos/{id:int}", (int id, TodoInput input) =>
{
    if (!store.ContainsKey(id)) return Results.NotFound();
    var updated = new Todo(id, input.Title, input.Done ?? false);
    store[id] = updated;
    return Results.Ok(updated);
});

Delete("/todos/{id:int}", (int id) =>
    store.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound());

await Run();

record Todo(int Id, string Title, bool Done);
record TodoInput(string Title, bool? Done);
