using ClassLibrary11.Models;
using ClassLibrary11.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassLibrary11.ViewModels
{
    internal class FurnitureTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FurnitureType type)
            {
                switch (type)
                {
                    case FurnitureType.Table:
                        return "Стол";
                    case FurnitureType.Chair:
                        return "Стул";
                    case FurnitureType.Cabinet:
                        return "Шкаф";
                    default:
                        return value != null ? value.ToString() : string.Empty;
                }
            }

            return value != null ? value.ToString() : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                switch (str)
                {
                    case "Стол":
                        return FurnitureType.Table;
                    case "Стул":
                        return FurnitureType.Chair;
                    case "Шкаф":
                        return FurnitureType.Cabinet;
                    default:
                        throw new ArgumentException(string.Format("Неизвестный тип мебели: {0}", str));
                }
            }

            throw new ArgumentException("Некорректное значение для конвертации");
        }
    }
}