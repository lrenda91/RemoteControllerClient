using MahApps.Metro.Controls;

namespace Client
{
    public partial class ProgressWindow : MetroWindow
    {
        public ProgressWindow(string file, long totLength)
        {
            InitializeComponent();
        }

        public string Message
        {
            get
            {
                return titleLabel.Content.ToString();
            }
            set
            {
                titleLabel.Content = value;
            }
        }
    }
}
