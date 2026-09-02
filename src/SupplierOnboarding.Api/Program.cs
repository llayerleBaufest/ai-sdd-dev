var builder = WebApplication.CreateBuilder(args);

// T010: manejo uniforme de errores no controlados (ProblemDetails) para cualquier endpoint futuro.
builder.Services.AddProblemDetails();

// T011: única fuente del instante de registro que Application pasará a Domain (research.md punto 10).
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler();

app.Run();
