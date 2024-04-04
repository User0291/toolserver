using MainWeb;
using System.Configuration;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// 注册配置
builder.Configuration.AddJsonFile("appsettings.json");

// 注册服务
builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();

var app = builder.Build();

// 配置 HTTP 请求管道
app.UseAuthorization();
app.MapControllers();
app.Run();
