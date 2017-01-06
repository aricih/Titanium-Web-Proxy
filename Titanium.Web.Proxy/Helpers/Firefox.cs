using System;
using System.IO;
using System.Linq;

namespace Titanium.Web.Proxy.Helpers
{
    /// <summary>
    /// A helper class to set proxy settings for firefox
    /// </summary>
    public class FireFoxProxySettingsManager
    {
        private const string FirefoxPreferencesProxyDisableConfiguration = "user_pref(\"network.proxy.type\", 0);";

        private void WriteToPreferencesFile(DirectoryInfo firefoxProfileDirectory, Func<string, string> contentTransformer)
        {
            var firefoxPreferences = firefoxProfileDirectory.FullName + "\\prefs.js";

            if (!File.Exists(firefoxPreferences))
            {
                return;
            }

            using (var preferencesReader = new StreamReader(firefoxPreferences))
            {
                var preferencesContent = preferencesReader.ReadToEnd();
                preferencesReader.Close();

                var newContent = contentTransformer(preferencesContent);

                // string.IsNullOrEmpty is avoided deliberetly because string.Empty could be a valid configuration file.
                if (newContent == null)
                {
                    return;
                }

                File.Delete(firefoxPreferences);
                File.WriteAllText(firefoxPreferences, newContent);
            }
        }

        /// <summary>
        /// Enables proxy in firefox preferences config file.
        /// </summary>
        /// <param name="firefoxProfileDirectory">The firefox profile directory.</param>
        private void EnableProxy(DirectoryInfo firefoxProfileDirectory)
        {
            WriteToPreferencesFile(firefoxProfileDirectory, 
                content => !content.Contains(FirefoxPreferencesProxyDisableConfiguration) 
                    ? null 
                    : content.Replace(FirefoxPreferencesProxyDisableConfiguration, string.Empty));
        }

        /// <summary>
        /// Disables proxy in firefox preferences config file.
        /// </summary>
        /// <param name="firefoxProfileDirectory">The firefox profile directory.</param>
        private void DisableProxy(DirectoryInfo firefoxProfileDirectory)
        {
            WriteToPreferencesFile(firefoxProfileDirectory,
                content => content.Contains(FirefoxPreferencesProxyDisableConfiguration)
                    ? null
                    : string.Concat(content, $"\r\n{FirefoxPreferencesProxyDisableConfiguration}"));
        }

        public void AddFirefox()
        {
            try
            {
                var firefoxProfileDirectory =
                    new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                      "\\Mozilla\\Firefox\\Profiles\\").GetDirectories("*.default").FirstOrDefault();

                var firefoxDevEditionProfileDirectory =
                    new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                      "\\Mozilla\\Firefox\\Profiles\\").GetDirectories("*.dev-edition-default").FirstOrDefault();

                if (firefoxProfileDirectory != null)
                {
                    EnableProxy(firefoxProfileDirectory);
                }

                if (firefoxDevEditionProfileDirectory != null)
                {
                    EnableProxy(firefoxDevEditionProfileDirectory);
                }
            }
            catch (Exception)
            {
                // Only exception should be a read/write error because the user opened up FireFox so they can be ignored.
            }
        }

        public  void RemoveFirefox()
        {
            try
            {
                var firefoxProfileDirectory =
                    new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                      "\\Mozilla\\Firefox\\Profiles\\").GetDirectories("*.default").FirstOrDefault();

                var firefoxDevEditionProfileDirectory =
                    new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                      "\\Mozilla\\Firefox\\Profiles\\").GetDirectories("*.dev-edition-default").FirstOrDefault();

                if (firefoxProfileDirectory != null)
                {
                    DisableProxy(firefoxProfileDirectory);
                }

                if (firefoxDevEditionProfileDirectory != null)
                {
                    DisableProxy(firefoxDevEditionProfileDirectory);
                }
            }
            catch (Exception)
            {
                // Only exception should be a read/write error because the user opened up FireFox so they can be ignored.
            }
        }
    }
}