using Castle.DynamicProxy;
using final_para.Aspectos;
using final_para.Estado;

namespace final_para.Servicios;

public class ServicioInterceptorError : IInterceptor
{
    private readonly PublisherError _publisher;

    public ServicioInterceptorError(PublisherError publisher)
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