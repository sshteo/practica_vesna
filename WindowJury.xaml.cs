using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace practica_vesna
{
    public partial class WindowJury : Window
    {
        private int userId;
        private string photoFileName;
        private string connectionString = "Server=.;Database=practica;Integrated Security=True;";

        public WindowJury(int userId, string lastName, string firstName, string middleName, string photoFileName, string roleName)
        {
            InitializeComponent();
            this.userId = userId;
            this.photoFileName = photoFileName;
            LoadPersonalInfo(lastName, firstName, middleName);
            LoadParticipants();
        }

        private void LoadPersonalInfo(string lastName, string firstName, string middleName)
        {
            txtFullName.Text = $"{lastName} {firstName} {middleName}";
            // Email и телефон из базы
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Email, Phone FROM Users WHERE UserID=@UserID", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtEmail.Text = $"Email: {reader["Email"]}";
                        txtPhone.Text = $"Телефон: {reader["Phone"]}";
                    }
                }
            }
            // Фото
            if (!string.IsNullOrEmpty(photoFileName))
            {
                string photoPath = System.IO.Path.Combine(@"D:\resours\organized_import\", photoFileName);
                if (File.Exists(photoPath))
                    imgPhoto.Source = new BitmapImage(new Uri(photoPath));
            }
        }

        private void LoadParticipants()
        {
            var table = new DataTable();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // Выбираем всех участников (роль 1 — участник)
                var cmd = new SqlCommand(@"
SELECT u.LastName, u.FirstName, u.MiddleName, u.Email, c.CountryName
FROM Users u
LEFT JOIN Countries c ON u.CountryCode2 = c.CountryCode2
JOIN UserRoles ur ON u.UserID = ur.UserID
WHERE ur.RoleID = 1", conn);
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(table);
                }
            }
            dgParticipants.ItemsSource = table.DefaultView;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
