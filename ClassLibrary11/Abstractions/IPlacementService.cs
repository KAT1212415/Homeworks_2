using CSharpFunctionalExtensions;
using ClassLibrary11.Models;

namespace ClassLibrary11.Abstractions
{
    public interface IPlacementService
    {
        Result Place(FurnitureType selectedFurnitureType, int count);
    }
}
