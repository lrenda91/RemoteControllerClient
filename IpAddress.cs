using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Data;

/**
 * Classe che modella un indirizzo IP espresso come stringa
 * e che implementando l'interfaccia INotifyPropertyChanged
 * permette il binding con la parte grafica
 * 
 **/
namespace Client
{
    public class IpAddress : INotifyPropertyChanged
    {
        private int _idx;
        private String _ipAddress;

        public int idx
        {
            get { return _idx; }
            set
            {
                _idx = value;
                NotifyPropertyChanged("idx");
            }
        }


        public String ipAddress
        {
            get { return _ipAddress; }
            set
            {
                _ipAddress = value;
                NotifyPropertyChanged("ipAddress");
            }
        }

        


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Helpers

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
