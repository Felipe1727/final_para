using app.Models.ViewModels;
using final_para.Ecuaciones;
using Microsoft.Extensions.Caching.Memory;

namespace app.Services;

/// <summary>
/// Almacena en memoria (IMemoryCache) la ecuación parseada y resultados de resolución
/// asociados a la sesión HTTP del usuario. TTL deslizante de 30 minutos.
/// </summary>
public class ServicioSesion
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan TTL = TimeSpan.FromMinutes(30);

    public ServicioSesion(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void GuardarEcuacion(string sessionId, Ecuacion ecuacion, EcuacionParseadaVM vm)
    {
        _cache.Set(KeyEcuacion(sessionId), ecuacion, OpcionesTTL());
        _cache.Set(KeyEcuacionVM(sessionId), vm, OpcionesTTL());
    }

    public Ecuacion? ObtenerEcuacion(string sessionId) =>
        _cache.TryGetValue(KeyEcuacion(sessionId), out Ecuacion? eq) ? eq : null;

    public EcuacionParseadaVM? ObtenerEcuacionVM(string sessionId) =>
        _cache.TryGetValue(KeyEcuacionVM(sessionId), out EcuacionParseadaVM? vm) ? vm : null;

    public void GuardarConfiguracion(string sessionId, ConfiguracionProblemaVM config)
    {
        _cache.Set(KeyConfiguracion(sessionId), config, OpcionesTTL());
    }

    public ConfiguracionProblemaVM? ObtenerConfiguracion(string sessionId) =>
        _cache.TryGetValue(KeyConfiguracion(sessionId), out ConfiguracionProblemaVM? cfg) ? cfg : null;

    public void GuardarResultado(string sessionId, ResultadoResolucionVM resultado)
    {
        _cache.Set(KeyResultado(sessionId), resultado, OpcionesTTL());
    }

    public ResultadoResolucionVM? ObtenerResultado(string sessionId) =>
        _cache.TryGetValue(KeyResultado(sessionId), out ResultadoResolucionVM? r) ? r : null;

    public void Limpiar(string sessionId)
    {
        _cache.Remove(KeyEcuacion(sessionId));
        _cache.Remove(KeyEcuacionVM(sessionId));
        _cache.Remove(KeyResultado(sessionId));
    }

    private static MemoryCacheEntryOptions OpcionesTTL() =>
        new() { SlidingExpiration = TTL };

    private static string KeyEcuacion(string id) => $"ecuacion:{id}";
    private static string KeyEcuacionVM(string id) => $"ecuacionvm:{id}";
    private static string KeyConfiguracion(string id) => $"configuracion:{id}";
    private static string KeyResultado(string id) => $"resultado:{id}";
}
