content = open("src/NexusAI.Api/Program.cs", "r", encoding="utf-8").read()

old = """builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));"""

new = """builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// OpenTelemetry — traces every agent session, HTTP call, and DB query
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("NexusAI")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());"""

content = content.replace(old, new)

# Add using at top
if "using OpenTelemetry" not in content:
    content = "using OpenTelemetry.Trace;\n" + content

open("src/NexusAI.Api/Program.cs", "w", encoding="utf-8").write(content)
print("Done")
