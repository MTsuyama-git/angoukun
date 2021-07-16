using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

namespace angoukun
{
    /// <summary>
    /// Contacts.xaml の相互作用ロジック
    /// </summary>
    public partial class Contacts : Window
    {
        ObservableCollection<Contact> contacts;

        public Contacts(ObservableCollection<Contact> contacts)
        {
            InitializeComponent();
            ContactList.ItemsSource = contacts;
            this.contacts = contacts;
            //contacts.Add(new("test", "test", "test"));
        }
        public void SetContacts(ObservableCollection<Contact> contacts)
        {
            ContactList.ItemsSource = contacts;
        }

        public void Update(object sender, RoutedEventArgs e)
        {
            if(ContactList.SelectedItems.Count > 0)
            {
                if (FirstName.Text == "" || LastName.Text == "" || PubKeyPreview.Text == "")
                {
                    MessageBox.Show("FirstName or LastName or PubKey is empty.");
                    return;
                }
                var idx = ContactList.SelectedIndex;
                contacts[idx].FirstName = FirstName.Text;
                contacts[idx].LastName = LastName.Text;
                contacts[idx].PubKey = PubKeyPreview.Text;
            }
            else
            {
                if(FirstName.Text == "" || LastName.Text == "" || PubKeyPreview.Text == "")
                {
                    MessageBox.Show("FirstName or LastName or PubKey is empty.");
                    return;
                }
                else
                {
                    this.contacts.Add(new Contact(FirstName.Text, LastName.Text, PubKeyPreview.Text));
                }
            }
            ContactList.UnselectAll();
            ContactList.Items.Refresh();
            UpdateButton.Content = "Add";
            ClearButton.Content = "Clear";
            return;
        }

        public void Clear(object sender, RoutedEventArgs e)
        {
            if(ContactList.SelectedItems.Count > 0)
            {
                contacts.Remove((Contact)ContactList.SelectedItem);
            }
            ContactList.UnselectAll();
            UpdateButton.Content = "Add";
            ClearButton.Content = "Clear";
            return;
        }

        public void onSelect(object sender, RoutedEventArgs e)
        {
            UpdateButton.Content = "Update";
            ClearButton.Content = "Delete";
            Contact item = (Contact)ContactList.SelectedItem;
            if (item != null)
            {
                FirstName.Text = item.FirstName;
                PubKeyPreview.Text = item.PubKey;
                LastName.Text = item.LastName;
            }
            else
            {
                FirstName.Text = "";
                LastName.Text = "";
                PubKeyPreview.Text = "";
            }
        }

        public void OpenPubkey(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Public key(*.pub)|*.pub|Pem key(*.pem)|*.pem";
            if(dialog.ShowDialog() == true)
            {
                string line;
                string all = "";
                using(StreamReader fr = new(dialog.FileName))
                {
                    while((line = fr.ReadLine()) != null)
                    {
                        if (all != "")
                        {
                            all += "\n";
                        }
                        all += line;
                    }
                    PubKeyPreview.Text = all;
                }

               // MessageBox.Show(dialog.FileName);
            }
        }
    }
}
