var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddControllersWithViews();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{action=Index}/{id?}",
    defaults: new
    {
        controller = "Authentication",
    });

app.Run();
