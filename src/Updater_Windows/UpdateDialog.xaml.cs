using System.Windows;

namespace Updater_Windows
{
    public partial class UpdateDialog : Window
    {
        public UpdateDialog()
        {
            InitializeComponent();
            DataContext = new ViewModel(OnTop);
        }


        private void OnTop()
        {
            this.Activate();
        }
    }
}