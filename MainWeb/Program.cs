using MainWeb.Service;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// ע������
builder.Configuration.AddJsonFile("appsettings.json");
// ע�� IConfiguration �ӿڣ��Ա���Ӧ�ó����з�������
builder.Services.AddSingleton(builder.Configuration);


// ��������
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

// ��ӷ���
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddTransient<DatabaseService>();

// ���������ṩ����
var serviceProvider = services.BuildServiceProvider();

// ��ȡ DatabaseService ʵ��
var databaseService = serviceProvider.GetRequiredService<DatabaseService>();

// �������֤��Կ
// Configure the HTTP request pipeline.
var app = builder.Build();
app.UseAuthorization();

app.MapControllers();

app.Run();
