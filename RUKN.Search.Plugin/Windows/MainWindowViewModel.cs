using System;
using System.Collections.Generic;
using System.Text;

namespace RUKN.Search.Plugin
{
    public class MainWindowViewModel : ViewModelBase
    {

        private string version;

        public string Version
        {
            get
            {
                return this.version;
            }

            set
            {
                this.version = value;
                this.OnPropertyChanged();
            }
        }
    }
}
