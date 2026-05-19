using aplicacion_prueba.Ejemplos;

string objetivo = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
bool exportCsv = args.Skip(1).Any(a => a is "--csv" or "csv");

switch (objetivo)
{
    case "calor":
        EjemploCalor1D.Ejecutar();
        break;
    case "poisson":
        EjemploPoisson2D.Ejecutar();
        break;
    case "comparar":
        EjemploComparativaFDMvsFEM.Ejecutar();
        break;
    case "tracking":
        EjemploTrackingDual.Ejecutar(exportCsv);
        break;
    case "all":
        EjemploCalor1D.Ejecutar();
        Console.WriteLine();
        EjemploPoisson2D.Ejecutar();
        Console.WriteLine();
        EjemploComparativaFDMvsFEM.Ejecutar();
        Console.WriteLine();
        EjemploTrackingDual.Ejecutar(exportCsv);
        break;
    default:
        Console.Error.WriteLine($"Argumento desconocido: '{objetivo}'.");
        Console.Error.WriteLine("Uso: dotnet run -- [calor|poisson|comparar|tracking|all] [--csv]");
        Environment.Exit(1);
        break;
}
