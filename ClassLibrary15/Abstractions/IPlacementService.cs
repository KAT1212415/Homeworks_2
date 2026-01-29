
﻿using CSharpFunctionalExtensions;
using ClassLibrary15.Models;

namespace ClassLibrary15.Abstractions
{
    public interface IPlacementService
    {
        Result Place(FurnitureType selectedFurnitureType, int count);
    }
}