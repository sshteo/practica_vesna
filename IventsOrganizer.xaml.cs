using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace practica_vesna
{
    public partial class IventsOrganizer : Window
    {
      
            public IventsOrganizer()
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


        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            // Создаем модальное окно для ввода данных
            Window addEventWindow = new Window
            {
                Title = "Добавить новое мероприятие",
                Width = 450,  // Увеличим ширину для удобства
                Height = 500,  // Увеличим высоту
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            // Главный контейнер с ScrollViewer
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Основной контейнер для элементов управления
            StackPanel mainPanel = new StackPanel
            {
                Margin = new Thickness(10),
                Width = 400  // Фиксированная ширина для корректного отображения
            };

            // Создаем элементы управления для ввода данных

            // Название мероприятия
            mainPanel.Children.Add(new Label { Content = "Название мероприятия:" });
            TextBox eventNameTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(eventNameTextBox);

            // Дата начала
            mainPanel.Children.Add(new Label { Content = "Дата начала:" });
            DatePicker startDatePicker = new DatePicker { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(startDatePicker);

            // Количество дней
            mainPanel.Children.Add(new Label { Content = "Количество дней:" });
            TextBox durationTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(durationTextBox);

            // ID организатора
            mainPanel.Children.Add(new Label { Content = "ID организатора:" });
            TextBox organizerIdTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(organizerIdTextBox);

            // Направление
            mainPanel.Children.Add(new Label { Content = "Направление:" });
            ComboBox directionComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(directionComboBox);

            // Тип мероприятия
            mainPanel.Children.Add(new Label { Content = "Тип мероприятия:" });
            ComboBox eventTypeComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            mainPanel.Children.Add(eventTypeComboBox);

            // Загружаем направления и типы мероприятий
            LoadComboBoxData(directionComboBox, eventTypeComboBox);

            // Кнопка добавления
            Button addButton = new Button
            {
                Content = "Добавить",
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            addButton.Click += (s, args) =>
            {
                if (ValidateInput(eventNameTextBox, startDatePicker, durationTextBox, organizerIdTextBox,
                                directionComboBox, eventTypeComboBox))
                {
                    NewEventData newEvent = new NewEventData
                    {
                        EventName = eventNameTextBox.Text,
                        StartDate = startDatePicker.SelectedDate.Value,
                        DurationDays = int.Parse(durationTextBox.Text),
                        OrganizerID = int.Parse(organizerIdTextBox.Text),
                        Direction = directionComboBox.SelectedItem.ToString(),
                        EventType = eventTypeComboBox.SelectedItem.ToString()
                    };

                    if (AddEventToDatabase(newEvent))
                    {
                        MessageBox.Show("Мероприятие успешно добавлено!", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        addEventWindow.Close();
                        LoadEvents(); // Обновляем список
                    }
                }
            };

            mainPanel.Children.Add(addButton);

            // Помещаем основной контейнер в ScrollViewer
            scrollViewer.Content = mainPanel;

            // Устанавливаем содержимое окна
            addEventWindow.Content = scrollViewer;

            addEventWindow.ShowDialog();
        }

        private bool ValidateInput(TextBox eventName, DatePicker startDate, TextBox duration,
                                     TextBox organizerId, ComboBox direction, ComboBox eventType)
            {
                if (string.IsNullOrWhiteSpace(eventName.Text))
                {
                    MessageBox.Show("Введите название мероприятия", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (!startDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату начала", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (!int.TryParse(duration.Text, out int days) || days <= 0)
                {
                    MessageBox.Show("Введите корректное количество дней", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (!int.TryParse(organizerId.Text, out int orgId) || orgId <= 0)
                {
                    MessageBox.Show("Введите корректный ID организатора", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (direction.SelectedItem == null)
                {
                    MessageBox.Show("Выберите направление", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (eventType.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип мероприятия", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                return true;
            }

            private void LoadComboBoxData(ComboBox directionComboBox, ComboBox eventTypeComboBox)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Загрузка направлений
                        string directionsQuery = "SELECT DirectionName FROM Directions";
                        SqlCommand directionsCommand = new SqlCommand(directionsQuery, connection);
                        SqlDataReader directionsReader = directionsCommand.ExecuteReader();

                        directionComboBox.Items.Clear();
                        while (directionsReader.Read())
                        {
                            directionComboBox.Items.Add(directionsReader["DirectionName"].ToString());
                        }
                        directionsReader.Close();

                        // Загрузка типов мероприятий
                        string typesQuery = "SELECT TypeName FROM EventTypes";
                        SqlCommand typesCommand = new SqlCommand(typesQuery, connection);
                        SqlDataReader typesReader = typesCommand.ExecuteReader();

                        eventTypeComboBox.Items.Clear();
                        while (typesReader.Read())
                        {
                            eventTypeComboBox.Items.Add(typesReader["TypeName"].ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private bool AddEventToDatabase(NewEventData newEvent)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Начинаем транзакцию
                        SqlTransaction transaction = connection.BeginTransaction();

                        try
                        {
                            // 1. Добавляем мероприятие в таблицу Events
                            string insertEventQuery = @"
                            INSERT INTO Events (EventName, StartDate, DurationDays, OrganizerID)
                            VALUES (@EventName, @StartDate, @DurationDays, @OrganizerID);
                            SELECT SCOPE_IDENTITY();";

                            SqlCommand eventCommand = new SqlCommand(insertEventQuery, connection, transaction);
                            eventCommand.Parameters.AddWithValue("@EventName", newEvent.EventName);
                            eventCommand.Parameters.AddWithValue("@StartDate", newEvent.StartDate);
                            eventCommand.Parameters.AddWithValue("@DurationDays", newEvent.DurationDays);
                            eventCommand.Parameters.AddWithValue("@OrganizerID", newEvent.OrganizerID);

                            int newEventId = Convert.ToInt32(eventCommand.ExecuteScalar());

                            // 2. Получаем ID направления и типа мероприятия
                            string getDirectionIdQuery = "SELECT DirectionID FROM Directions WHERE DirectionName = @DirectionName";
                            SqlCommand directionCommand = new SqlCommand(getDirectionIdQuery, connection, transaction);
                            directionCommand.Parameters.AddWithValue("@DirectionName", newEvent.Direction);
                            int directionId = Convert.ToInt32(directionCommand.ExecuteScalar());

                            string getTypeIdQuery = "SELECT EventTypeID FROM EventTypes WHERE TypeName = @TypeName";
                            SqlCommand typeCommand = new SqlCommand(getTypeIdQuery, connection, transaction);
                            typeCommand.Parameters.AddWithValue("@TypeName", newEvent.EventType);
                            int typeId = Convert.ToInt32(typeCommand.ExecuteScalar());

                            // 3. Добавляем связь между мероприятием, типом и направлением
                            string insertRelationQuery = @"
                            INSERT INTO EventTypeDirection (EventID, EventTypeID, DirectionID)
                            VALUES (@EventID, @EventTypeID, @DirectionID)";

                            SqlCommand relationCommand = new SqlCommand(insertRelationQuery, connection, transaction);
                            relationCommand.Parameters.AddWithValue("@EventID", newEventId);
                            relationCommand.Parameters.AddWithValue("@EventTypeID", typeId);
                            relationCommand.Parameters.AddWithValue("@DirectionID", directionId);
                            relationCommand.ExecuteNonQuery();

                            // Фиксируем транзакцию
                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // Откатываем транзакцию в случае ошибки
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при добавлении мероприятия: {ex.Message}", "Ошибка",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }


        private void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            if (EventsList.SelectedItem == null)
            {
                MessageBox.Show("Выберите мероприятие для удаления", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Event selectedEvent = (Event)EventsList.SelectedItem;

            MessageBoxResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить мероприятие '{selectedEvent.EventName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Начинаем транзакцию
                        SqlTransaction transaction = connection.BeginTransaction();

                        try
                        {
                            // 1. Удаляем связи из EventTypeDirection
                            string deleteRelationsQuery =
                                "DELETE FROM EventTypeDirection WHERE EventID = @EventID";
                            SqlCommand relationsCommand = new SqlCommand(deleteRelationsQuery, connection, transaction);
                            relationsCommand.Parameters.AddWithValue("@EventID", selectedEvent.EventID);
                            relationsCommand.ExecuteNonQuery();

                            // 2. Удаляем само мероприятие
                            string deleteEventQuery =
                                "DELETE FROM Events WHERE EventID = @EventID";
                            SqlCommand eventCommand = new SqlCommand(deleteEventQuery, connection, transaction);
                            eventCommand.Parameters.AddWithValue("@EventID", selectedEvent.EventID);
                            int rowsAffected = eventCommand.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                transaction.Commit();
                                MessageBox.Show("Мероприятие успешно удалено", "Успех",
                                                MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadEvents(); // Обновляем список
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при удалении мероприятия: {ex.Message}", "Ошибка",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        public class NewEventData
            {
                public string EventName { get; set; }
                public DateTime StartDate { get; set; }
                public int DurationDays { get; set; }
                public int OrganizerID { get; set; }
                public string Direction { get; set; }
                public string EventType { get; set; }
            }
            // Остальные методы (LoadEvents, LoadDirections, ApplyFilter_Click и т.д.) остаются без изменений
            // ...
        

      private void BackButton_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }
    }
}

  

        
    