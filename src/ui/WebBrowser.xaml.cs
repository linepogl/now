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

namespace Now {
	public partial class WebBrowser : Window {
		
		public WebBrowser() {
			InitializeComponent();
		}

		private string _body;
		public string Body {
			get => _body;
			set {
				if (_body != value) {
					_body = value;
					BodyWebBrowser.NavigateToString(
						"<META http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">" +
						value);;
				}
			}
		}

		//private void BodyWebBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e) {
		//	var doc = BodyWebBrowser.Document as HTMLDocument;
		//	var css = doc.createStyleSheet("", 0);
		//	css.cssText = @"body { font: Sergoe, sans-serif; padding: 10px; background:#f5f7fa; -ms-overflow-style:none; overflow:auto;}";
		//}
	}
}
