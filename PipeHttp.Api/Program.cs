
using PipeHttp.Api;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddScoped<CallParallelService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/call", async (CallParallelService callParallelService) =>
{
    return await callParallelService.Execute();
})
.WithName("get-call")
.WithOpenApi();

app.MapPost("/match", (JsoNataRequest request) =>
{
    var itemSerialized = JsonSerializer.Serialize(request.Data);
    JToken data = JToken.Parse(itemSerialized);

    JsonataQuery query = new JsonataQuery(request.Instruction);

      JToken result = query.Eval(data);

    return result.ToFlatString();
})
.WithName("get-match")
.WithOpenApi();

app.Run();



