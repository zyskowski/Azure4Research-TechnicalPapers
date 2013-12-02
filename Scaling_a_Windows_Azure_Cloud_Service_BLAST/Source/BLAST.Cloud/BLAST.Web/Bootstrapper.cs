using System.Web.Http;
using Microsoft.Practices.Unity;
using RecipeVVM.Mocks;
using RecipeVVM.Base;
using BLAST.ProcessingUnits;
using BLAST.Web.Proxies;
using Microsoft.WindowsAzure;
using Microsoft.ServiceBus;

namespace BLAST.Web
{
    public static class Bootstrapper
    {
        public static void Initialise()
        {
            var container = BuildUnityContainer();

            GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);
        }

        private static IUnityContainer BuildUnityContainer()
        {
            var container = new UnityContainer();

            container.RegisterType<SearchTaskPUProxy, SearchTaskPUProxy>();
            return container;
        }
    }
}