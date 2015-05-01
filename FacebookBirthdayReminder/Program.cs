using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Facebook;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Dynamic;
using System.Net;

namespace FacebookBirthdayReminder
{
	static class Program
	{
		#region Data Members
		private static AuthorizationForm AuthForm = null;
		private static string TokenDir = ".";
		private static string TokenFile = @".\data.bin";
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			TokenDir = string.Format(@"{0}\FacebookBirthdayReminder", appDataPath);
			TokenFile = string.Format(@"{0}\data.bin", TokenDir);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			CheckFacebook();
			Application.Exit();
		}

		#region private static dynamic GetBirthdayGuys(string acessToken)
		private static dynamic GetBirthdayGuys(string acessToken)
		{
			FacebookClient client = new FacebookClient(acessToken);
			DateTime date = DateTime.Today;
			string queryStr = string.Format("SELECT name, birthday FROM user WHERE uid IN (SELECT uid2 FROM friend WHERE uid1 = me()) and substr(birthday_date, 0, 5) = '{0:00}/{1:00}'", date.Month, date.Day);
			dynamic result = client.Get("fql", new { q = queryStr });
			if (result != null)
			{
				return result.data;
			}
			return null;
		}
		#endregion

		#region private static string GetAccessToken()
		private static string GetAccessToken()
		{
			FacebookOAuthResult result = null;
			CFacebookOAuthResult serializableResult = null;
			if (Directory.Exists(TokenDir) == false)
			{
				Directory.CreateDirectory(TokenDir);
			}
			FileStream fileStream = new FileStream(TokenFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 1024);
			BinaryFormatter binaryFormater = new BinaryFormatter();
			try
			{
				serializableResult = (CFacebookOAuthResult)binaryFormater.Deserialize(fileStream);
			}
			catch
			{
			}
			if ((serializableResult == null || serializableResult.ExpiresOn < DateTime.UtcNow))
			{
				FacebookClient facebookClient = new FacebookClient();
				facebookClient.AppId = "202383989812841";
				facebookClient.AppSecret = "9bd50e8aeb2af7469a9d5a49c8d098ae";

				if (AuthForm == null || AuthForm.Result.IsSuccess == false || AuthForm.Result.Expires < DateTime.UtcNow)
				{
					dynamic parameters = new ExpandoObject();
					string[] extendedPermissions = new[] { "friends_birthday", "offline_access" };
					parameters.response_type = "token";
					parameters.display = "popup";
					parameters.redirect_uri = "https://www.facebook.com/connect/login_success.html";

					if (extendedPermissions != null && extendedPermissions.Length > 0)
					{
						string scope = string.Join(",", extendedPermissions);
						parameters.scope = scope;
					}

					int i = 0;
					do
					{
						AuthForm = new AuthorizationForm(facebookClient, parameters);
						if (i > 0)
						{
							AuthForm.CurrentProxy = InternetProxy.ProxyList[i - 1];
						}
						Application.Run(AuthForm);
						i++;
					}
					while (AuthForm.DialogResult == DialogResult.Retry && i <= InternetProxy.ProxyList.Count);

					if (AuthForm.Result != null && AuthForm.Result.IsSuccess == false)
					{
						return null;
					}
				}

				result = AuthForm.Result;
				dynamic newresult = facebookClient.Get("oauth/access_token", new
				{
					client_id = facebookClient.AppId,
					client_secret = facebookClient.AppSecret,
					grant_type = "fb_exchange_token",
					fb_exchange_token = result.AccessToken,
				});

				string access_token = newresult.access_token;
				long secondsToExpiry = newresult.expires;
				DateTime expiresOn = DateTime.UtcNow.AddSeconds(secondsToExpiry);


				serializableResult = new CFacebookOAuthResult(access_token, expiresOn);
				fileStream.Seek(0, SeekOrigin.Begin);
				binaryFormater.Serialize(fileStream, serializableResult);
			}
			fileStream.Close();
			if (serializableResult != null)
			{
				return serializableResult.AccessToken;
			}
			return null;
		}
		#endregion

