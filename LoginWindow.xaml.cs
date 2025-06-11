using System;
using System.Data.SqlClient;
using System.Drawing;           // для Font, FontStyle, Color
using System.Drawing.Imaging;  // для ImageFormat
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;  // для BitmapImage


namespace practica_vesna
{
    public partial class LoginWindow : Window
    {
        private string connectionString = "Server=SHTEO;Database=practica;Integrated Security=True;";
        private string currentCaptchaText;
        private int failedAttempts = 0;
        private bool isLocked = false;

        public LoginWindow()
        {
            InitializeComponent();
            LoadSavedCredentials();
            GenerateCaptcha();
        }

        private void LoadSavedCredentials()
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.SavedLogin))
                txtLogin.Text = Properties.Settings.Default.SavedLogin;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.SavedPassword))
            {
                txtPassword.Password = Properties.Settings.Default.SavedPassword;
                chkRememberMe.IsChecked = true;
            }
        }

        private void SaveCredentials()
        {
            if (chkRememberMe.IsChecked == true)
            {
                Properties.Settings.Default.SavedLogin = txtLogin.Text;
                Properties.Settings.Default.SavedPassword = txtPassword.Password;
            }
            else
            {
                Properties.Settings.Default.SavedLogin = "";
                Properties.Settings.Default.SavedPassword = "";
            }
            Properties.Settings.Default.Save();
        }

        private void GenerateCaptcha()
        {
            currentCaptchaText = GenerateRandomText(4);
            imgCaptcha.Source = GenerateCaptchaImage(currentCaptchaText);
            txtCaptchaAnswer.Text = "";
        }

        private string GenerateRandomText(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // без похожих символов
            var random = new Random();
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
                buffer[i] = chars[random.Next(chars.Length)];
            return new string(buffer);
        }

        private BitmapImage GenerateCaptchaImage(string text)
        {
            int width = 120;
            int height = 40;
            var bmp = new Bitmap(width, height);
            var rnd = new Random();

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                // Рисуем шум (точки)
                for (int i = 0; i < 100; i++)
                {
                    int x = rnd.Next(width);
                    int y = rnd.Next(height);
                    bmp.SetPixel(x, y, Color.LightGray);
                }

                // Рисуем шум (линии)
                for (int i = 0; i < 5; i++)
                {
                    int x1 = rnd.Next(width);
                    int y1 = rnd.Next(height);
                    int x2 = rnd.Next(width);
                    int y2 = rnd.Next(height);
                    g.DrawLine(new Pen(Color.LightGray), x1, y1, x2, y2);
                }

                // Рисуем текст
                var fonts = new[] { "Arial", "Georgia", "Tahoma", "Comic Sans MS" };
                for (int i = 0; i < text.Length; i++)
                {
                    var fontName = fonts[rnd.Next(fonts.Length)];
                    using (Font font = new Font(new FontFamily(fontName), 20, System.Drawing.FontStyle.Bold))
                    {
                        var brush = new SolidBrush(Color.FromArgb(rnd.Next(50, 150), rnd.Next(50, 150), rnd.Next(50, 150)));
                        int x = 10 + i * 25 + rnd.Next(-3, 3);
                        int y = rnd.Next(0, 10);
                        g.DrawString(text[i].ToString(), font, brush, x, y);
                    }
                }
            }

            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (isLocked)
            {
                MessageBox.Show("Система заблокирована. Пожалуйста, подождите.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();
            string captchaInput = txtCaptchaAnswer.Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (captchaInput != currentCaptchaText)
            {
                MessageBox.Show("Неверно введена капча. Попробуйте ещё раз.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                GenerateCaptcha();
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
SELECT u.UserID, u.LastName, u.FirstName, u.MiddleName, u.Photo, r.RoleID, r.RoleName
FROM Users u
JOIN UserRoles ur ON u.UserID = ur.UserID
JOIN Roles r ON ur.RoleID = r.RoleID
WHERE u.Email = @Login AND u.PasswordHash = @Password";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@Password", password);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                SaveCredentials();

                                int userId = reader.GetInt32(reader.GetOrdinal("UserID"));
                                string lastName = reader.GetString(reader.GetOrdinal("LastName"));
                                string firstName = reader.GetString(reader.GetOrdinal("FirstName"));
                                string middleName = reader.IsDBNull(reader.GetOrdinal("MiddleName")) ? "" : reader.GetString(reader.GetOrdinal("MiddleName"));
                                string photoFileName = reader.IsDBNull(reader.GetOrdinal("Photo")) ? null : reader.GetString(reader.GetOrdinal("Photo"));
                                int roleId = reader.GetInt32(reader.GetOrdinal("RoleID"));
                                string roleName = reader.GetString(reader.GetOrdinal("RoleName"));

                                switch (roleId)
                                {
                                    case 1:
                                        var windowUser = new WindowUser(userId, lastName, firstName, middleName, photoFileName, roleName);
                                        windowUser.Show();
                                        break;
                                    case 2:
                                        var windowModerator = new WindowModerator(userId, lastName, firstName, middleName, photoFileName, roleName);
                                        windowModerator.Show();
                                        break;
                                    case 3:
                                        string gender = "Мужской";
                                        var windowOrganizer = new WindowOrganizer(lastName, firstName, middleName, gender, photoFileName);
                                        windowOrganizer.Show();
                                        break;
                                    case 4:
                                        var windowJury = new WindowJury(userId, lastName, firstName, middleName, photoFileName, roleName);
                                        windowJury.Show();
                                        break;
                                    default:
                                        MessageBox.Show("Неизвестная роль пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                }
                                this.Close();
                            }
                            else
                            {
                                await HandleFailedAttemptAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task HandleFailedAttemptAsync()
        {
            failedAttempts++;
            if (failedAttempts >= 3)
            {
                isLocked = true;
                btnLogin.IsEnabled = false;
                MessageBox.Show("Слишком много неудачных попыток. Система заблокирована на 10 секунд.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Warning);
                await Task.Delay(10000);
                isLocked = false;
                btnLogin.IsEnabled = true;
                failedAttempts = 0;
                GenerateCaptcha();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                GenerateCaptcha();
            }
        }

        private void RefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
        }

        private void btnMainMenu_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}