using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDT
{
    public static class UserDirectories
    {
        public static string AppsDirectory = "", DesktopDirectory = "", DownloadsDirectory = "",
            DocumentsDirectory = "", ExplorerDirectory = "", ChromeBookmarksDirectory = "",
            OutlookDirectory = "", SecureEmailDirectory = "", SapDirectory = "";
        //public static void InitializeDirectories(string usersFolderName)
        //{
        //    AppsDirectory = @"C:\Apps";
        //    DesktopDirectory = @"C:\Users\" + usersFolderName + @"\Desktop" ;
        //    DownloadsDirectory = @"C:\Users\" + usersFolderName + @"\Downloads";
        //    DocumentsDirectory = @"HOUIC-NA-V506\" + usersFolderName + @"$\Cached\My Documents";
        //    ExplorerDirectory = @"C:\Users\" + usersFolderName + @"\OneDrive - Shell\Favorites";
        //    ChromeBookmarksDirectory = @"C:\Users\" + usersFolderName + @"\AppData\Local\Google\Chrome\User Data\Default";
        //    OutlookDirectory = @"C:\Users\" + usersFolderName + @"\AppData\Local\Microsoft\Outlook";
        //    SecureEmailDirectory = @"C:\Users\" + usersFolderName + @"\AppData\Roaming\\Microsoft\SystemCertificates";
        //    SapDirectory = @"C:\Windows";
        //}
        public static Dictionary<string, string> InitializeDictionary(string usersFolderName)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>
            {
                { "Apps", @"C:\Apps" },
                { "Desktop", @"C:\Users\" + usersFolderName + @"\Desktop" },
                { "Downloads", @"C:\Users\" + usersFolderName + @"\Downloads" },
                { "IExplorer", @"C:\Users\" + usersFolderName + @"\OneDrive\Favorites" },
                { "Chrome Bookmarks", @"C:\Users\" + usersFolderName + @"\AppData\Local\Google\Chrome\User Data\Default\Bookmarks" },
                { "Outlook", @"C:\Users\" + usersFolderName + @"\AppData\Local\Microsoft\Outlook" },
                { "Secure Email", @"C:\Users\" + usersFolderName + @"\AppData\Roaming\\Microsoft\SystemCertificates" },
                { "Documents", @"C:\Users\" + usersFolderName + @"\OneDrive" }
            };
            return keyValuePairs;
        }
    }
}
