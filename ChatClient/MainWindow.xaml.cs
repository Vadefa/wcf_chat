using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChatClient.ServiceChat;                   // чтобы не писать каждый раз ChatClient.ServiceChat, а только ServiceChat
using System.Data;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit; 

namespace ChatClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ServiceChat.IServiceChatCallback
    {
        bool isConnected = false;               // подключён ли клиент к сервису на данный момент
        ServiceChatClient client;   // нужно создать объект типа нашего хоста в нашем клиенте, чтобы мы могли взаимодействовать с его методами
        int ID;
        string clientName = "";
        object rank = 0;
        string currentChannel = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        void ConnectUser()
        {

            if (!isConnected)                   // если подключились
            {
                DB db = new DB();                                   // база данных
                DataTable table = new DataTable();                  // объект для преобразований комманд в таблицу, в которой можно работать с каждым из объектов
                MySqlDataAdapter adapter = new MySqlDataAdapter();  // объект для работы с коммандами и таблицами

                MySqlCommand command = new MySqlCommand("SELECT * FROM `userdata` WHERE `login` = @ul AND `password` = @up", db.GetConnection());
                command.Parameters.Add("@ul", MySqlDbType.VarChar).Value = tbUserName.Text;
                command.Parameters.Add("@up", MySqlDbType.VarChar).Value = tbPassword.Text;

                adapter.SelectCommand = command;                // получили комманду
                adapter.Fill(table);                            // трансформировали данные команды в табличку

                if (table.Rows.Count > 0)                       // если нашлось хоть одно поле с совпадающим логином
                {
                    client = new ServiceChatClient(new System.ServiceModel.InstanceContext(this));
                    ID = client.Connect(tbUserName.Text);
                    if (ID > 0)
                    {
                        clientName = tbUserName.Text;

                        connectVisibility();

                        setRankFunctionality();

                        bConnDiscon.Content = "Disconnect";
                        isConnected = true;
                    }

                    //выгрузка истории сообщений
                    showMessages();
                }
                else
                {
                    MessageBox.Show("Такого пользователя нет.");
                    tbUserName.Clear();
                }
            }
        }

        void DisconnectUser()
        {

            if (isConnected)                   // если ещё не подключены
            {
                clientName = "";
                rank = 0;
                client.Disconnect(ID);
                client = null;

                disconnectVisibility();

                bConnDiscon.Content = "Connect";
                isConnected = false;
                lbChat.Items.Clear();           // скрываем все сообщения
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (isConnected)
                DisconnectUser();
            else
                ConnectUser();
        }

        public void MsgCallback(string msg)
        {
            lbChat.Items.Add(msg);
            lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
            sendMails(msg);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            currentChannel = (tcChannelPages.Items[0] as TabItem).Header.ToString();

            labelSM.Visibility = Visibility.Hidden;
            tbSM.IsEnabled = false;
            tbSM.Visibility = Visibility.Hidden;
            imSMClose.IsEnabled = false;
            imSMClose.Visibility = Visibility.Hidden;

            btnChCreate.IsEnabled = false;
            btnChDelete.IsEnabled = false;
            tcChannelPages.IsEnabled = false;
            pChCrDel.IsEnabled = false;

            btnChCreate.Visibility = Visibility.Hidden;
            btnChDelete.Visibility = Visibility.Hidden;
            tcChannelPages.Visibility = Visibility.Hidden;
            pChCrDel.Visibility = Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectUser();
        }

        private void tbMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (tbMessage.Text == "")
                    return;

                if (client != null)
                {
                    client.SendMsg(tbMessage.Text, ID);     // отправляем текст на сервер для отображения всем подсоединённым клиентам

                    // сохраняем сообщение и информацию о нём в базу данных
                    DB db = new DB();

                    MySqlCommand command = new MySqlCommand($"INSERT INTO `{currentChannel}` (`date`, `time`, `name`, `text`) VALUES (@date, @time, @name, @text)", db.GetConnection());

                    command.Parameters.Add("@date", MySqlDbType.VarChar).Value = DateTime.Now.ToShortDateString();
                    command.Parameters.Add("@time", MySqlDbType.VarChar).Value = DateTime.Now.ToShortTimeString();
                    command.Parameters.Add("@name", MySqlDbType.VarChar).Value = clientName;
                    command.Parameters.Add("@text", MySqlDbType.Text).Value = tbMessage.Text;

                    db.openConnection();

                    if (command.ExecuteNonQuery() != 1)
                        MessageBox.Show("Сообщение не было отправлено");

                    db.closeConnection();

                    tbMessage.Text = string.Empty;
                }
            }
        }

        public bool isUserExists(string username)
        {
            DB db = new DB();
            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            MySqlCommand command = new MySqlCommand("SELECT * FROM `userdata` WHERE `login` = @ul", db.GetConnection());
            command.Parameters.Add("@ul", MySqlDbType.VarChar).Value = username;

            adapter.SelectCommand = command;                // получили комманду
            adapter.Fill(table);                            // трансформировали данные команды в табличку

            if (table.Rows.Count > 0)                       // если нашлось хоть одно поле с совпадающим логином
                return true;
            else
                return false;
        }
        private void bRegister_Click(object sender, RoutedEventArgs e)
        {
            if (tbUserName.Text == "" || tbPassword.Text == "")
            {
                MessageBox.Show("Заполните поля для вовда электронной почты и пароля");
                return;
            }

            if (isUserExists(tbUserName.Text))             // проверка в базе данных на существование пользователей с вводимым логином
            {
                MessageBox.Show("Пользователь с такой электронной почтой уже существует");
                return;
            }

            // регистрация происходит в базе данных
            DB db = new DB();

            MySqlCommand command = new MySqlCommand("INSERT INTO `userdata` (`login`, `password`, `rank`) VALUES (@login, @pass, @rank)", db.GetConnection());   // ID устанавливать не нужно, его удалили

            command.Parameters.Add("@login", MySqlDbType.VarChar).Value = tbUserName.Text;
            command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = tbPassword.Text;
            command.Parameters.Add("@rank", MySqlDbType.Int32).Value = rank;

            db.openConnection();

            if (command.ExecuteNonQuery() == 1)
                MessageBox.Show("Аккаунт был создан!");
            else
                MessageBox.Show("Аккаунт не был создан.");

            db.closeConnection();
        }
        public void disconnectVisibility()
        {
            tbUserName.IsEnabled = true;
            tbUserName.Visibility = Visibility.Visible;
            tbPassword.IsEnabled = true;
            tbPassword.Visibility = Visibility.Visible;
            labelLogin.Visibility = Visibility.Visible;
            labelPassword.Visibility = Visibility.Visible;
            bRegister.IsEnabled = true;
            bRegister.Visibility = Visibility.Visible;

            //поиск сообщений
            labelSM.Visibility = Visibility.Hidden;
            tbSM.IsEnabled = false;
            tbSM.Visibility = Visibility.Hidden;

            //каналы
            btnChCreate.IsEnabled = false;
            btnChDelete.IsEnabled = false;
            tcChannelPages.IsEnabled = false;

            btnChCreate.Visibility = Visibility.Hidden;
            btnChDelete.Visibility = Visibility.Hidden;
            tcChannelPages.Visibility = Visibility.Hidden;
        }
        public void connectVisibility()
        {
            tbUserName.IsEnabled = false;
            tbUserName.Visibility = Visibility.Hidden;
            tbUserName.Clear();
            tbPassword.IsEnabled = false;
            tbPassword.Visibility = Visibility.Hidden;
            tbPassword.Clear();
            labelLogin.Visibility = Visibility.Hidden;
            labelPassword.Visibility = Visibility.Hidden;
            bRegister.IsEnabled = false;
            bRegister.Visibility = Visibility.Hidden;

            //поиск сообщений
            labelSM.Visibility = Visibility.Visible;
            tbSM.IsEnabled = true;
            tbSM.Visibility = Visibility.Visible;
            imSMClose.IsEnabled = false;
            imSMClose.Visibility = Visibility.Hidden;

            //панель каналов
            tcChannelPages.IsEnabled = true;

            tcChannelPages.Visibility = Visibility.Visible;
        }

        private void tbSM_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (tbSM.Text == "")
                    return;

                // ищем сообщение в базе данных
                DB db = new DB();
                MySqlConnection connection = db.GetConnection();
                MySqlCommand command = new MySqlCommand($"SELECT * FROM `{currentChannel}` WHERE `text` LIKE @text", connection);
                command.Parameters.Add("@text", MySqlDbType.Text).Value = "%" + tbSM.Text + "%";
                connection.Open();

                lbChat.Items.Clear();

                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lbChat.Items.Add(reader["time"] + " " + reader["name"] + ": " + reader["text"]);
                }
                connection.Close();

                tbMessage.Text = string.Empty;
                tbMessage.IsEnabled = false;

                imSMClose.IsEnabled = true;
                imSMClose.Visibility = Visibility.Visible;
            }
        }
        public void showMessages()
        {
            DB db = new DB();
            MySqlCommand command = new MySqlCommand($"SELECT * FROM `{currentChannel}`", db.GetConnection());

            MySqlConnection connection = db.GetConnection();
            connection.Open();

            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                lbChat.Items.Add(reader["time"] + " " + reader["name"] + ": " + reader["text"]);
            }

            connection.Close();

            if (lbChat.Items.Count > 0)
                lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
        }

        private void imSMClose_Click(object sender, MouseButtonEventArgs e)
        {
            lbChat.Items.Clear();
            showMessages();
            tbSM.Text = "";

            tbMessage.IsEnabled = true;
            imSMClose.IsEnabled = false;
            imSMClose.Visibility = Visibility.Hidden;
        }

        public void setRankFunctionality ()
        {
            DB db = new DB();
            MySqlCommand command = new MySqlCommand("SELECT * FROM `userdata` WHERE `login` = @login", db.GetConnection());
            command.Parameters.Add("@login", MySqlDbType.VarChar).Value = clientName;
            MySqlConnection connection = db.GetConnection();
            connection.Open();

            MySqlDataReader reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                rank = reader["rank"];
            }

            connection.Close();

            if ((int)rank == 0)
                return;
            else if ((int)rank > 1)
            {
                // создание/удаление канала
                btnChCreate.IsEnabled = true;
                btnChDelete.IsEnabled = true;

                btnChCreate.Visibility = Visibility.Visible;
                btnChDelete.Visibility = Visibility.Visible;
            }

        }

        private void btnChCreate_Click(object sender, RoutedEventArgs e)
        {
            pChCrDel.IsEnabled = true;
            pForLogin.IsEnabled = true;
            pChCrDel.Visibility = Visibility.Visible;
            pForLogin.Visibility = Visibility.Visible;
            btnChCrDel.Content = "Создать";
        }

        private void btnChDelete_Click(object sender, RoutedEventArgs e)
        {
            pChCrDel.IsEnabled = true;
            pForLogin.IsEnabled = false;
            pChCrDel.Visibility = Visibility.Visible;
            pForLogin.Visibility = Visibility.Hidden;
            btnChCrDel.Content = "Удалить";
        }

        private void btnChCrDel_Click(object sender, RoutedEventArgs e)
        {
            if (tbChName.Text == "")
            {
                MessageBox.Show("Введите название канала");
                return;
            }
            if (pForLogin.IsEnabled)            // если канал создаётся
            {
                if (tbChManager.Text == "")
                {
                    MessageBox.Show("Введите имя руководителя отдела");
                    return;
                }

                if (isUserExists(tbChManager.Text) == false)
                {
                    MessageBox.Show("Пользователя с таким логином не существует");
                    return;
                }

                // создание канала в таблице каналов
                DB db = new DB();
                MySqlCommand command = new MySqlCommand("INSERT INTO `channels` (`name`, `manager`) VALUES(@name, @manager)", db.GetConnection());
                command.Parameters.Add("@name", MySqlDbType.VarChar).Value = tbChName.Text;
                command.Parameters.Add("@manager", MySqlDbType.VarChar).Value = tbChManager.Text;

                db.openConnection();
                if (command.ExecuteNonQuery() == 1)
                    MessageBox.Show("Канал создан");
                else
                    MessageBox.Show("Канал не создан");
                db.closeConnection();

                // обновление пользователя, который будет руководителем канала
                db = new DB();
                command = new MySqlCommand("UPDATE `userdata` SET `rank` = '1' WHERE `userdata`.`login` = @manager", db.GetConnection());
                command.Parameters.Add("@name", MySqlDbType.VarChar).Value = tbChName.Text;
                command.Parameters.Add("@manager", MySqlDbType.VarChar).Value = tbChManager.Text;

                db.openConnection();
                if (command.ExecuteNonQuery() != 1)
                    MessageBox.Show("Руководитель канала не обновлён");
                db.closeConnection();

                // создание таблицы для канала
                db = new DB();
                command = new MySqlCommand($"CREATE TABLE `users`.`{tbChName.Text}` ( `id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT, `date` VARCHAR(30) NOT NULL, `time` VARCHAR(30) NOT NULL, `name` VARCHAR(100) NOT NULL, `text` TEXT NOT NULL, UNIQUE `id` (`id`) )", db.GetConnection());

                db.openConnection();
                command.ExecuteNonQuery();
                db.closeConnection();

                TabItem newChannel = new TabItem();
                newChannel.Header = tbChName.Text;
                tcChannelPages.Items.Add(newChannel);
            }
            // если pForLogin не enabled, то мы удаляем канал
            else
            {
                if (tbChName.Text == "main")
                {
                    MessageBox.Show("Основной канал нельзя удалить");
                    return;
                }
                try
                {
                    DB db = new DB();
                    MySqlCommand command = new MySqlCommand($"DROP TABLE `users`.`{tbChName.Text}`", db.GetConnection());

                    db.openConnection();
                    command.ExecuteNonQuery();
                    db.closeConnection();

                    db = new DB();
                    command = new MySqlCommand($"DELETE FROM `channels` WHERE `channels`.`name` = \"{tbChName.Text}\"", db.GetConnection());

                    db.openConnection();
                    command.ExecuteNonQuery();
                    db.closeConnection();

                    for (int i = 0; i < tcChannelPages.Items.Count; i++)
                        if ((tcChannelPages.Items[i] as TabItem).Header.ToString() == tbChName.Text)
                        {
                            tcChannelPages.SelectedItem = tcChannelPages.Items[i - 1];
                            tcChannelPages.Items.RemoveAt(i);
                            break;
                        }

                    MessageBox.Show($"Канал {tbChName.Text} удалён.");
                }
                catch
                {
                    MessageBox.Show("Такого канала не существует");
                }
            }
            tbChName.Clear();
            tbChManager.Clear();
            pChCrDel.Visibility = Visibility.Hidden;
            pChCrDel.IsEnabled = false;
        }

        private void tcChannelPages_Loaded(object sender, RoutedEventArgs e)
        {
            DB db = new DB();
            MySqlCommand command = new MySqlCommand("SELECT * FROM `channels`", db.GetConnection());

            MySqlConnection connection = db.GetConnection();
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (tcChannelPages.Items.Contains(reader["name"]))
                    continue;
                else
                {
                    TabItem channel = new TabItem();
                    channel.Header = reader["name"];
                    tcChannelPages.Items.Add(channel);
                }
            }
            connection.Close();
        }

        private void tcChannelPages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (clientName != "")
            {
                lbChat.Items.Clear();
                currentChannel = (tcChannelPages.SelectedItem as TabItem).Header.ToString();
                showMessages();
            }
        }

        //почта webchatPicpo@gmail.com
        //пароль от почты - RashitRashit
        //пароль приложения для устройства: serh tgcx ghwe kjni
        public void sendMails(string msg)
        {
            // создаём объект mime message который мы собираемся заполнить данными сообщения
            MimeMessage message = new MimeMessage();
            // добавляем информацию об отправителе
            message.From.Add(new MailboxAddress("web-chat application", "webchatpicpo@gmail.com"));
            // добавляем почту получателя
            message.To.Add(MailboxAddress.Parse("suhinin.vadim2002@yandex.ru"));

            // тема сообщения
            message.Subject = "New message in the chat!";

            // тело сообщения. plain значит, что это простое сообщение, не содержащее ссылок и других объектов
            message.Body = new TextPart("plain")
            {
                Text = msg
            };

            // создаём SMTP клиента
            SmtpClient smtpClient = new SmtpClient();

            string emailAddress = "webchatpicpo@gmail.com";
            string password = "serhtgcxghwekjni";
            try
            {
                // подключаемся к gmail smtp серверу со включенным SSL
                smtpClient.Connect("smtp.gmail.com", 587, false);
                smtpClient.Authenticate(emailAddress, password);
                smtpClient.Send(message);

                //MessageBox.Show("Сообщение также отправлено по почте");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                // в любом случае отключаемся от клиента и освобождаем его объект
                smtpClient.Disconnect(true);
                smtpClient.Dispose();
            }
        }
    }
}
