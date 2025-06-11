using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Data.SqlClient;

namespace practica_vesna
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadDirections();
                LoadEvents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        string connectionString = "Server=SHTEO;Database=practica;Integrated Security=True;";

        private void LoadEvents()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"SELECT 
                                    e.EventID, 
                                    ISNULL(d.DirectionName, 'Не указано') AS DirectionName,
                                    ISNULL(et.TypeName, 'Не указан') AS EventType,
                                    e.EventName, 
                                    e.StartDate, 
                                    ISNULL(e.DurationDays, 0) AS DurationDays, 
                                    ISNULL(e.OrganizerID, 0) AS OrganizerID 
                                   FROM Events e
                                   LEFT JOIN EventTypeDirection etd ON e.EventID = etd.EventID
                                   LEFT JOIN Directions d ON etd.DirectionID = d.DirectionID
                                   LEFT JOIN EventTypes et ON etd.EventTypeID = et.EventTypeID";

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    List<Event> events = new List<Event>();
                    while (reader.Read())
                    {
                        events.Add(new Event()
                        {
                            EventID = reader.GetInt32(0),
                            Direction = reader.GetString(1),
                            EventType = reader.GetString(2),
                            EventName = reader.IsDBNull(3) ? "Без названия" : reader.GetString(3),
                            StartDate = reader.GetDateTime(4),
                            DurationDays = reader.GetInt32(5),
                            OrganizerID = reader.GetInt32(6)
                        });
                    }

                    EventsList.ItemsSource = events;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке мероприятий:\n{ex.ToString()}", "Ошибка",
                             MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDirections()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT DirectionName FROM Directions";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    DirectionFilter.Items.Clear();
                    DirectionFilter.Items.Add("Все направления");

                    while (reader.Read())
                    {
                        DirectionFilter.Items.Add(reader["DirectionName"].ToString());
                    }

                    DirectionFilter.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке направлений:\n{ex.ToString()}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string directionFilter = DirectionFilter.SelectedItem?.ToString();
                DateTime? dateFrom = DateFromFilter.SelectedDate;
                DateTime? dateTo = DateToFilter.SelectedDate;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT 
                                    e.EventID, 
                                    ISNULL(d.DirectionName, 'Не указано') AS DirectionName,
                                    ISNULL(et.TypeName, 'Не указан') AS EventType,
                                    e.EventName, 
                                    e.StartDate, 
                                    ISNULL(e.DurationDays, 0) AS DurationDays, 
                                    ISNULL(e.OrganizerID, 0) AS OrganizerID 
                                   FROM Events e
                                   LEFT JOIN EventTypeDirection etd ON e.EventID = etd.EventID
                                   LEFT JOIN Directions d ON etd.DirectionID = d.DirectionID
                                   LEFT JOIN EventTypes et ON etd.EventTypeID = et.EventTypeID
                                   WHERE 1=1";

                    if (directionFilter != null && directionFilter != "Все направления")
                    {
                        query += " AND d.DirectionName = @Direction";
                    }

                    if (dateFrom.HasValue)
                    {
                        query += " AND e.StartDate >= @DateFrom";
                    }

                    if (dateTo.HasValue)
                    {
                        query += " AND e.StartDate <= @DateTo";
                    }

                    SqlCommand command = new SqlCommand(query, connection);

                    if (directionFilter != null && directionFilter != "Все направления")
                    {
                        command.Parameters.AddWithValue("@Direction", directionFilter);
                    }

                    if (dateFrom.HasValue)
                    {
                        command.Parameters.AddWithValue("@DateFrom", dateFrom.Value);
                    }

                    if (dateTo.HasValue)
                    {
                        command.Parameters.AddWithValue("@DateTo", dateTo.Value);
                    }

                    SqlDataReader reader = command.ExecuteReader();

                    List<Event> events = new List<Event>();
                    while (reader.Read())
                    {
                        events.Add(new Event()
                        {
                            EventID = reader.GetInt32(0),
                            Direction = reader.GetString(1),
                            EventType = reader.GetString(2),
                            EventName = reader.IsDBNull(3) ? "Без названия" : reader.GetString(3),
                            StartDate = reader.GetDateTime(4),
                            DurationDays = reader.GetInt32(5),
                            OrganizerID = reader.GetInt32(6)
                        });
                    }

                    EventsList.ItemsSource = events;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении фильтра: {ex.Message}", "Ошибка",
                             MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            DirectionFilter.SelectedIndex = 0;
            DateFromFilter.SelectedDate = null;
            DateToFilter.SelectedDate = null;
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            ResetFilter_Click(sender, e);
            LoadEvents();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close(); 
        }
    }

    public class Event
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public string Direction { get; set; }
        public string EventType { get; set; }
        public DateTime StartDate { get; set; }
        public int DurationDays { get; set; }
        public int OrganizerID { get; set; }
    }

    public class User
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Gender { get; set; }
        public UserRole Role { get; set; }
    }

    public class UserRole
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; }
    }

}