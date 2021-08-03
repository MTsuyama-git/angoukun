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
using System.Threading;

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

   private class ThreadArgs
        {
            public string FilePath
            {
                get; set;
            }

            public string OutputPath
            {
                get; set;
            }

            public string RSAContent
            {
                get; set;
            }
            public ThreadArgs(string FilePath, string OutputPath, string RSAContent)
            {
                this.FilePath = FilePath;
                this.OutputPath = OutputPath;
                this.RSAContent = RSAContent;
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

        public void ProceedEncryptCore(object Obj)
        {
            ThreadArgs ta = (ThreadArgs)Obj;
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();
            try
            {
                using FileStream inputStream = new(ta.FilePath, FileMode.Open);
                long inputStreamLength = inputStream.Length;
                long totalLoad = 0;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Progress0.Minimum = totalLoad;
                    Progress0.Maximum = inputStreamLength;
                });
                using FileStream outputStream = new(ta.OutputPath, FileMode.Create);
                using BinaryWriter bw = new(outputStream);
                Span<byte> readBuffer = new(new byte[8192]);
                RSACryptoServiceProvider rsa = SSHKeyManager.ReadSSHPublicKeyFromContent(ta.RSAContent);
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
                    totalLoad += readLen;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Progress0.Value = totalLoad;
                    });
                    encrypt.Write(readBuffer.ToArray(), 0, readLen);
                }
                encrypt.FlushFinalBlock();
                encrypt.Close();
                sw.Stop();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Progress0.Value = 0;
                    _ = MessageBox.Show("End Elapsed: " + sw.ElapsedMilliseconds + " ms.");
                    contactList.SelectedItem = null;
                    FilePath.Text = "";
                });
            }
            catch(Exception e)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _ = MessageBox.Show(e.Message);
                    Progress0.Value = 0;
                    contactList.SelectedItem = null;
                    FilePath.Text = "";
                });
            }
        }

        public void ProceedEncrypt(object sender, RoutedEventArgs e)
        {
            if(contactList.SelectedItem == null)
            {
                _ = MessageBox.Show("Please select receipient.");
                return;
            }
            else if(FilePath.Text == "")
            {
                _ = MessageBox.Show("Please select a file to encrypt.");
                return;
            }
            SaveFileDialog dialog = new ();
            dialog.Filter = "AES File(*.aes)|*.aes";
            if (dialog.ShowDialog() == true)
            {
                //MessageBox.Show(dialog.FileName);

                Thread workerThread = new(ProceedEncryptCore);
                workerThread.Start(new ThreadArgs(FilePath.Text, dialog.FileName, ((Contact)contactList.SelectedItem).PubKey));
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
