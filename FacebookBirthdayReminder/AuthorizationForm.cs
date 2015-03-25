using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Facebook;

namespace FacebookBirthdayReminder
{
	public partial class AuthorizationForm : Form
	{
		#region Data Members
		public FacebookClient FacebookClient = null;
		public FacebookOAuthResult Result = null;
		public string ResultUrl = null;
		public Uri LoginUrl = null;
		#endregion

		#region Constructor
		public AuthorizationForm(FacebookClient facebookClient, dynamic parameters)
		{
			InitializeComponent();
			FacebookClient = facebookClient;
			LoginUrl = facebookClient.GetLoginUrl(parameters);
		}
		#endregion

		#region private void AuthorizationForm_Load(object sender, EventArgs e)
		private void AuthorizationForm_Load(object sender, EventArgs e)
		{
			webBrowser1.Navigate(LoginUrl);
		}
		#endregion

		#region public InternetProxy CurrentProxy
		public InternetProxy CurrentProxy
		{
			get
			{
				return webBrowser1.Proxy;
			}
			set
			{
				webBrowser1.Proxy = value;
			}
		}
		#endregion

		#region void timer1_Tick(object sender, EventArgs e)
		void timer1_Tick(object sender, EventArgs e)
		{
			this.Close();
		}
		#endregion

		#region private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			if (FacebookClient.TryParseOAuthCallbackUrl(e.Url, out Result))
			{
				if (Result.IsSuccess)
				{
					var accesstoken = Result.AccessToken;
					ResultUrl = e.Url.OriginalString;
				}
				else
				{
					var errorDescription = Result.ErrorDescription;
					var errorReason = Result.ErrorReason;
				}
				this.DialogResult = System.Windows.Forms.DialogResult.OK;
				timer1.Interval = 1000;
				timer1.Start();
				timer1.Tick += new EventHandler(timer1_Tick);
			}
			else if (webBrowser1.ReadyState == WebBrowserReadyState.Complete && webBrowser1.DocumentTitle.Contains("Canceled"))
			{
				this.DialogResult = System.Windows.Forms.DialogResult.Retry;
				timer1.Interval = 1000;
				timer1.Start();
				timer1.Tick += new EventHandler(timer1_Tick);
			}
		}
		#endregion
	}
}
