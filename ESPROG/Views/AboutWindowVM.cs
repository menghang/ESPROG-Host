using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESPROG.Views
{
    internal class AboutWindowVM : BaseViewModel
    {
        private string version = string.Empty;
        public string Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }
    }
}
