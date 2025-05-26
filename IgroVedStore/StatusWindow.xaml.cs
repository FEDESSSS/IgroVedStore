using System.Windows;

namespace IgroVedStore
{
    public partial class StatusWindow : Window
    {
        public string NewStatus { get; private set; }
        private readonly string[] _availableStatuses = { "Новый", "В обработке", "Отправлен", "Доставлен", "Отменен" };

        public StatusWindow(string currentStatus)
        {
            InitializeComponent();
            NewStatus = currentStatus;
            statusComboBox.ItemsSource = _availableStatuses;
            statusComboBox.SelectedItem = currentStatus;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            NewStatus = statusComboBox.SelectedItem as string;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}