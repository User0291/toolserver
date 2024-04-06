using MainWeb;
using System.Configuration;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// 注册配置
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 注册服务
builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
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