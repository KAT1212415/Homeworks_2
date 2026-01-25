using Autodesk.Revit.UI;
using ClassLibrary11.Abstractions;
using ClassLibrary11.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSharpFunctionalExtensions;
using System.Collections.ObjectModel;

namespace ClassLibrary11.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly IPlacementService _placementService;
        private FurnitureType _selectedFurnitureType;
        private int _count;
        private string _statusMessage;

        public MainWindowViewModel(IPlacementService placementService)
        {
            PlaceCommand = new RelayCommand(PlaceFurniture);

            FurnitureTypes = new ObservableCollection<FurnitureType>
            {
                FurnitureType.Table,
                FurnitureType.Chair,
                FurnitureType.Cabinet
            };

            _placementService = placementService;
        }

        public ObservableCollection<FurnitureType> FurnitureTypes { get; }

        public FurnitureType SelectedFurnitureType
        {
            get => _selectedFurnitureType;
            set => SetProperty(ref _selectedFurnitureType, value);
        }

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public RelayCommand PlaceCommand { get; }

        private void PlaceFurniture()
        {
            CSharpFunctionalExtensions.Result result = _placementService.Place(SelectedFurnitureType, Count);
            if (result.IsSuccess)
            {
                StatusMessage = $"Размещено {Count} экземпляров мебели";
                TaskDialog.Show("Размещение мебели", StatusMessage);
            }
            else
            {
                StatusMessage = $"Ошибка {result.Error}";
                TaskDialog.Show("Размещение мебели", result.Error);
            }
        }
    }
}
