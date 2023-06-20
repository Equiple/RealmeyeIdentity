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

builder.Services.AddDistributedMemoryCache();

ConfigurationManager config = builder.Configuration;
builder.Services.Configure<UserDatabaseOptions>(config.GetSection("UserDatabase"));
builder.Services.Configure<PasswordOptions>(config.GetSection("Password"));
builder.Services.Configure<RegistrationSessionOptions>(config.GetSection("RegistrationSession"));
builder.Services.Configure<CodeGeneratorOptions>(config.GetSection("CodeGenerator"));
builder.Services.Configure<IdTokenOptions>(config.GetSection("IdToken"));

UserBsonMap.Register();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{action=Login}",
    defaults: new
    {
        controller = "Authentication",
    });

app.Run();
