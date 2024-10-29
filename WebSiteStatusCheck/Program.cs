using WebsiteMonitorApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<WebsiteMonitorService>(); // HttpClient kayıt
builder.Services.AddHostedService<WebsiteMonitorService>();
builder.Services.AddSingleton<NotificationService>();

var app = builder.Build();

app.MapGet("/", () => "Website Monitor App is Running");

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




