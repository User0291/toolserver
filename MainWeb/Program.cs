using MainWeb;
using System.Configuration;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// ע������
builder.Configuration.AddJsonFile("appsettings.json");

// ע�����
builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();

var app = builder.Build();

// ���� HTTP ����ܵ�
app.UseAuthorization();
app.MapControllers();
app.Run();
