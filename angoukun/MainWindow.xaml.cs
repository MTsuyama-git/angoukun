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
using Microsoft.Win32;
using Utility;
using System.Security.Cryptography;

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
            contactList.ItemsSource = contacts;
        }

        public string passwordCb()
        {
            return "";
        }

        public void ProceedEncrypt(object sender, RoutedEventArgs e)
        {
            if(contactList.SelectedItem == null)
            {
                MessageBox.Show("Please select receipient.");
            }
            else if(FilePath.Text == "")
            {
                MessageBox.Show("Please select a file to encrypt.");
            }
            SaveFileDialog dialog = new ();
            dialog.Filter = "AES File(*.aes)|*.aes";
            if (dialog.ShowDialog() == true)
            {
                //MessageBox.Show(dialog.FileName);
                
                try
                {
                    //MessageBox.Show(((Contact)contactList.SelectedItem).PubKey);
                    using FileStream inputStream = new(FilePath.Text, FileMode.Open);
                    using FileStream outputStream = new(dialog.FileName, FileMode.Create);
                    using BinaryWriter bw = new(outputStream);
                    Span<byte> readBuffer = new(new byte[8192]);
                    RSACryptoServiceProvider rsa = SSHKeyManager.ReadSSHPublicKeyFromContent(((Contact)contactList.SelectedItem).PubKey);
                    byte[] passKey = PassphraseGenerator.Generate(128);
                    byte[] cipherBytes = rsa.Encrypt(passKey, RSAEncryptionPadding.Pkcs1);
                    bw.Write(Utility.ByteConverter.convertToByte((ushort)(cipherBytes.Length & 0xFFFF), Endian.LITTLE));
                    bw.Write(cipherBytes);
                    // generate salt
                    byte[] magic = Encoding.UTF8.GetBytes("Salted__"); // magic
                    byte[] salt = PassphraseGenerator.Generate(8); // PKCS5_SALT_LEN = 8
                    bw.Write(magic);
                    bw.Write(salt);
                    Rfc2898DeriveBytes b = new(passKey, salt, 10000, HashAlgorithmName.SHA256);
                    byte[] keyIv = b.GetBytes(48);
                    Aes encAlg = Aes.Create();
                    encAlg.Key = Misc.BlockCopy(keyIv, 0, 32);
                    encAlg.IV = Misc.BlockCopy(keyIv, 32, 16);
                    int readLen = 0;

                    using CryptoStream encrypt = new(outputStream, encAlg.CreateEncryptor(), CryptoStreamMode.Write);
                    while ((readLen = inputStream.Read(readBuffer)) > 0)
                    {
                        encrypt.Write(readBuffer.ToArray(), 0, readLen);
                    }
                    encrypt.FlushFinalBlock();
                    encrypt.Close();

                }
                catch (Exception exception)
                {
                    _ = MessageBox.Show(exception.Message);
                }
                contactList.SelectedItem = null;
                FilePath.Text = "";
            }
        }
        
        public void ProceedClear(object sender, RoutedEventArgs e)
        {
            FilePath.Text = "";
            contactList.SelectedItem = null;
        }

        public void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new ();
            dialog.Filter = "All Files(*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                FilePath.Text = dialog.FileName;

                // MessageBox.Show(dialog.FileName);
            }
            return;
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