using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Settings
    {
        private short firstPort = 2000;

        public short FirstPort
        {
            get
            {
                return firstPort;
            }
            set
            {
                firstPort = value;
            }
        }
    }
}
