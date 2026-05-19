using app.Hubs;
using app.Services;
using final_para.Aspectos;
using final_para.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// Servicios de dominio de la librería final_para (parser comparte regex JSON via singleton).
builder.Services.AddSingleton<ServicioParser>();

// Aspectos: instancia por resolución (transient) para no cruzar suscripciones entre sesiones.
builder.Services.AddTransient<PublisherComputo>(_ => new PublisherComputo(intervalo: 25));
builder.Services.AddTransient<PublisherError>();

// Servicios propios del app.
builder.Services.AddSingleton<ServicioSesion>();
builder.Services.AddSingleton<GeneradorMalla>();
builder.Services.AddScoped<ServicioResolucion>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ProgresoHub>("/hubs/progreso");

app.Run();
