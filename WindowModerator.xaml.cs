using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace practica_vesna
{
    // ViewModel для отображения активностей с возможностью прикрепления
    public class ActivityViewModel : INotifyPropertyChanged
    {
        public int ActivityID { get; set; }
        public string ActivityName { get; set; }
        public int DayNumber { get; set; }
        public string StartTime { get; set; }
        public string ModeratorName { get; set; }
        public int? ModeratorID { get; set; }
        public bool CanAttach { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class WindowModerator : Window
    {
        private int userId;
        private string photoFileName;
        private string connectionString = "Server=.;Database=practica;Integrated Security=True;";
        private int moderatorRoleId = -1;

        public WindowModerator(int userId, string lastName, string firstName, string middleName, string photoFileName, string roleName)
        {
            InitializeComponent();
            this.userId = userId;
            this.photoFileName = photoFileName;
            LoadPersonalInfo(lastName, firstName, middleName);
            LoadEvents();
            GetModeratorRoleId();
        }

        // Получение RoleID для роли "Модератор"
        private void GetModeratorRoleId()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT RoleID FROM Roles WHERE RoleName = N'Модератор'", conn);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    moderatorRoleId = Convert.ToInt32(result);
                else
                    MessageBox.Show("Роль 'Модератор' не найдена в базе данных!");
            }
        }

        // Загрузка личной информации пользователя
        private void LoadPersonalInfo(string lastName, string firstName, string middleName)
        {
            txtFullName.Text = $"{lastName} {firstName} {middleName}";
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
            if (!string.IsNullOrEmpty(photoFileName))
            {
                string photoPath = Path.Combine(@"D:\resours\organized_import\", photoFileName);
                if (File.Exists(photoPath))
                    imgPhoto.Source = new BitmapImage(new Uri(photoPath));
            }
        }

        // Загрузка списка мероприятий
        private void LoadEvents()
        {
            var table = new DataTable();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "SELECT EventID, EventName, StartDate, DurationDays FROM Events ORDER BY StartDate DESC", conn);
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(table);
                }
            }
            dgEvents.ItemsSource = table.DefaultView;
        }

        // Обработка выбора мероприятия — загрузка активностей этого мероприятия
        private void dgEvents_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgEvents.SelectedItem is DataRowView row)
            {
                int eventId = Convert.ToInt32(row["EventID"]);
                LoadActivities(eventId);
            }
            else
            {
                dgActivities.ItemsSource = null;
            }
        }

        // Загрузка активностей выбранного мероприятия
        private void LoadActivities(int eventId)
        {
            var activities = new ObservableCollection<ActivityViewModel>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
SELECT ea.ActivityID, a.ActivityName, ea.DayNumber, ea.StartTime,
       u.LastName + ' ' + u.FirstName AS ModeratorName, ea.ModeratorID
FROM EventActivities ea
JOIN Activities a ON ea.ActivityID = a.ActivityID
LEFT JOIN Users u ON ea.ModeratorID = u.UserID
WHERE ea.EventID = @EventID", conn);
                cmd.Parameters.AddWithValue("@EventID", eventId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var modId = reader["ModeratorID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ModeratorID"]);
                        activities.Add(new ActivityViewModel
                        {
                            ActivityID = Convert.ToInt32(reader["ActivityID"]),
                            ActivityName = reader["ActivityName"].ToString(),
                            DayNumber = Convert.ToInt32(reader["DayNumber"]),
                            StartTime = reader["StartTime"].ToString(),
                            ModeratorName = reader["ModeratorName"] == DBNull.Value ? "" : reader["ModeratorName"].ToString(),
                            ModeratorID = modId,
                            CanAttach = !modId.HasValue // Можно прикрепиться только если модератора нет
                        });
                    }
                }
            }
            dgActivities.ItemsSource = activities;
        }

        // Обработка нажатия кнопки "Прикрепиться" к активности
        private void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgActivities.SelectedItem is ActivityViewModel activity)
            {
                if (activity.ModeratorID.HasValue)
                {
                    MessageBox.Show("К этой активности уже прикреплён модератор.");
                    return;
                }
                if (MessageBox.Show("Вы действительно хотите стать модератором этой активности?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        var cmd = new SqlCommand("UPDATE EventActivities SET ModeratorID=@UserID WHERE ActivityID=@ActivityID AND EventID=@EventID", conn);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@ActivityID", activity.ActivityID);
                        // Нужно взять EventID из выбранного мероприятия
                        if (dgEvents.SelectedItem is DataRowView eventRow)
                        {
                            int eventId = Convert.ToInt32(eventRow["EventID"]);
                            cmd.Parameters.AddWithValue("@EventID", eventId);
                        }
                        else
                        {
                            MessageBox.Show("Ошибка: не выбрано мероприятие.");
                            return;
                        }
                        cmd.ExecuteNonQuery();
                    }
                    // Обновить список активностей
                    if (dgEvents.SelectedItem is DataRowView eventRow2)
                        LoadActivities(Convert.ToInt32(eventRow2["EventID"]));
                }
            }
            else
            {
                MessageBox.Show("Выберите активность.");
            }
        }

        // Обработка нажатия кнопки "Прикрепиться к мероприятию"
        private void btnAttachToEvent_Click(object sender, RoutedEventArgs e)
        {
            if (dgEvents.SelectedItem is DataRowView row)
            {
                int eventId = Convert.ToInt32(row["EventID"]);
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM UserEvents WHERE UserID=@UserID AND EventID=@EventID AND RoleID=@RoleID", conn);
                    checkCmd.Parameters.AddWithValue("@UserID", userId);
                    checkCmd.Parameters.AddWithValue("@EventID", eventId);
                    checkCmd.Parameters.AddWithValue("@RoleID", moderatorRoleId);
                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        MessageBox.Show("Вы уже прикреплены к этому мероприятию как модератор.");
                        return;
                    }

                    if (MessageBox.Show("Вы действительно хотите стать модератором этого мероприятия?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        var insertCmd = new SqlCommand(
                            "INSERT INTO UserEvents (UserID, EventID, RoleID) VALUES (@UserID, @EventID, @RoleID)", conn);
                        insertCmd.Parameters.AddWithValue("@UserID", userId);
                        insertCmd.Parameters.AddWithValue("@EventID", eventId);
                        insertCmd.Parameters.AddWithValue("@RoleID", moderatorRoleId);
                        insertCmd.ExecuteNonQuery();
                        MessageBox.Show("Вы успешно прикреплены к мероприятию как модератор!");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите мероприятие.");
            }
        }

        // Кнопка выхода из окна
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
