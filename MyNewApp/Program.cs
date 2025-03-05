using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITaskSevice>(new IMemoryTaskService());

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
app.Use(async (context, next) => 
{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.Now}] Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.Now}] Finished.");
});

app.MapGet("/todos", (ITaskSevice service) => service.GetTodos());

app.MapGet("/todos/{id}", IResult (int id, ITaskSevice service) => 
{
    var targetTodo = service.GetTodoById(id);
    return targetTodo is null
        ? Results.NotFound()
        : Results.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task, ITaskSevice service) => 
{
    service.AddTodo(task);
    return TypedResults.Created("/todos/{id}", task);
})
.AddEndpointFilter(async (context, next) => 
{
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();

    if (taskArgument.DueDate > DateTime.Now) 
    {
        errors.Add(nameof(Todo.DueDate), ["Due date must be in the past"]);
    }

    if(errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/todos/{id}", (int id, ITaskSevice service) => 
{
    service.DeleteTodo(id);
    return Results.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsComplete);

interface ITaskSevice
{
    List<Todo> GetTodos();
    Todo? GetTodoById(int id);
    Todo AddTodo(Todo task);
    void DeleteTodo(int id);
}

class IMemoryTaskService : ITaskSevice
{
    private readonly List<Todo> _todos =
    [
        new(1, "Learn C#", DateTime.Now.AddDays(1), false),
        new(2, "Build a Blazor app", DateTime.Now.AddDays(2), false),
        new(3, "Publish to Azure", DateTime.Now.AddDays(3), false)
    ];

    public List<Todo> GetTodos() => _todos;

    public Todo? GetTodoById(int id) => _todos.SingleOrDefault(t => t.Id == id);

    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeleteTodo(int id)
    {
        _todos.RemoveAll(t => t.Id == id);
    }
}