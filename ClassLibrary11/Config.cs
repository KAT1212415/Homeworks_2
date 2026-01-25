using ClassLibrary11.Abstractions;
using ClassLibrary11.Services;
using ClassLibrary11.ViewModels;
using ClassLibrary11.Views;
using Microsoft.Extensions.DependencyInjection;
using RxBim.Di;

namespace ClassLibrary11
{
    internal class Config : IServiceConfiguration  // ← другой интерфейс!
    {
        public void Configure(IServiceCollection services)
        {
            services.AddSingleton<IPlacementService, PlacementService>();
            services.AddSingleton<MainWindowViewModel, MainWindowViewModel>();
            services.AddSingleton<MainWindow, MainWindow>();
        }
    }
}