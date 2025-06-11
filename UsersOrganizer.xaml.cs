using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace practica_vesna
{
    public partial class UsersOrganizer : Window
    {
        // Исправленная строка подключения (укажите ваши параметры)
        private string connectionString = "Server=.;Database=practica;Integrated Security=True;";
        private string photosFolderPath = @"D:\resours\organized_import\";

        public UsersOrganizer()
        {
            InitializeComponent();
            LoadParticipants();
            LoadDirections();
        }

        private void LoadParticipants(int? directionId = null)
        {
            ParticipantsList.Items.Clear();

            string query = @"
                SELECT u.* 
                FROM Users u
                JOIN UserRoles ur ON u.UserID = ur.UserID
                WHERE ur.RoleID = 1";

            if (directionId.HasValue)
            {
                query += @"
                    AND u.UserID IN (
                        SELECT UserID FROM UserDirections 
                        WHERE DirectionID = @DirectionID
                    )";
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);

                    if (directionId.HasValue)
                    {
                        command.Parameters.AddWithValue("@DirectionID", directionId.Value);
                    }

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        var participant = new Participant
                        {
                            UserID = Convert.ToInt32(reader["UserID"]),
                            LastName = reader["LastName"].ToString(),
                            FirstName = reader["FirstName"].ToString(),
                            MiddleName = reader["MiddleName"].ToString(),
                            Email = reader["Email"].ToString(),
                            BirthDate = Convert.ToDateTime(reader["BirthDate"]),
                            Phone = reader["Phone"].ToString(),
                            PhotoFileName = reader["Photo"].ToString()
                        };

                        // Загрузка фото
                        if (!string.IsNullOrEmpty(participant.PhotoFileName))
                        {
                            string photoPath = Path.Combine(photosFolderPath, participant.PhotoFileName);
                            if (File.Exists(photoPath))
                            {
                                participant.PhotoPath = new BitmapImage(new Uri(photoPath));
                            }
                        }

                        ParticipantsList.Items.Add(participant);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке участников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDirections()
        {
            DirectionFilter.Items.Clear();

            string query = "SELECT DirectionID, DirectionName FROM Directions";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        DirectionFilter.Items.Add(new
                        {
                            DirectionID = Convert.ToInt32(reader["DirectionID"]),
                            DirectionName = reader["DirectionName"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке направлений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (DirectionFilter.SelectedItem != null)
            {
                dynamic selectedItem = DirectionFilter.SelectedItem;
                int directionId = selectedItem.DirectionID;
                LoadParticipants(directionId);
            }
            else
            {
                MessageBox.Show("Выберите направление для фильтрации", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            DirectionFilter.SelectedItem = null;
            LoadParticipants();
        }

       

        private void AddParticipant_Click(object sender, RoutedEventArgs e)
        {
            Window addParticipantWindow = new Window
            {
                Title = "Добавить нового участника",
                Width = 450,  // Увеличено с 400
                Height = 650, // Увеличено с 550
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            // Добавляем ScrollViewer для прокрутки при необходимости
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            StackPanel mainPanel = new StackPanel
            {
                Margin = new Thickness(10),
                Width = 430 // Увеличено для соответствия новому размеру окна
            };

            // Поля для ввода данных (без изменений)
            mainPanel.Children.Add(new Label { Content = "Фамилия:" });
            TextBox lastNameTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(lastNameTextBox);

            mainPanel.Children.Add(new Label { Content = "Имя:" });
            TextBox firstNameTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(firstNameTextBox);

            mainPanel.Children.Add(new Label { Content = "Отчество:" });
            TextBox middleNameTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(middleNameTextBox);

            mainPanel.Children.Add(new Label { Content = "Email:" });
            TextBox emailTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(emailTextBox);

            mainPanel.Children.Add(new Label { Content = "Дата рождения:" });
            DatePicker birthDatePicker = new DatePicker { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(birthDatePicker);

            mainPanel.Children.Add(new Label { Content = "Телефон:" });
            TextBox phoneTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(phoneTextBox);

            mainPanel.Children.Add(new Label { Content = "Пароль:" });
            PasswordBox passwordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(passwordBox);

            mainPanel.Children.Add(new Label { Content = "Фото (имя файла):" });
            TextBox photoTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(photoTextBox);

            // Направления с увеличенной высотой ListBox
            mainPanel.Children.Add(new Label { Content = "Направления:" });
            ListBox directionsListBox = new ListBox
            {
                Height = 120, // Увеличено с 100
                Margin = new Thickness(0, 0, 0, 10),
                SelectionMode = SelectionMode.Multiple
            };
            mainPanel.Children.Add(directionsListBox);

            // Загрузка направлений
            LoadDirectionsToListBox(directionsListBox);

            // Кнопка добавления с увеличенными отступами
            Button addButton = new Button
            {
                Content = "Добавить",
                Margin = new Thickness(0, 20, 0, 10), // Увеличено сверху
                Padding = new Thickness(15, 8, 15, 8), // Увеличено
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 100
            };

            addButton.Click += (s, args) =>
            {
                if (ValidateParticipantInput(lastNameTextBox, firstNameTextBox, emailTextBox,
                    birthDatePicker, phoneTextBox, passwordBox, directionsListBox))
                {
                    try
                    {
                        // Добавление пользователя
                        int newUserId = AddUserToDatabase(
                            lastNameTextBox.Text,
                            firstNameTextBox.Text,
                            middleNameTextBox.Text,
                            emailTextBox.Text,
                            birthDatePicker.SelectedDate.Value,
                            phoneTextBox.Text,
                            passwordBox.Password,
                            photoTextBox.Text
                        );

                        // Добавление роли участника (RoleID = 1)
                        AddUserRole(newUserId, 1);

                        // Добавление направлений
                        foreach (var selectedItem in directionsListBox.SelectedItems)
                        {
                            dynamic direction = selectedItem;
                            AddUserDirection(newUserId, direction.DirectionID);
                        }

                        MessageBox.Show("Участник успешно добавлен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        addParticipantWindow.Close();
                        LoadParticipants();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при добавлении участника: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };

            mainPanel.Children.Add(addButton);

            // Помещаем mainPanel в ScrollViewer
            scrollViewer.Content = mainPanel;

            // Устанавливаем содержимое окна
            addParticipantWindow.Content = scrollViewer;

            addParticipantWindow.ShowDialog();
        }

        private void LoadDirectionsToListBox(ListBox listBox)
        {
            listBox.Items.Clear();
            listBox.DisplayMemberPath = "DirectionName";

            string query = "SELECT DirectionID, DirectionName FROM Directions";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        listBox.Items.Add(new
                        {
                            DirectionID = Convert.ToInt32(reader["DirectionID"]),
                            DirectionName = reader["DirectionName"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке направлений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int AddUserToDatabase(string lastName, string firstName, string middleName,
            string email, DateTime birthDate, string phone, string password, string photo)
        {
            string query = @"
                INSERT INTO Users (LastName, FirstName, MiddleName, Email, BirthDate, Phone, PasswordHash, Photo)
                OUTPUT INSERTED.UserID
                VALUES (@LastName, @FirstName, @MiddleName, @Email, @BirthDate, @Phone, @PasswordHash, @Photo)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LastName", lastName);
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@MiddleName", string.IsNullOrEmpty(middleName) ? (object)DBNull.Value : middleName);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@BirthDate", birthDate);
                command.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(phone) ? (object)DBNull.Value : phone);
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                command.Parameters.AddWithValue("@Photo", string.IsNullOrEmpty(photo) ? (object)DBNull.Value : photo);

                return (int)command.ExecuteScalar();
            }
        }

        private void AddUserRole(int userId, int roleId)
        {
            string query = "INSERT INTO UserRoles (UserID, RoleID) VALUES (@UserID, @RoleID)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@RoleID", roleId);
                command.ExecuteNonQuery();
            }
        }

        private void AddUserDirection(int userId, int directionId)
        {
            string query = "INSERT INTO UserDirections (UserID, DirectionID) VALUES (@UserID, @DirectionID)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@DirectionID", directionId);
                command.ExecuteNonQuery();
            }
        }

        private string HashPassword(string password)
        {
            // Здесь должна быть реализация хеширования пароля
            // Например, можно использовать BCrypt или другой алгоритм
            return password; // В реальном приложении замените на хеширование
        }

        private bool ValidateParticipantInput(TextBox lastName, TextBox firstName, TextBox email,
     DatePicker birthDate, TextBox phone, PasswordBox password, ListBox directions)
        {
            // Проверка ФИО
            if (string.IsNullOrWhiteSpace(lastName.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(firstName.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка email
            if (string.IsNullOrWhiteSpace(email.Text) ||
                !Regex.IsMatch(email.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введите корректный email!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка даты рождения
            if (birthDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату рождения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка телефона (если не пустой)
            if (!string.IsNullOrWhiteSpace(phone.Text) &&
                !Regex.IsMatch(phone.Text, @"^\+7\(\d{3}\)-\d{3}-\d{2}-\d{2}$"))
            {
                MessageBox.Show("Введите телефон в формате +7(999)-099-90-90 или оставьте поле пустым!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка пароля
            string passwordText = password.Password;

            if (string.IsNullOrWhiteSpace(passwordText) || passwordText.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(passwordText, @"[A-Z]") || !Regex.IsMatch(passwordText, @"[a-z]"))
            {
                MessageBox.Show("Пароль должен содержать заглавные и строчные буквы!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(passwordText, @"\d"))
            {
                MessageBox.Show("Пароль должен содержать хотя бы одну цифру!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(passwordText, @"[!@#$%^&*()_+=\[{\]};:<>|./?,-]"))
            {
                MessageBox.Show("Пароль должен содержать хотя бы один спецсимвол!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка направлений
            if (directions.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одно направление", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void DeleteParticipant_Click(object sender, RoutedEventArgs e)
        {
            if (ParticipantsList.SelectedItem == null)
            {
                MessageBox.Show("Выберите участника для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить выбранного участника?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Participant selected = (Participant)ParticipantsList.SelectedItem;

                    // Удаление из UserDirections
                    string deleteDirectionsQuery = "DELETE FROM UserDirections WHERE UserID = @UserID";
                    // Удаление из UserRoles
                    string deleteRolesQuery = "DELETE FROM UserRoles WHERE UserID = @UserID";
                    // Удаление из UserEvents
                    string deleteEventsQuery = "DELETE FROM UserEvents WHERE UserID = @UserID";
                    // Удаление из Users
                    string deleteUserQuery = "DELETE FROM Users WHERE UserID = @UserID";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        SqlTransaction transaction = connection.BeginTransaction();

                        try
                        {
                            // Удаляем связи с направлениями
                            SqlCommand command = new SqlCommand(deleteDirectionsQuery, connection, transaction);
                            command.Parameters.AddWithValue("@UserID", selected.UserID);
                            command.ExecuteNonQuery();

                            // Удаляем связи с ролями
                            command = new SqlCommand(deleteRolesQuery, connection, transaction);
                            command.Parameters.AddWithValue("@UserID", selected.UserID);
                            command.ExecuteNonQuery();

                            // Удаляем связи с мероприятиями
                            command = new SqlCommand(deleteEventsQuery, connection, transaction);
                            command.Parameters.AddWithValue("@UserID", selected.UserID);
                            command.ExecuteNonQuery();

                            // Удаляем самого пользователя
                            command = new SqlCommand(deleteUserQuery, connection, transaction);
                            command.Parameters.AddWithValue("@UserID", selected.UserID);
                            command.ExecuteNonQuery();

                            transaction.Commit();
                            LoadParticipants();
                            MessageBox.Show("Участник успешно удален", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении участника: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class Participant
    {
        public int UserID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; }
        public string PhotoFileName { get; set; }
        public BitmapImage PhotoPath { get; set; }
    }
}