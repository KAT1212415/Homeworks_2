//using ClassLibrary15.ViewModels;
//using ClassLibrary15.Abstractions;
//using ClassLibrary15.Services;
//using ClassLibrary15.Views;
//using Microsoft.Extensions.DependencyInjection;
//using RxBim.Di;

//namespace ClassLibrary15
//{
//    internal class Config : ICommandConfiguration
//    {
//        public void Configure(IServiceCollection services)
//        {
//            services.AddSingleton<IPlacementService, PlacementService>();
//            services.AddSingleton<MainWindowViewModel, MainWindowViewModel>();
//            services.AddSingleton<MainWindow, MainWindow>();
//        }
//    }
//}

using ClassLibrary15.Abstractions;
using ClassLibrary15.Services;
using ClassLibrary15.ViewModels;
using RxBim.Di;

namespace ClassLibrary15
{
    internal class Config : IPluginConfiguration
    {
        public void Configure(IContainer container)
        {
            // Методы расширения RxBim
            container.AddSingleton<IPlacementService, PlacementService>();
            container.AddSingleton<MainWindowViewModel>();
        }
    }
}