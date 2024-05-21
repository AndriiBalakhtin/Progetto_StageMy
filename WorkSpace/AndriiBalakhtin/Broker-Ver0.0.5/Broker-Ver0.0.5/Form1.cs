// Andrii Balakhtin 2IoT 12.04.2024

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using MySql.Data.MySqlClient;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace Broker_Ver0._0._5
{
public partial class Form1 : Form
{
    string connectionString = "server=127.0.0.1;database=sensordata;uid=root"; // put here you're database!!!
    private MqttClient mqttClient;
    private PlotView plotView1;
    private LineSeries temperatureSeries;
    private LineSeries humiditySeries;
    private Button buttonRed;
    private Button buttonGreen;
    private Button buttonBlue;
    private Label Humidity;
    private Label HumidityDesc;
    private Label HumidityElement;
    private Label Temperature;
    private Label TemperatureDesc;
    private Label TemperatureElement;

    private bool LedRedState = false;
    private bool LedGreenState = false;
    private bool LedBlueState = false;

        public Form1()
        {
            InitializeComponent();
            InitializeObjects();
            InitializeMQTT();
            InitializeDatabase();
            StartDataSimulation();
            SendDateToMySql();
            this.FormClosing += Form1_FormClosing;
            MinimumSize = new Size(810, 300);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // Empty
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                mqttClient.Unsubscribe(new string[] { "JkxkZZAPKlpOwiIxZSpWWlPsbkeoZZ9GjKspWPnqZKlLqADdswLkMl", "KKOpWz9PoGPSzzQ39lfkCXNzXHHZm1oo1KK8fjJSKW992ZxJqWddNw" }); // temperature and humidity
                    mqttClient.Disconnect();
                Environment.Exit(0);
            }

        }
        private void InitializeObjects()
        {
            // Buttons
            buttonRed = new Button
            {
                Text = "RED LED OFF",
                Location = new System.Drawing.Point(60, 420),
                Size = new System.Drawing.Size(105, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            buttonRed.Click += ButtonRed_Click;
            Controls.Add(buttonRed);

            buttonGreen = new Button
            {
                Text = "GREEN LED OFF",
                Location = new System.Drawing.Point(170, 420),
                Size = new System.Drawing.Size(105, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            buttonGreen.Click += ButtonGreen_Click;
            Controls.Add(buttonGreen);

            buttonBlue = new Button
            {
                Text = "BLUE LED OFF",
                Location = new System.Drawing.Point(280, 420),
                Size = new System.Drawing.Size(105, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            buttonBlue.Click += ButtonBlue_Click;
            Controls.Add(buttonBlue);
            //  Labels
            Humidity = new Label
            {
                Text = "N/A",
                Location = new System.Drawing.Point(135, 5),
                Size = new System.Drawing.Size(30, 15)
            };
            Controls.Add(Humidity);

            HumidityDesc = new Label
            {
                Text = "Humidity, ",
                Location = new System.Drawing.Point(60, 5),
                Size = new System.Drawing.Size(80, 15)
            };
            Controls.Add(HumidityDesc);

            HumidityElement = new Label
            {
                Text = "%",
                Location = new System.Drawing.Point(162, 5),
                Size = new System.Drawing.Size(20, 15)
            };
            Controls.Add(HumidityElement);

            Temperature = new Label
            {
                Text = "N/A",
                Location = new System.Drawing.Point(135, 28),
                Size = new System.Drawing.Size(30, 15)
            };
            Controls.Add(Temperature);

            TemperatureDesc = new Label
            {
                Text = "Temperature, ",
                Location = new System.Drawing.Point(60, 28),
                Size = new System.Drawing.Size(80, 15)
            };
            Controls.Add(TemperatureDesc);

            TemperatureElement = new Label
            {
                Text = "°C",
                Location = new System.Drawing.Point(162, 28),
                Size = new System.Drawing.Size(30, 15)
            };
            Controls.Add(TemperatureElement);
            // Initialize Plot
            plotView1 = new PlotView
            {
                Dock = DockStyle.Fill,
                Location = new System.Drawing.Point(10, 100)
            };
            Controls.Add(plotView1);

            var plotModel = new PlotModel { Title = "Temperature and Humidity " };
            temperatureSeries = new LineSeries { Title = "Temperature", Color = OxyColors.Red };
            humiditySeries = new LineSeries { Title = "Humidity", Color = OxyColors.Blue };
            plotModel.Series.Add(temperatureSeries);
            plotModel.Series.Add(humiditySeries);
            plotModel.Axes.Add(new OxyPlot.Axes.DateTimeAxis { Position = OxyPlot.Axes.AxisPosition.Bottom, Title = "Time" });
            plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Title = "Value" });
            plotView1.Model = plotModel;         
        }       
        private void InitializeMQTT()
        {
            try
            {
                mqttClient = new MqttClient("broker.hivemq.com");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Check you're internet!");
       
                Environment.Exit(0);
                return;
            }
            mqttClient.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
            string clientId = Guid.NewGuid().ToString();
            mqttClient.Connect(clientId);
            mqttClient.Subscribe(new string[] { "JkxkZZAPKlpOwiIxZSpWWlPsbkeoZZ9GjKspWPnqZKlLqADdswLkMl", "KKOpWz9PoGPSzzQ39lfkCXNzXHHZm1oo1KK8fjJSKW992ZxJqWddNw" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }
        private async Task UpdateTemperatureLabelAsync(string message)
        {
            await Task.Run(() =>
            {
                if (Temperature.InvokeRequired)
                {
                    Temperature.Invoke(new Action(() => Temperature.Text = message));
                }
            });
        }
        private async Task UpdateHumidityLabelAsync(string message)
        {
            await Task.Run(() =>
            {
                if (Humidity.InvokeRequired)
                {
                    Humidity.Invoke(new Action(() => Humidity.Text = message));
                }
            });
        }
        private void MqttClient_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string topic = e.Topic;
            string message = System.Text.Encoding.UTF8.GetString(e.Message);

            if (topic == "JkxkZZAPKlpOwiIxZSpWWlPsbkeoZZ9GjKspWPnqZKlLqADdswLkMl")
            {
                double temperature = double.Parse(message);
                UpdateTemperatureLabelAsync(message);

            }
            else if (topic == "KKOpWz9PoGPSzzQ39lfkCXNzXHHZm1oo1KK8fjJSKW992ZxJqWddNw")
            {
                double humidity = double.Parse(message);
                UpdateHumidityLabelAsync(message);
            }
        }
        private void SaveSensorData(DateTime timestamp, double? temperature, double? humidity)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO AndriiBalakhtinHouse (Timestamp, Temperature, Humidity) VALUES (@Timestamp, @Temperature, @Humidity)";
                MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Timestamp", timestamp);
                insertCommand.Parameters.AddWithValue("@Temperature", temperature ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Humidity", humidity ?? (object)DBNull.Value);
                insertCommand.ExecuteNonQuery();
            }
        }
        private void InitializeDatabase()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS AndriiBalakhtinHouse (
                        ID INT AUTO_INCREMENT PRIMARY KEY,
                        Timestamp DATETIME,
                        Temperature DOUBLE,
                        Humidity DOUBLE
                    )";
                    MySqlCommand createTableCommand = new MySqlCommand(createTableQuery, connection);
                    createTableCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database: {ex.Message}");
                Environment.Exit(1);
            }
        }
        private void SimulateData(double temperature, double humidity)
        {
            this.Invoke((MethodInvoker)delegate
            {
                temperatureSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Now), temperature));
                humiditySeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Now), humidity));
                plotView1.InvalidatePlot(true);
            });
        }
        private void SendDateToMySql()
        {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 60000; //every 60 sec send date
            timer.Tick += (sender, e) =>
            {
                string TemperatureStr = Temperature.Text;
                string HumidityStr = Humidity.Text;

                double d1 = 0.0;
                double d2 = 0.0;

                double.TryParse(TemperatureStr, out d1);
                double.TryParse(HumidityStr, out d2);

                SaveSensorData(DateTime.Now, d1, d2);
            };
            timer.Start();
        }
        private void StartDataSimulation()
        {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += (sender, e) =>
            {
                string TemperatureStr = Temperature.Text;
                string HumidityStr = Humidity.Text;

                double d1 = 0.0;
                double d2 = 0.0;

                double.TryParse(TemperatureStr, out d1);
                double.TryParse(HumidityStr, out d2);

                SimulateData(d1, d2);
                if (LedRedState) // 100% way to pubblish for buttons, cuz i like cycles
                {
                    mqttClient.Publish("RJ5nE3uVKZMdeYK1Hr2Po9zNOtFKlxqjWz6YvSpnteqLAI0GsC8Dw", System.Text.Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    buttonRed.Text = "RED LED ON";
                }
                else
                {
                    mqttClient.Publish("RJ5nE3uVKZMdeYK1Hr2Po9zNOtFKlxqjWz6YvSpnteqLAI0GsC8Dw", System.Text.Encoding.UTF8.GetBytes("0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    buttonRed.Text = "RED LED OFF";
                }

                if (LedGreenState)
                {
                    mqttClient.Publish("X1jGdY3pRf8sQc9Vw2LzWtJqUl7Pe0yZmHFnK45kONiMb6IaTSvuo", System.Text.Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    buttonGreen.Text = "GREEN LED ON";
                }
                else
                {
                    mqttClient.Publish("X1jGdY3pRf8sQc9Vw2LzWtJqUl7Pe0yZmHFnK45kONiMb6IaTSvuo", System.Text.Encoding.UTF8.GetBytes("0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    buttonGreen.Text = "GREEN LED OFF";
                }

                if (LedBlueState)
                {
                    mqttClient.Publish("aY9oJpRlF4hG7dHtO2sMnWqK6bL1xZ5cVgIuTzXeC8yE3wQvUkDf", System.Text.Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    buttonBlue.Text = "BLUE LED ON";
                }
                else
                {
                    mqttClient.Publish("aY9oJpRlF4hG7dHtO2sMnWqK6bL1xZ5cVgIuTzXeC8yE3wQvUkDf", System.Text.Encoding.UTF8.GetBytes("0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    buttonBlue.Text = "BLUE LED OFF";
                }
            };
            timer.Start();          
        }
        private void ButtonRed_Click(object sender, EventArgs e)
        {
            LedRedState = !LedRedState;
            if (LedRedState)
            {
                mqttClient.Publish("RJ5nE3uVKZMdeYK1Hr2Po9zNOtFKlxqjWz6YvSpnteqLAI0GsC8Dw", System.Text.Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                buttonRed.Text = "RED LED ON";
            }
            else
            {
                mqttClient.Publish("RJ5nE3uVKZMdeYK1Hr2Po9zNOtFKlxqjWz6YvSpnteqLAI0GsC8Dw", System.Text.Encoding.UTF8.GetBytes("0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                buttonRed.Text = "RED LED OFF";
            }
        }
        private void ButtonGreen_Click(object sender, EventArgs e)
        {
            LedGreenState = !LedGreenState;
            if (LedGreenState)
            {
                mqttClient.Publish("X1jGdY3pRf8sQc9Vw2LzWtJqUl7Pe0yZmHFnK45kONiMb6IaTSvuo", System.Text.Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                buttonGreen.Text = "GREEN LED ON";
            }
            else
            {
                mqttClient.Publish("X1jGdY3pRf8sQc9Vw2LzWtJqUl7Pe0yZmHFnK45kONiMb6IaTSvuo", System.Text.Encoding.UTF8.GetBytes("0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                buttonGreen.Text = "GREEN LED OFF";
            }
        }
        private void ButtonBlue_Click(object sender, EventArgs e)
        {
            LedBlueState = !LedBlueState;
            if (LedBlueState)
            {
                mqttClient.Publish("aY9oJpRlF4hG7dHtO2sMnWqK6bL1xZ5cVgIuTzXeC8yE3wQvUkDf", System.Text.Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                buttonBlue.Text = "BLUE LED ON";
            }
            else
            {
                mqttClient.Publish("aY9oJpRlF4hG7dHtO2sMnWqK6bL1xZ5cVgIuTzXeC8yE3wQvUkDf", System.Text.Encoding.UTF8.GetBytes("0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                buttonBlue.Text = "BLUE LED OFF";
            }
        }
    }
}


