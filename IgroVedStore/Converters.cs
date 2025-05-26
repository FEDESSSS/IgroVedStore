using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IgroVedStore
{
    public class OrderStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool allItemsInStock)
            {
                return allItemsInStock ? Brushes.LightGreen : Brushes.LightPink;
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StockToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stockQuantity)
            {
                return stockQuantity < 5 ? Brushes.Red : Brushes.Green;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}