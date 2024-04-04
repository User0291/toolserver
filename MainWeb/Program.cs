using MainWeb.Service;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// 注册配置
builder.Configuration.AddJsonFile("appsettings.json");
// 注册 IConfiguration 接口，以便在应用程序中访问配置
builder.Services.AddSingleton(builder.Configuration);


// 构建配置
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

// 添加服务
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddTransient<DatabaseService>();

// 创建服务提供程序
var serviceProvider = services.BuildServiceProvider();

// 获取 DatabaseService 实例
var databaseService = serviceProvider.GetRequiredService<DatabaseService>();

// 输入许可证密钥
// Configure the HTTP request pipeline.
var app = builder.Build();
app.UseAuthorization();

app.MapControllers();

app.Run();
