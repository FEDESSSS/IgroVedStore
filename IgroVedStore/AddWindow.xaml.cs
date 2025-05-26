using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using IgroVedStore.DataBase;
using System.Linq;
using System.Threading.Tasks;

namespace IgroVedStore
{
    public partial class AddWindow : Window
    {
        private readonly OnlineStoreEntities2 _db;
        private Products _product;
        private byte[] _imageBytes;
        private readonly bool _isEditMode;
        public int ProductID { get; set; }

        public AddWindow() : this(0) // Конструктор для добавления нового товара
        {
            _isEditMode = false;
            this.Title = "Добавление нового товара";
        }

        public AddWindow(int productId) // Конструктор для редактирования товара
        {
            InitializeComponent();
            _isEditMode = productId > 0;
            this.Title = _isEditMode ? "Редактирование товара" : "Добавление нового товара";

            ProductID = productId;
            _db = new OnlineStoreEntities2();
            _product = new Products();

            Loaded += AddWindow_Loaded;
        }
        private async void AddWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Загрузка данных для ComboBox в UI потоке
                cmbCategory.ItemsSource = await Task.Run(() => _db.Categories.ToList());
                cmbCategory.DisplayMemberPath = "CategoryName";
                cmbCategory.SelectedValuePath = "CategoryID";

                cmbSuppliers.ItemsSource = await Task.Run(() => _db.Suppliers.ToList());
                cmbSuppliers.DisplayMemberPath = "SupplierName";
                cmbSuppliers.SelectedValuePath = "SupplierID";

                await LoadProduct(ProductID);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
        private async Task LoadProduct(int productId)
        {
            await Task.Run(() =>
            {
                _product = _db.Products.Find(productId);
            });

            if (_product == null)
            {
                MessageBox.Show("Товар не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // Заполняем поля данными товара в UI потоке
            Dispatcher.Invoke(() =>
            {
                txtName.Text = _product.ProductName;
                txtBrand.Text = _product.Brand;
                txtPrice.Text = _product.Price?.ToString();
                txtQuantity.Text = _product.StockQuantity?.ToString();

                // Устанавливаем изображение
                if (_product.Image != null && _product.Image.Length > 0)
                {
                    productImage.Source = LoadImage(_product.Image);
                    _imageBytes = _product.Image;
                }

                // Устанавливаем выбранные значения в ComboBox
                if (_product.Categories != null)
                    cmbCategory.SelectedValue = _product.Categories.CategoryID;

                if (_product.Suppliers != null)
                    cmbSuppliers.SelectedValue = _product.Suppliers.SupplierID;
            });
        }
        

        private BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private void ChangeImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Выберите изображение товара"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    productImage.Source = LoadImage(_imageBytes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Введите название товара", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Обновляем данные товара
                _product.ProductName = txtName.Text;
                _product.Brand = txtBrand.Text;

                // Устанавливаем связи с категорией и поставщиком
                if (cmbCategory.SelectedValue != null)
                    _product.Categories = _db.Categories.Find((int)cmbCategory.SelectedValue);

                if (cmbSuppliers.SelectedValue != null)
                    _product.Suppliers = _db.Suppliers.Find((int)cmbSuppliers.SelectedValue);

                _product.Image = _imageBytes;

                if (decimal.TryParse(txtPrice.Text, out var price))
                    _product.Price = price;
                else
                    _product.Price = null;

                if (int.TryParse(txtQuantity.Text, out var quantity))
                    _product.StockQuantity = quantity;
                else
                    _product.StockQuantity = null;

                // Сохраняем изменения
                _db.SaveChanges();

                MessageBox.Show("Изменения сохранены успешно!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Exit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}