		#region public static void CheckFacebook()
		public static void CheckFacebook()
		{
            //FacebookClient.SetDefaultHttpWebRequestFactory((Uri uri) =>
            //{
            //    WebRequest webRequest = HttpWebRequest.Create(uri);
            //    HttpWebRequestWrapper wrapper = new HttpWebRequestWrapper(webRequest as HttpWebRequest);
            //    Uri proxyUri = webRequest.Proxy.GetProxy(uri);
            //    WebProxy webProxy = new WebProxy(proxyUri);
            //    webProxy.UseDefaultCredentials = true;
            //    wrapper.Proxy = webProxy;
            //    return wrapper;
            //});

			DialogResult result;
			string acessToken = GetAccessToken();
			if (acessToken == null || acessToken.Trim() == string.Empty)
			{
				return;
			}
			dynamic birthdayGuys = null;

			int i = 0;
			StringBuilder message = new StringBuilder();
			do
			{
				try
				{
					birthdayGuys = GetBirthdayGuys(acessToken);
					break;
				}
				catch (Exception ex)
				{
					if (i < InternetProxy.ProxyList.Count)
					{
						System.Net.WebRequest.DefaultWebProxy = new System.Net.WebProxy(InternetProxy.ProxyList[i].Address);
						i++;
					}
					else
					{
						message.Append("Not able to get the birthdays from Facebook.");
						message.Append(Environment.NewLine);
						message.Append("Exception Message: ");
						message.Append(ex.Message);
						MessageBox.Show(message.ToString(), "Birthday Reminder", MessageBoxButtons.OK, MessageBoxIcon.Error,
							MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
						return;
					}
				}
			}
			while (i <= InternetProxy.ProxyList.Count);

			if (birthdayGuys == null || birthdayGuys.Count <= 0)
			{
				message.Append("No birtdays today.\n");
			}
			else
			{
				message.Append("It's Birthday of ");
				foreach (dynamic guy in birthdayGuys)
				{
					message.Append(guy.name);
					message.Append(", ");
				}
				message.Remove(message.Length - 2, 2);
				message.Append(".\n");
			}
			message.Append("Do you want to visit facebook?");
			result = MessageBox.Show(message.ToString(), "Birthday Reminder", MessageBoxButtons.YesNo, MessageBoxIcon.Information,
				MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
			if (result == DialogResult.Yes)
			{
				Process.Start(@"http:\\www.facebook.com");
			}
		}
		#endregion
	}

	[Serializable]
	class CFacebookOAuthResult : ISerializable
	{
		#region Data Members
		public string AccessToken;
		public DateTime ExpiresOn;
		#endregion

		#region Constructors

		#region public CFacebookOAuthResult(string accessToken, DateTime expiresOn)
		public CFacebookOAuthResult(string accessToken, DateTime expiresOn)
		{
			AccessToken = accessToken;
			ExpiresOn = expiresOn;
		}
		#endregion

		#region private CFacebookOAuthResult(SerializationInfo info, StreamingContext context)
		private CFacebookOAuthResult(SerializationInfo info, StreamingContext context)
		{
			AccessToken = info.GetValue("AccessToken", typeof(CEncryptString)).ToString();
			ExpiresOn = info.GetDateTime("ExpiresOn");
		}
		#endregion

		#endregion

		#region public void GetObjectData(SerializationInfo info, StreamingContext context)
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("AccessToken", new CEncryptString(AccessToken));
			info.AddValue("ExpiresOn", ExpiresOn);
		}
		#endregion
	}

	[Serializable]
	class CEncryptString : ISerializable
	{
		#region Data Members
		private string EncryptedString;
		private string DecryptedString;
		#endregion

		#region Constructors

		#region public CEncryptString(string sourceString)
		public CEncryptString(string sourceString)
		{
			DecryptedString = sourceString;
			if (string.IsNullOrEmpty(sourceString))
			{
				EncryptedString = string.Empty;
				return;
			}
			byte[] sourceData = ASCIIEncoding.ASCII.GetBytes(sourceString);
			if (sourceData != null && sourceData.Length > 0)
			{
				byte[] destData = new byte[sourceData.Length];
				destData[0] = sourceData[0];
				for (int i = 1; i < sourceData.Length; i++)
				{
					destData[i] = (byte)((int)sourceData[i - 1] ^ (int)sourceData[i]);
				}
				EncryptedString = ASCIIEncoding.ASCII.GetString(destData);
			}
			else
			{
				EncryptedString = sourceString;
			}
		}
		#endregion

		#region public CEncryptString(SerializationInfo info, StreamingContext context)
		public CEncryptString(SerializationInfo info, StreamingContext context)
		{
			EncryptedString = info.GetString("EncryptedString");
			byte[] encryptedData = ASCIIEncoding.ASCII.GetBytes(EncryptedString);
			if (encryptedData != null && encryptedData.Length > 0)
			{
				byte[] decryptedData = new byte[encryptedData.Length];
				decryptedData[0] = encryptedData[0];
				for (int i = 1; i < encryptedData.Length; i++)
				{
					decryptedData[i] = (byte)((int)decryptedData[i - 1] ^ (int)encryptedData[i]);
				}
				DecryptedString = ASCIIEncoding.ASCII.GetString(decryptedData);
			}
			else
			{
				DecryptedString = EncryptedString;
			}
		}
		#endregion

		#endregion

		#region public override string ToString()
		public override string ToString()
		{
			return DecryptedString;
		}
		#endregion

		#region public void GetObjectData(SerializationInfo info, StreamingContext context)
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("EncryptedString", EncryptedString);
		}
		#endregion
	}
}
