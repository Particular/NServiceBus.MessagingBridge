namespace NServiceBus
{
    using System.Linq;
    using Hosting.Helpers;
    using Microsoft.Extensions.DependencyInjection;

    static class CustomCheckRegistrar
    {
        internal static IServiceCollection AddCustomChecks(this IServiceCollection services)
        {
            var customChecks = new AssemblyScanner()
                .GetScannableAssemblies()
                .Types
                .Where(type =>
                    (type.IsAssignableFrom(typeof(ICustomCheck)) || type.IsSubclassOf(typeof(CustomCheck)))
                    && !type.Name.Equals("ICustomCheck")
                    && !type.Name.Equals("CustomCheck"));

            foreach (var customCheck in customChecks)
            {
                services.AddTransient(typeof(ICustomCheck), customCheck);
            }

            return services;
        }
    }
}