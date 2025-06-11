using Microsoft.Win32;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace practica_vesna
{
    public partial class JuryOrganizer : Window
    {
        private string photoFileName = "";
        private const string PhotoFolderPath = "Photos"; // Папка для хранения фото

        public JuryOrganizer()
        {
            InitializeComponent();
            LoadData();
            GenerateUserId();
        }

        private void LoadData()
        {
            // Загрузка данных в комбобоксы
            LoadGenders();
            LoadRoles();
            LoadDirections();
            LoadEvents();
        }

        private void GenerateUserId()
        {
            // Генерация уникального ID
            IdTextBox.Text = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        private void LoadGenders()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string query = "SELECT GenderID, GenderName FROM Genders";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    GenderComboBox.Items.Clear();
                    while (reader.Read())
                    {
                        GenderComboBox.Items.Add(new
                        {
                            GenderID = reader["GenderID"],
                            GenderName = reader["GenderName"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка полов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRoles()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string query = "SELECT RoleID, RoleName FROM Roles WHERE RoleName IN ('Жюри', 'Модератор')";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    RoleComboBox.Items.Clear();
                    while (reader.Read())
                    {
                        RoleComboBox.Items.Add(new
                        {
                            RoleID = reader["RoleID"],
                            RoleName = reader["RoleName"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка ролей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDirections()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string query = "SELECT DirectionID, DirectionName FROM Directions";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    DirectionComboBox.Items.Clear();
                    while (reader.Read())
                    {
                        DirectionComboBox.Items.Add(new
                        {
                            DirectionID = reader["DirectionID"],
                            DirectionName = reader["DirectionName"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка направлений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEvents()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string query = "SELECT EventID, EventName FROM Events";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    EventComboBox.Items.Clear();
                    while (reader.Read())
                    {
                        EventComboBox.Items.Add(new
                        {
                            EventID = reader["EventID"],
                            EventName = reader["EventName"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка мероприятий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetConnectionString()
        {
            return "Data Source=(local);Initial Catalog=practica;Integrated Security=True";
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                photoFileName = openFileDialog.SafeFileName;
                try
                {
                    // Загрузка изображения для предпросмотра
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PhotoImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddDirection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Добавление нового направления", "Введите название направления:");
            if (dialog.ShowDialog() == true)
            {
                string newDirection = dialog.Answer;
                if (!string.IsNullOrWhiteSpace(newDirection))
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                        {
                            connection.Open();
                            string query = "INSERT INTO Directions (DirectionName) VALUES (@DirectionName); SELECT SCOPE_IDENTITY();";
                            SqlCommand command = new SqlCommand(query, connection);
                            command.Parameters.AddWithValue("@DirectionName", newDirection);
                            int newId = Convert.ToInt32(command.ExecuteScalar());

                            DirectionComboBox.Items.Add(new { DirectionID = newId, DirectionName = newDirection });
                            DirectionComboBox.SelectedIndex = DirectionComboBox.Items.Count - 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка добавления направления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private bool ValidateInput()
        {
            // Проверка ФИО
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Фамилия и имя обязательны для заполнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка пола
            if (GenderComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите пол!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка роли
            if (RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                !Regex.IsMatch(EmailTextBox.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введите корректный email!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка телефона
            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text) ||
                !Regex.IsMatch(PhoneTextBox.Text, @"^\+7\(\d{3}\)-\d{3}-\d{2}-\d{2}$"))
            {
                MessageBox.Show("Введите телефон в формате +7(999)-099-90-90!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка направления
            if (DirectionComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите направление!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка пароля
            string password = PasswordBox.Password;
            string repeatPassword = RepeatPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(password, @"[A-Z]") || !Regex.IsMatch(password, @"[a-z]"))
            {
                MessageBox.Show("Пароль должен содержать заглавные и строчные буквы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(password, @"\d"))
            {
                MessageBox.Show("Пароль должен содержать хотя бы одну цифру!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+=\[{\]};:<>|./?,-]"))
            {
                MessageBox.Show("Пароль должен содержать хотя бы один спецсимвол!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (password != repeatPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // 1. Добавление пользователя
                        string insertUserQuery = @"
                        INSERT INTO Users (LastName, FirstName, MiddleName, Email, Phone, PasswordHash, Photo)
                        VALUES (@LastName, @FirstName, @MiddleName, @Email, @Phone, @Password, @Photo);
                        SELECT SCOPE_IDENTITY();";

                        SqlCommand userCommand = new SqlCommand(insertUserQuery, connection, transaction);
                        userCommand.Parameters.AddWithValue("@LastName", LastNameTextBox.Text);
                        userCommand.Parameters.AddWithValue("@FirstName", FirstNameTextBox.Text);
                        userCommand.Parameters.AddWithValue("@MiddleName", string.IsNullOrWhiteSpace(MiddleNameTextBox.Text) ?
                            (object)DBNull.Value : MiddleNameTextBox.Text);
                        userCommand.Parameters.AddWithValue("@Email", EmailTextBox.Text);
                        userCommand.Parameters.AddWithValue("@Phone", PhoneTextBox.Text);
                        userCommand.Parameters.AddWithValue("@Password", PasswordBox.Password); // Сохраняем пароль в открытом виде
                        userCommand.Parameters.AddWithValue("@Photo", string.IsNullOrEmpty(photoFileName) ?
                            (object)DBNull.Value : photoFileName);

                        int userId = Convert.ToInt32(userCommand.ExecuteScalar());

                        // 2. Добавление пола пользователя
                        dynamic selectedGender = GenderComboBox.SelectedItem;
                        int genderId = selectedGender.GenderID;

                        string insertGenderQuery = "INSERT INTO UserGenders (UserID, GenderID) VALUES (@UserID, @GenderID)";
                        SqlCommand genderCommand = new SqlCommand(insertGenderQuery, connection, transaction);
                        genderCommand.Parameters.AddWithValue("@UserID", userId);
                        genderCommand.Parameters.AddWithValue("@GenderID", genderId);
                        genderCommand.ExecuteNonQuery();

                        // 3. Добавление роли пользователя
                        dynamic selectedRole = RoleComboBox.SelectedItem;
                        int roleId = selectedRole.RoleID;

                        string insertRoleQuery = "INSERT INTO UserRoles (UserID, RoleID) VALUES (@UserID, @RoleID)";
                        SqlCommand roleCommand = new SqlCommand(insertRoleQuery, connection, transaction);
                        roleCommand.Parameters.AddWithValue("@UserID", userId);
                        roleCommand.Parameters.AddWithValue("@RoleID", roleId);
                        roleCommand.ExecuteNonQuery();

                        // 4. Добавление направления пользователя
                        dynamic selectedDirection = DirectionComboBox.SelectedItem;
                        int directionId = selectedDirection.DirectionID;

                        string insertDirectionQuery = "INSERT INTO UserDirections (UserID, DirectionID) VALUES (@UserID, @DirectionID)";
                        SqlCommand directionCommand = new SqlCommand(insertDirectionQuery, connection, transaction);
                        directionCommand.Parameters.AddWithValue("@UserID", userId);
                        directionCommand.Parameters.AddWithValue("@DirectionID", directionId);
                        directionCommand.ExecuteNonQuery();

                        // 5. Прикрепление к мероприятию (если выбрано)
                        if (AttachToEventCheckBox.IsChecked == true && EventComboBox.SelectedItem != null)
                        {
                            dynamic selectedEvent = EventComboBox.SelectedItem;
                            int eventId = selectedEvent.EventID;

                            string insertEventQuery = "INSERT INTO UserEvents (UserID, EventID, RoleID) VALUES (@UserID, @EventID, @RoleID)";
                            SqlCommand eventCommand = new SqlCommand(insertEventQuery, connection, transaction);
                            eventCommand.Parameters.AddWithValue("@UserID", userId);
                            eventCommand.Parameters.AddWithValue("@EventID", eventId);
                            eventCommand.Parameters.AddWithValue("@RoleID", roleId);
                            eventCommand.ExecuteNonQuery();
                        }

                        // Копирование фото в папку приложения (если выбрано)
                        if (!string.IsNullOrEmpty(photoFileName))
                        {
                            if (!Directory.Exists(PhotoFolderPath))
                            {
                                Directory.CreateDirectory(PhotoFolderPath);
                            }

                            string sourcePath = PhotoImage.Source.ToString().Replace("file:///", "");
                            string destPath = Path.Combine(PhotoFolderPath, photoFileName);

                            if (File.Exists(sourcePath) && !File.Exists(destPath))
                            {
                                File.Copy(sourcePath, destPath);
                            }
                        }

                        transaction.Commit();
                        MessageBox.Show("Регистрация жюри/модератора успешно завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // Вспомогательный класс для диалога ввода нового направления
    public class InputDialog : Window
    {
        public string Answer { get; set; }

        public InputDialog(string title, string question)
        {
            this.Title = title;
            this.Width = 300;
            this.Height = 150;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            StackPanel panel = new StackPanel();

            Label questionLabel = new Label
            {
                Content = question,
                Margin = new Thickness(10)
            };
            panel.Children.Add(questionLabel);

            TextBox answerTextBox = new TextBox
            {
                Margin = new Thickness(10)
            };
            panel.Children.Add(answerTextBox);

            Button okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(10),
                IsDefault = true
            };
            okButton.Click += (sender, e) =>
            {
                Answer = answerTextBox.Text;
                DialogResult = true;
            };
            panel.Children.Add(okButton);

            Button cancelButton = new Button
            {
                Content = "Отмена",
                Width = 80,
                Margin = new Thickness(10),
                IsCancel = true
            };
            cancelButton.Click += (sender, e) =>
            {
                DialogResult = false;
            };
            panel.Children.Add(cancelButton);

            this.Content = panel;
        }
    }
}