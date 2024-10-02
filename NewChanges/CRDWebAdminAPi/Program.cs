using CRDWebAdminAPi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Register AuthSession as scoped
builder.Services.AddScoped<AuthSession>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AuthSession>>();
    var config = sp.GetRequiredService<IConfiguration>();
    return new AuthSession(
        config["WebAdmin:BaseUrl"],
        config["WebAdmin:Username"],
        config["WebAdmin:Password"]
    );
});

// Register ServerNames as scoped
builder.Services.AddScoped<ServerNames>();

// Register LogCollector as scoped
builder.Services.AddScoped<LogCollector>();

// Register BlotterScraper as scoped
builder.Services.AddScoped<BlotterScraper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
