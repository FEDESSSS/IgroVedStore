using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using IgroVedStore.DataBase;

namespace IgroVedStore
{
    public partial class OrdersWindow : Window
    {
        private readonly OnlineStoreEntities2 _db;
        public ObservableCollection<OrderVM> Orders { get; set; }

        public OrdersWindow()
        {
            InitializeComponent();
            _db = new OnlineStoreEntities2();
            DataContext = this;
            LoadOrders();
            sortComboBox.SelectionChanged += SortComboBox_SelectionChanged;
        }

        private void LoadOrders()
        {
            var orders = _db.Orders
                .Include("Customers")
                .Include("OrderItems.Products")
                .ToList();

            Orders = new ObservableCollection<OrderVM>(
                orders.Select(o => new OrderVM
                {
                    OrderID = o.OrderID,
                    CustomerName = o.Customers?.Name ?? "Неизвестный клиент",
                    OrderDate = o.OrderDate ?? DateTime.Now,
                    TotalAmount = o.TotalAmount ?? 0m,
                    Status = o.Status ?? "Новый",
                    Items = new ObservableCollection<OrderItemVM>(
                        o.OrderItems.Select(oi => new OrderItemVM
                        {
                            ProductName = oi.Products?.ProductName ?? "Неизвестный товар",
                            Quantity = oi.Quantity ?? 0,
                            UnitPrice = oi.UnitPrice ?? 0m,
                            SubTotal = oi.SubTotal ?? 0m,
                            StockQuantity = oi.Products?.StockQuantity ?? 0
                        })
                    ),
                    AllItemsInStock = o.OrderItems.All(oi =>
                        oi.Products != null && oi.Products.StockQuantity >= (oi.Quantity ?? 0) + 3)
                })
            );

            ordersListView.ItemsSource = Orders;
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Orders == null) return;

            IOrderedEnumerable<OrderVM> sorted = null;
            if (sortComboBox.SelectedIndex == 0)
            {
                sorted = Orders.OrderBy(o => o.TotalAmount);
            }
            else if (sortComboBox.SelectedIndex == 1)
            {
                sorted = Orders.OrderByDescending(o => o.TotalAmount);
            }

            if (sorted != null)
            {
                ordersListView.ItemsSource = new ObservableCollection<OrderVM>(sorted);
            }
        }

        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (ordersListView.SelectedItem is OrderVM selectedOrder)
            {
                var statusWindow = new StatusWindow(selectedOrder.Status);
                if (statusWindow.ShowDialog() == true)
                {
                    var order = _db.Orders.Find(selectedOrder.OrderID);
                    if (order != null)
                    {
                        order.Status = statusWindow.NewStatus;
                        _db.SaveChanges();
                        selectedOrder.Status = statusWindow.NewStatus;
                        ordersListView.Items.Refresh();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите заказ для изменения статуса", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Exit_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }
    }

    public class OrderVM
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public ObservableCollection<OrderItemVM> Items { get; set; }
        public bool AllItemsInStock { get; set; }
    }

    public class OrderItemVM
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public int? StockQuantity { get; set; }
    }
}