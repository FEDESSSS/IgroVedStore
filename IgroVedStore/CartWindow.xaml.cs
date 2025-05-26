using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IgroVedStore.DataBase;

namespace IgroVedStore
{
    public partial class CartWindow : Window, INotifyPropertyChanged
    {
        private readonly OnlineStoreEntities2 _db;
        private int? _customerId;

        public event PropertyChangedEventHandler PropertyChanged;

        public decimal TotalWithoutDiscount => ShoppingCart.Instance.Items.Sum(item =>
            (item.Product.Price ?? 0m) * item.Quantity);

        public decimal DiscountAmount => TotalWithoutDiscount - TotalPrice;

        public decimal TotalPrice => ShoppingCart.Instance.GetTotal();

        public CartWindow(int? customerId = null)
        {
            InitializeComponent();
            _db = new OnlineStoreEntities2();
            _customerId = customerId;
            DataContext = this;

            cartItemsListView.ItemsSource = ShoppingCart.Instance.Items;
            ShoppingCart.Instance.CartUpdated += OnCartUpdated;
        }

        private void OnCartUpdated()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPrice)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiscountAmount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalWithoutDiscount)));
            RefreshCartItems();
        }

        public void RefreshCartItems()
        {
            cartItemsListView.Items.Refresh();
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button button && button.Tag is int productId)
            {
                ShoppingCart.Instance.IncreaseQuantity(productId);
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button button && button.Tag is int productId)
            {
                ShoppingCart.Instance.DecreaseQuantity(productId);
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is Button button && button.Tag is int productId)
            {
                ShoppingCart.Instance.RemoveItem(productId);
            }
        }

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (!ShoppingCart.Instance.Items.Any())
            {
                MessageBox.Show("Корзина пуста", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var order = new Orders
                {
                    CustomerID = _customerId,
                    OrderDate = DateTime.Now,
                    Status = "Новый",
                    TotalAmount = TotalPrice
                };

                _db.Orders.Add(order);

                foreach (var item in ShoppingCart.Instance.Items)
                {
                    _db.OrderItems.Add(new OrderItems
                    {
                        OrderID = order.OrderID,
                        ProductID = item.Product.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price.GetValueOrDefault(),
                        SubTotal = item.Product.Price.GetValueOrDefault() * item.Quantity
                    });
                }

                _db.SaveChanges();
                MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ShoppingCart.Instance.Clear();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            ShoppingCart.Instance.CartUpdated -= OnCartUpdated;
            base.OnClosed(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }

    public class CartItem : INotifyPropertyChanged
    {
        public Products Product { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiscountedPrice)));
            }
        }

        public decimal DiscountPercentage => CalculateDynamicDiscount();

        public bool HasDiscount => DiscountPercentage > 0;

        public decimal DiscountedPrice => (Product.Price ?? 0m) * (1 - DiscountPercentage / 100m);

        private decimal CalculateDynamicDiscount()
        {
            using (var db = new OnlineStoreEntities2())
            {
                // Получаем общую сумму продаж по этому товару
                var totalSales = db.OrderItems
                    .Where(oi => oi.ProductID == Product.ProductID)
                    .Sum(oi => oi.SubTotal) ?? 0m;

                // Получаем максимальную сумму продаж среди всех товаров
                var maxSales = db.OrderItems.Sum(oi => oi.SubTotal) ?? 1m;

                // Рассчитываем процент от максимальных продаж
                decimal percentage = totalSales / maxSales;

                // Применяем логику скидок
                if (percentage > 0.75m) return 15m;
                if (percentage > 0.5m) return 10m;
                if (percentage > 0.25m) return 5m;

                return 0m;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public sealed class ShoppingCart
    {
        private static readonly Lazy<ShoppingCart> _instance =
            new Lazy<ShoppingCart>(() => new ShoppingCart());

        public static ShoppingCart Instance => _instance.Value;

        public ObservableCollection<CartItem> Items { get; }
        public event Action CartUpdated;

        private ShoppingCart()
        {
            Items = new ObservableCollection<CartItem>();
        }

        public decimal GetTotal()
        {
            decimal total = 0;
            foreach (var item in Items)
            {
                total += item.DiscountedPrice * item.Quantity;
            }
            return total;
        }

        public void AddItem(Products product)
        {
            var existingItem = Items.FirstOrDefault(i => i.Product.ProductID == product.ProductID);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                Items.Add(new CartItem { Product = product, Quantity = 1 });
            }
            CartUpdated?.Invoke();
        }

        public void RemoveItem(int productId)
        {
            var item = Items.FirstOrDefault(i => i.Product.ProductID == productId);
            if (item != null)
            {
                Items.Remove(item);
                CartUpdated?.Invoke();
            }
        }

        public void IncreaseQuantity(int productId)
        {
            var item = Items.FirstOrDefault(i => i.Product.ProductID == productId);
            if (item != null)
            {
                item.Quantity++;
                CartUpdated?.Invoke();
            }
        }

        public void DecreaseQuantity(int productId)
        {
            var item = Items.FirstOrDefault(i => i.Product.ProductID == productId);
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    Items.Remove(item);
                }
                CartUpdated?.Invoke();
            }
        }

        public void UpdateQuantity(int productId, int newQuantity)
        {
            var item = Items.FirstOrDefault(i => i.Product.ProductID == productId);
            if (item != null)
            {
                item.Quantity = newQuantity;
                CartUpdated?.Invoke();
            }
        }

        public void Clear()
        {
            Items.Clear();
            CartUpdated?.Invoke();
        }
    }
}