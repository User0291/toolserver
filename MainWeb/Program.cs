using MainWeb;
using Microsoft.AspNetCore.SignalR;
using System.Configuration;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// ע������
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// ע�����
builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
// ע�� ILogger<MyHub> ��ʵ��
builder.Services.AddLogging();
builder.Services.AddSingleton<ILogger<MyHub>, Logger<MyHub>>();
builder.Services.Configure<HubOptions>(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // ���ÿͻ��˳�ʱʱ��Ϊ 30 ��
});
builder.Services.AddSignalR(); // ��� SignalR ����
var app = builder.Build();

// ���� HTTP ����ܵ�
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();


app.MapControllers(); // �ڶ���ע�������·��
app.MapHub<MyHub>("/myHub"); // �ڶ���ע�� SignalR ·��

app.Run();