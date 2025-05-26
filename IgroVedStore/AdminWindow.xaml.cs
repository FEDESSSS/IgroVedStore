using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IgroVedStore.DataBase;
using System.Data.Entity;
using System.Globalization;
using System.Windows.Data;

namespace IgroVedStore
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DiscountToStrikethroughConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasDiscount && hasDiscount)
            {
                return TextDecorations.Strikethrough;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class AdminWindow : Window
    {
        public ObservableCollection<ProductVM> Products { get; set; }
        public int? Role { get; set; }
        private readonly OnlineStoreEntities2 db;
        private CartWindow _cartWindow;

        public AdminWindow(int? role)
        {
            try
            {
                InitializeComponent();
                db = new OnlineStoreEntities2();
                DataContext = this;
                Role = role;
                LoadProductsFromDatabase();
                InitializeBrandFilter();

                this.Loaded += AdminWindow_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации окна: {ex.Message}");
                Products = new ObservableCollection<ProductVM>();
            }
        }

        private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Role.HasValue)
                {
                    MessageBox.Show("Роль пользователя не определена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                this.Title += $" (Роль: {Role.Value})";

                switch (Role.Value)
                {
                    case 1: break;
                    case 2:
                        deleteButton.Visibility = Visibility.Collapsed;
                        break;
                    case 3:
                        deleteButton.Visibility = Visibility.Collapsed;
                        addButton.Visibility = Visibility.Collapsed;
                        editButton.Visibility = Visibility.Collapsed;
                        orderButton.Visibility = Visibility.Collapsed;
                        break;
                    default:
                        MessageBox.Show("Неизвестная роль пользователя", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке окна: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProductsFromDatabase()
        {
            try
            {
                var rawProducts = db.Products.ToList();
                var maxSalesAmount = db.OrderItems.Sum(i => i.SubTotal) ?? 1m;

                Products = new ObservableCollection<ProductVM>(
                    rawProducts.Select(product =>
                    {
                        if (product == null) return null;

                        // Рассчитываем общую сумму продаж для этого товара
                        var productSales = db.OrderItems
                            .Where(oi => oi.ProductID == product.ProductID)
                            .Sum(oi => oi.SubTotal) ?? 0m;

                        var discount = CalculateDiscount(productSales, maxSalesAmount);

                        return new ProductVM
                        {
                            ProductID = product.ProductID,
                            ProductName = product.ProductName,
                            Brand = product.Brand,
                            Price = product.Price ?? 0m,
                            Image = GetImageSource(product.Image),
                            DiscountPercentage = discount
                        };
                    }).Where(p => p != null)
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке продуктов: {ex.Message}");
                Products = new ObservableCollection<ProductVM>();
            }
        }

        private decimal CalculateDiscount(decimal productSales, decimal maxSalesAmount)
        {
            if (maxSalesAmount <= 0) return 0m;

            decimal percentage = productSales / maxSalesAmount;

            if (percentage > 0.75m) return 15m;
            if (percentage > 0.5m) return 10m;
            if (percentage > 0.25m) return 5m;

            return 0m;
        }

        private ImageSource GetImageSource(byte[] imageBinary)
        {
            try
            {
                if (imageBinary == null || imageBinary.Length == 0)
                {
                    return new BitmapImage(new Uri("/image/picture.png", UriKind.Relative));
                }

                using (MemoryStream stream = new MemoryStream(imageBinary))
                {
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.StreamSource = stream;
                    img.EndInit();
                    return img;
                }
            }
            catch
            {
                return new BitmapImage(new Uri("/image/picture.png", UriKind.Relative));
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Products == null) return;

                var selectedBrand = brandFilterComboBox.SelectedItem?.ToString();
                var filteredProducts = selectedBrand == "Все бренды" || string.IsNullOrEmpty(selectedBrand)
                    ? Products
                    : new ObservableCollection<ProductVM>(Products.Where(p => p.Brand == selectedBrand));

                switch (sortComboBox.SelectedIndex)
                {
                    case 0:
                        productsListView.ItemsSource = new ObservableCollection<ProductVM>(filteredProducts.OrderBy(p => p.Price));
                        break;
                    case 1:
                        productsListView.ItemsSource = new ObservableCollection<ProductVM>(filteredProducts.OrderByDescending(p => p.Price));
                        break;
                    default:
                        productsListView.ItemsSource = filteredProducts;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сортировке: {ex.Message}");
            }
        }

        private void BrandFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Products == null || brandFilterComboBox.SelectedIndex == -1) return;

                var selectedBrand = brandFilterComboBox.SelectedItem.ToString();
                var filteredProducts = selectedBrand == "Все бренды"
                    ? Products
                    : new ObservableCollection<ProductVM>(Products.Where(p => p.Brand == selectedBrand));

                productsListView.ItemsSource = filteredProducts;

                if (sortComboBox.SelectedIndex != -1)
                {
                    SortComboBox_SelectionChanged(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}");
            }
        }

        private void InitializeBrandFilter()
        {
            try
            {
                brandFilterComboBox.Items.Clear();
                brandFilterComboBox.Items.Add("Все бренды");

                var brands = db.Products.Select(p => p.Brand).Distinct().ToList();
                foreach (var brand in brands)
                {
                    brandFilterComboBox.Items.Add(brand);
                }

                brandFilterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации фильтра брендов: {ex.Message}");
            }
        }

        private void productsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (productsListView.SelectedItem is ProductVM selectedProduct)
            {
                var product = db.Products.Find(selectedProduct.ProductID);
                if (product != null)
                {
                    ShoppingCart.Instance.AddItem(product);

                    if (_cartWindow == null || !_cartWindow.IsLoaded)
                    {
                        _cartWindow = new CartWindow();
                        _cartWindow.Closed += (s, args) => _cartWindow = null;
                        _cartWindow.Show();
                    }
                    else
                    {
                        _cartWindow.Activate();
                        _cartWindow.RefreshCartItems();
                    }
                }
            }
        }

        private void exit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.Close();
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выходе: {ex.Message}");
            }
        }

        private void Cart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_cartWindow == null || !_cartWindow.IsLoaded)
            {
                _cartWindow = new CartWindow();
                _cartWindow.Closed += (s, args) => _cartWindow = null;
                _cartWindow.Show();
            }
            else
            {
                _cartWindow.Activate();
            }
        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            if (Role == 2 || Role == 3)
            {
                MessageBox.Show("У вас нет прав для выполнения этого действия", "Ошибка прав",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProduct = (ProductVM)productsListView.SelectedItem;
            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите товар для редактирования", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new AddWindow(selectedProduct.ProductID);
            editWindow.Closed += (s, args) =>
            {
                if (editWindow.DialogResult == true)
                {
                    LoadProductsFromDatabase();
                }
            };

            try
            {
                editWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна редактирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Role == 2 || Role == 3)
            {
                MessageBox.Show("У вас нет прав для выполнения этого действия", "Ошибка прав",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var selectedProduct = (ProductVM)productsListView.SelectedItem;

                if (selectedProduct == null)
                {
                    MessageBox.Show("Пожалуйста, выберите товар для удаления", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Вы уверены, что хотите удалить товар '{selectedProduct.ProductName}'?",
                                            "Подтверждение удаления",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var productToDelete = db.Products.FirstOrDefault(p => p.ProductID == selectedProduct.ProductID);

                    if (productToDelete != null)
                    {
                        db.Products.Remove(productToDelete);
                        db.SaveChanges();
                        LoadProductsFromDatabase();

                        MessageBox.Show("Товар успешно удален", "Успех",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении товара: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadProductsFromDatabase();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            if (Role == 3)
            {
                MessageBox.Show("У вас нет прав для выполнения этого действия", "Ошибка прав",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var addWindow = new AddWindow();
            addWindow.Closed += (s, args) =>
            {
                if (addWindow.DialogResult == true)
                {
                    LoadProductsFromDatabase();
                }
            };

            try
            {
                addWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна добавления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void orderButton_Click(object sender, RoutedEventArgs e)
        {
            if (Role == 3)
            {
                MessageBox.Show("У вас нет прав для выполнения этого действия", "Ошибка прав",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OrdersWindow ordersWindow = new OrdersWindow();
            ordersWindow.Show();
            


        }
    }

    public class ProductVM
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public ImageSource Image { get; set; }
        public decimal DiscountPercentage { get; set; }

        public decimal FinalPrice => Price * (1 - DiscountPercentage / 100);
        public bool HasDiscount => DiscountPercentage > 0;
        public bool HasHighDiscount => DiscountPercentage > 15;
    }
}