using MainWeb;
using Microsoft.AspNetCore.SignalR;
using System.Configuration;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// 注册配置
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 注册服务
builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
// 注册 ILogger<MyHub> 的实例
builder.Services.AddLogging();
builder.Services.AddSingleton<ILogger<MyHub>, Logger<MyHub>>();
builder.Services.Configure<HubOptions>(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // 设置客户端超时时间为 30 秒
});
builder.Services.AddSignalR(); // 添加 SignalR 服务
var app = builder.Build();

// 配置 HTTP 请求管道
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


app.MapControllers(); // 在顶级注册控制器路由
app.MapHub<MyHub>("/myHub"); // 在顶级注册 SignalR 路由

app.Run();