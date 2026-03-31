using System.Windows;

namespace VeraCom
{
    public partial class CanAdapterDialog : Window
    {
        public ushort SelectedAdapter { get; private set; }

        public CanAdapterDialog(List<ushort> adapters)
        {
            InitializeComponent();
            AdapterList.ItemsSource = adapters;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (AdapterList.SelectedItem != null)
            {
                SelectedAdapter = (ushort)AdapterList.SelectedItem;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Bitte Adapter auswählen");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}