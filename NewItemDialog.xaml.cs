using System.Windows;
using System.Windows.Controls;

namespace FileManager
{
    public partial class NewItemDialog : Window
    {
        public string SelectedItemType { get; private set; }
        public string ItemName { get; private set; }

        public NewItemDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ItemNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите имя элемента.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedItemType = ((ComboBoxItem)ItemTypeComboBox.SelectedItem).Content.ToString();
            ItemName = ItemNameTextBox.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ItemNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}