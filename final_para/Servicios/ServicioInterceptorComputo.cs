using Castle.DynamicProxy;
using final_para.Aspectos;
using final_para.Estado;

namespace final_para.Servicios;

public class ServicioInterceptorComputo : IInterceptor
{
    private readonly PublisherComputo _publisher;

    public ServicioInterceptorComputo(PublisherComputo publisher)
    {
        _publisher = publisher;
    }

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();

        if (invocation.Method.Name == "Resolver" && invocation.ReturnValue is EstadoSolucion estado)
            _publisher.Disparar(estado);
    }
}