using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace practica_vesna
{
    public partial class WindowUser : Window
    {
        private string connectionString = "Server=.;Database=practica;Integrated Security=True;";
        private string photosFolderPath = @"D:\resours\organized_import\";

        // Поля для хранения данных пользователя
        private int userId;
        private string lastName;
        private string firstName;
        private string middleName;
        private string photoFileName;
        private string roleName;

        // Новый конструктор с 6 параметрами
        public WindowUser(int userId, string lastName, string firstName, string middleName, string photoFileName, string roleName)
        {
            InitializeComponent();

            this.userId = userId;
            this.lastName = lastName;
            this.firstName = firstName;
            this.middleName = middleName;
            this.photoFileName = photoFileName;
            this.roleName = roleName;

            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                // Установка приветствия
                WelcomeText.Text = $"Добро пожаловать, {firstName} {lastName}!";

                // Загрузка фото
                if (!string.IsNullOrEmpty(photoFileName))
                {
                    string photoPath = Path.Combine(photosFolderPath, photoFileName);
                    if (File.Exists(photoPath))
                    {
                        UserPhoto.Source = new BitmapImage(new Uri(photoPath));
                    }
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Загрузка подробной информации о пользователе из базы
                    string userQuery = @"
                        SELECT u.BirthDate, u.Email, u.Phone, c.CountryName 
                        FROM Users u
                        LEFT JOIN Countries c ON u.CountryCode2 = c.CountryCode2
                        WHERE u.UserID = @UserID";

                    SqlCommand userCommand = new SqlCommand(userQuery, connection);
                    userCommand.Parameters.AddWithValue("@UserID", userId);

                    using (SqlDataReader reader = userCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            FullNameText.Text = $"{lastName} {firstName} {middleName}";
                            BirthDateText.Text = reader["BirthDate"] != DBNull.Value ? Convert.ToDateTime(reader["BirthDate"]).ToString("dd.MM.yyyy") : "не указана";
                            EmailText.Text = reader["Email"]?.ToString() ?? "не указан";
                            PhoneText.Text = reader["Phone"]?.ToString() ?? "не указан";
                            CountryText.Text = reader["CountryName"]?.ToString() ?? "не указана";
                        }
                    }

                    // Загрузка направлений пользователя
                    string directionQuery = @"
                        SELECT d.DirectionName 
                        FROM UserDirections ud
                        JOIN Directions d ON ud.DirectionID = d.DirectionID
                        WHERE ud.UserID = @UserID";

                    SqlCommand directionCommand = new SqlCommand(directionQuery, connection);
                    directionCommand.Parameters.AddWithValue("@UserID", userId);

                    using (SqlDataReader directionReader = directionCommand.ExecuteReader())
                    {
                        string directions = "";
                        while (directionReader.Read())
                        {
                            directions += directionReader["DirectionName"] + ", ";
                        }
                        if (directions.Length > 0)
                        {
                            directions = directions.Remove(directions.Length - 2);
                        }
                        DirectionText.Text = string.IsNullOrEmpty(directions) ? "не указано" : directions;
                    }

                    // Загрузка мероприятий пользователя
                    string eventsQuery = @"
                        SELECT e.EventName, e.StartDate, e.DurationDays 
                        FROM UserEvents ue
                        JOIN Events e ON ue.EventID = e.EventID
                        WHERE ue.UserID = @UserID AND ue.RoleID = 1
                        ORDER BY e.StartDate DESC";

                    SqlCommand eventsCommand = new SqlCommand(eventsQuery, connection);
                    eventsCommand.Parameters.AddWithValue("@UserID", userId);

                    System.Data.DataTable eventsTable = new System.Data.DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(eventsCommand))
                    {
                        adapter.Fill(eventsTable);
                    }

                    EventsList.ItemsSource = eventsTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
