using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThingMagic.URA2.ViewModel;

namespace ThingMagic.URA2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ConnectionWizard : Window
    {
        ConnectionWizardVM vm;
        public ConnectionWizard()
        {
            vm = new ConnectionWizardVM();
            InitializeComponent();
            this.DataContext = vm;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        /// <summary>
        /// Open Help File on press of "F1"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                string location = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                location = System.IO.Path.Combine(location, "URAHelp.chm");
                System.Diagnostics.Process.Start(location);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Opening Help File.\n" + ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                //Onlog("Error Opening Help File.\n" + ex.Message);
            }
        }
    }
}
