using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel; 
using TheFirstGetTheMilk.Resources;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;  


//// TODO: Add the following using statement. 
//using Microsoft.WindowsAzure.MobileServices; 


namespace TheFirstGetTheMilk
{
    public class TodoItem
    {
        public string Id { get; set; }

        //// TODO: Add the following serialization attribute. 
        [JsonProperty(PropertyName = "text")] 
        public string Text { get; set; }

        //// TODO: Add the following serialization attribute. 
        [JsonProperty(PropertyName = "complete")] 
        public bool Complete { get; set; }
    }

    public partial class MainPage : PhoneApplicationPage
    {
        // TODO: Comment out the following line that defined the in-memory collection. 
        //private ObservableCollection<TodoItem> items = new ObservableCollection<TodoItem>();

        //// MobileServiceCollection implements ObservableCollection for databinding  
        //// TODO: Uncomment the following two lines of code to replace the following collection with todoTable,  
        //// a proxy for the table in SQL Database. 
         private MobileServiceCollection<TodoItem, TodoItem> items; 
        private IMobileServiceTable<TodoItem> todoTable = App.MobileService.GetTable<TodoItem>(); 

        // Constructor 
        public MainPage()
        {
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void InsertTodoItem(TodoItem todoItem)
        {
            // TODO: Delete or comment the following statement; Mobile Services auto-generates the ID. 
            todoItem.Id = Guid.NewGuid().ToString();

            //// This code inserts a new TodoItem into the database. When the operation completes 
            //// and Mobile Services has assigned an Id, the item is added to the CollectionView 
            //// TODO: Mark this method as "async" and uncomment the following statement. 
            await todoTable.InsertAsync(todoItem); 

            items.Add(todoItem);
        }

        private async void RefreshTodoItems()
        {
            //// TODO #1: Mark this method as "async" and uncomment the following statment 
            ///that defines a simple query for all items.  
            items = await todoTable
                .Where(todoItem => todoItem.Complete == false) 
                .ToCollectionAsync(); 

            ListItems.ItemsSource = items;
            this.ButtonSave.IsEnabled = true;
        }


        private MobileServiceUser user;
        private async System.Threading.Tasks.Task Authenticate()
        {
            string message;
            // This sample uses the Facebook provider.
             var provider = "MicrosoftAccount";
            // var provider = "Facebook";
            // var provider = "Google";
            // Provide some additional app-specific security for the encryption.
            byte[] entropy = { 1, 8, 3, 6, 5 };

            // Authorization credential.
            MobileServiceUser user = null;

            // Isolated storage for the app.
            IsolatedStorageSettings settings =
                IsolatedStorageSettings.ApplicationSettings;

            while (user == null)
            {
                // Try to get an existing encrypted credential from isolated storage.                    
                if (settings.Contains(provider))
                {
                    // Get the encrypted byte array, decrypt and deserialize the user.
                    var encryptedUser = settings[provider] as byte[];
                    var userBytes = ProtectedData.Unprotect(encryptedUser, entropy);
                    user = JsonConvert.DeserializeObject<MobileServiceUser>(
                        System.Text.Encoding.Unicode.GetString(userBytes, 0, userBytes.Length));
                }
                if (user != null)
                {
                    // Set the user from the stored credentials.
                    App.MobileService.CurrentUser = user;

                    try
                    {
                        // Try to return an item now to determine if the cached credential has expired.
                        await App.MobileService.GetTable<TodoItem>().Take(1).ToListAsync();
                    }
                    catch (MobileServiceInvalidOperationException ex)
                    {
                        if (ex.Response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            // Remove the credential with the expired token.
                            settings.Remove(provider);
                            user = null;
                            continue;
                        }
                    }
                }
                else
                {
                    try
                    {
                        // Login with the identity provider.
                        user = await App.MobileService
                            .LoginAsync(provider);

                        // Serialize the user into an array of bytes and encrypt with DPAPI.
                        var userBytes = System.Text.Encoding.Unicode
                            .GetBytes(JsonConvert.SerializeObject(user));
                        byte[] encryptedUser = ProtectedData.Protect(userBytes, entropy);

                        // Store the encrypted user credentials in local settings.
                        settings.Add(provider, encryptedUser);
                        settings.Save();
                    }
                    catch (MobileServiceInvalidOperationException ex)
                    {
                        message = "Vous devez vous connecté";
                    }
                }
                message = string.Format("Vous êtes maintenant connecté - {0}", user.UserId);
                MessageBox.Show(message);
            }
        }

        private async void UpdateCheckedTodoItem(TodoItem item)
        {
            //// This code takes a freshly completed TodoItem and updates the database. When the MobileService  
            //// responds, the item is removed from the list. 
            //// TODO: Mark this method as "async" and uncomment the following statement 
            await todoTable.UpdateAsync(item);       
            items.Remove(item);
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshTodoItems();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var todoItem = new TodoItem { Text = TextInput.Text };
            InsertTodoItem(todoItem);
        }

        private void CheckBoxComplete_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            TodoItem item = cb.DataContext as TodoItem;
            item.Complete = true;
            UpdateCheckedTodoItem(item);
        }

  //      protected override void OnNavigatedTo(NavigationEventArgs e)
  //      {
  //          RefreshTodoItems();
  //      }
        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Authenticate();
            RefreshTodoItems();
        }

    } 

}