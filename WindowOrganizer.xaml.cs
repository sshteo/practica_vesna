using practica_vesna;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace practica_vesna
{
    public partial class WindowOrganizer : Window
    {
        private const string PhotoFolderPath = @"D:\resours\organized_import";

        public WindowOrganizer(string lastName, string firstName, string middleName, string gender, string photoFileName)
        {
            InitializeComponent();

            string prefix = gender == "Мужской" ? "Mr" : "Mrs";
            string fullName = $"{lastName} {firstName} {middleName}".Trim();
            labelUserName.Content = $"{prefix} {fullName}";

            SetGreeting();

            LoadPhoto(photoFileName);
        }

        private void SetGreeting()
        {
            int hour = DateTime.Now.Hour;
            string greeting;

            if (hour >= 9 && hour < 11)
                greeting = "Доброе утро";
            else if (hour >= 11 && hour < 18)
                greeting = "Добрый день";
            else
                greeting = "Добрый вечер";

            labelGreeting.Content = greeting + "!";
        }

        private void LoadPhoto(string photoFileName)
        {
            if (string.IsNullOrEmpty(photoFileName))
            {
                // Можно установить фото по умолчанию или очистить
                imgPhoto.Source = null;
                return;
            }

            string fullPath = Path.Combine(PhotoFolderPath, photoFileName);

            if (!File.Exists(fullPath))
            {
                MessageBox.Show($"Файл фото не найден:\n{fullPath}", "Ошибка загрузки фото", MessageBoxButton.OK, MessageBoxImage.Warning);
                imgPhoto.Source = null;
                return;
            }

            try
            {
                BitmapImage bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Для безопасности потоков

                imgPhoto.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фото: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                imgPhoto.Source = null;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var IventsOrganizer = new IventsOrganizer();
            IventsOrganizer.Show();
           // this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var UsersOrganizer = new UsersOrganizer();
            UsersOrganizer.Show();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var JuryOrganizer = new JuryOrganizer();
            JuryOrganizer.Show();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
