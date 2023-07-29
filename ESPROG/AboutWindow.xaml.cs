using ESPROG.Views;
using System.Windows;

namespace ESPROG
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private readonly AboutWindowVM view;
        public AboutWindow()
        {
            InitializeComponent();
            this.view = new AboutWindowVM();
            this.DataContext = this.view;
        }
        public void SetVersion(string v) => this.view.Version = v;
    }
}
