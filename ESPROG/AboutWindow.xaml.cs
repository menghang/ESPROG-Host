using ESPROG.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
