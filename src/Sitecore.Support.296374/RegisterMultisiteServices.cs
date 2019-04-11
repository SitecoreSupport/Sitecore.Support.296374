using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.XA.Foundation.Multisite.Services;

namespace Sitecore.Support.XA.Foundation.Multisite.Pipelines.IoC
{
  public class RegisterMultisiteServices : IServicesConfigurator
  {
    public void Configure(IServiceCollection serviceCollection)
    {
      serviceCollection.AddSingleton<IPushCloneService, Sitecore.Support.XA.Foundation.Multisite.Services.PushCloneService>();
    }
  }
}