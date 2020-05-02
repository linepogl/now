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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace now
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var screen = SystemParameters.WorkArea;
			this.Top = screen.Height - this.Height - 8;
			this.Left = screen.Width - this.Width - 8;
		}

		private void Window_MouseEnter(object sender, MouseEventArgs e)
		{
			this.Opacity = 1.0D;
		}

		private void Window_MouseLeave(object sender, MouseEventArgs e)
		{
			this.Opacity = 0.9D;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}
	}
}
