using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.IO;
using System.ComponentModel;

namespace angoukun
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Contact> contacts = null;
        string cacheFile = "";

        private void loadCache()
        {
            char[] del = { ',' };
            string line;
            contacts = new();
            if (File.Exists(cacheFile))
            {
                using (StreamReader fr = new(cacheFile))
                {
                    while ((line = fr.ReadLine()) != null)
                    {
                        var items = line.Split(del, StringSplitOptions.RemoveEmptyEntries);
                        if (items.Length == 3)
                        {
                            items[2] = items[2].Replace("<BR>", "\n");
                            this.contacts.Add(new Contact(items[0], items[1], items[2]));
                        }
                    }
                }
            }
            
        }

        public MainWindow()
        {
            InitializeComponent();
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cacheDir = System.IO.Path.Combine(appdata, Constants.Vendor, Constants.AppName);
            this.cacheFile = System.IO.Path.Combine(cacheDir, Constants.CacheName);
            Directory.CreateDirectory(cacheDir);
            this.loadCache();
        }

        public void saveCache(object sender, CancelEventArgs e)
        {
            using(StreamWriter fw = new(cacheFile))
            {
                foreach(var item in  contacts)
                {
                    item.PubKey = item.PubKey.Replace("\r", "");
                    item.PubKey = item.PubKey.Replace("\n", "<BR>");
                    fw.WriteLine(item.FirstName + "," + item.LastName + "," + item.PubKey);
                }
            }
        }

        private void OpenContacts(object sender, RoutedEventArgs e)
        {
            Contacts contacts = new(this.contacts);
            //contacts.SetContacts(this.contacts);
            contacts.Title = "Contacts";
            contacts.Width = 640;
            contacts.Height = 480;
            contacts.ShowDialog();

            return;
        }
    }
}