using RealmeyeIdentity.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ICodeGenerator, CodeGenerator>();
builder.Services.AddSingleton<IRealmeyeService, RealmeyeService>();

builder.Services.AddDistributedMemoryCache();

ConfigurationManager config = builder.Configuration;
builder.Services.Configure<UserDatabaseOptions>(config.GetSection("UserDatabase"));
builder.Services.Configure<PasswordOptions>(config.GetSection("Password"));
builder.Services.Configure<AuthenticationOptions>(config.GetSection("Authentication"));
builder.Services.Configure<CodeGeneratorOptions>(config.GetSection("CodeGenerator"));

UserBsonMap.Register();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    //app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}
else
{
    app.UseCors(builder => builder
        .WithOrigins("https://equiple.net")
        .AllowAnyMethod()
        .AllowAnyHeader());
}

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{action=Login}",
    defaults: new
    {
        controller = "Authentication",
    });

app.Run();
