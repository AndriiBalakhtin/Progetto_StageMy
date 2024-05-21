#include <WiFi.h>
#include <SPI.h>
#include <DHT.h>
#include <Adafruit_Sensor.h>
#include <WiFiClient.h>
#include <WebServer.h>
#include <uri/UriBraces.h>
#include <Adafruit_I2CDevice.h>
#include <U8g2lib.h>
#include <PubSubClient.h> 

#define WIFI_SSID "HotPost"
#define WIFI_PASSWORD "11223344"
#define WIFI_CHANNEL 6

const char* mqtt_server = "broker.hivemq.com";
const char* mqtt_topic = "esp32";

WebServer server(80);
WiFiClient espClient;
PubSubClient client(espClient);

U8G2_SH1106_128X64_NONAME_F_HW_I2C u8g2(U8G2_R0, /* reset=*/ U8X8_PIN_NONE);

const int LEDGREEN = 17;
const int LEDRED = 5;
const int LEDBLUE = 18;
const int LEDConnection = 2;
const int DHT_PIN = 4;
const int DHT_TYPE = DHT11;
float latestTemperature = 0.0;
float latestHumidity = 0.0;
DHT dht(DHT_PIN, DHT_TYPE);

bool LedRedState = false;
bool LedGreenState = false;
bool LedBlueState = false;
bool TrymqttConnect = true;
bool isFirstConnection = true;
bool WiFiConnected = false;
bool LostConnection = false;

void sendHtml() {
  float temp = dht.readTemperature();
  float humi = dht.readHumidity();

  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  String ledRColor = LedRedState ? "green" : "red";
  String ledGColor = LedGreenState ? "green" : "red";
  String ledBColor = LedBlueState ? "green" : "red";

  String ipAddress = WiFi.localIP().toString();

  String response = 
    "<!DOCTYPE html>\n"
    "<html>\n"
    "<head>\n"
    "  <title>ESP32 Web Server Online</title>\n"
    "  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n"
    "  <style>\n"
    "    /* Ваш стиль CSS */\n"
    "    body {\n"
    "      font-family: Arial, sans-serif;\n"
    "    }\n"
    "    .container {\n"
    "      display: inline-block;\n"
    "      margin: 10px;\n"
    "      vertical-align: top;\n"
    "      width: 300px;\n"
    "      border: 2px solid #ccc;\n"
    "      border-radius: 10px;\n"
    "      padding: 20px;\n"
    "    }\n"
    "    .btn-container {\n"
    "      text-align: center;\n"
    "    }\n"
    "    .btn {\n"
    "      display: block;\n"
    "      padding: 10px 20px;\n"
    "      font-size: 16px;\n"
    "      text-decoration: none;\n"
    "      background-color: #f44336;\n"
    "      color: white;\n"
    "      border: none;\n"
    "      border-radius: 5px;\n"
    "      cursor: pointer;\n"
    "      margin: 10px auto;\n"
    "      font-weight: bold;\n"
    "    }\n"
    "    .btn:hover {\n"
    "      background-color: #d32f2f;\n"
    "    }\n"
    "  </style>\n"
    "</head>\n"
    "<body>\n"
    "  <h1>ESP32 Web Server By Andrii Balakhtin</h1>\n"
    "  <div class=\"container\">\n"
    "    <h2>SMD RGB LED Controller</h2>\n"
    "    <div class=\"btn-container\">\n"
    "      <a href=\"/toggle/1\" class=\"btn LEDRed_TEXT\" style=\"background-color: " + ledRColor + "\">Toggle RED</a>\n"
    "      <a href=\"/toggle/2\" class=\"btn LEDGreen_TEXT\" style=\"background-color: " + ledGColor + "\">Toggle GREEN</a>\n"
    "      <a href=\"/toggle/3\" class=\"btn LEDBlue_TEXT\" style=\"background-color: " + ledBColor + "\">Toggle BLUE</a>\n"
    "    </div>\n"
    "  </div>\n"
    "  <div class=\"container\">\n"
    "    <h2>Sensor Data</h2>\n"
    "    <p>IP Address: " + ipAddress + "</p>\n"
    "    <p>Temperature: <span id=\"temperature\">" + String(temp) + "</span> °C</p>\n"
    "    <p>Humidity: <span id=\"humidity\">" + String(humi) + "</span> %</p>\n"
    "  </div>\n"
    "  <script>\n"
    "    //i don't know javascript aoaooaoao, i know only ahk\n"
    "  </script>\n"
    "</body>\n"
    "</html>\n";

  response += "</h2></body><meta http-equiv=\"refresh\" content=\"60\"></html>";

  server.send(200, "text/html", response);
}


void callback(char* topic, byte* payload, unsigned int length) {
  if (TrymqttConnect) {
    Serial.print("Message arrived [");
    Serial.print(topic);
    Serial.print("] ");
    for (int i = 0; i < length; i++) {
      Serial.print((char)payload[i]);
    }
    Serial.println();
  }

  String payloadStr = "";
  for (int i = 0; i < length; i++) {
    payloadStr += (char)payload[i];
  } 

  if (strcmp(topic, "RJ5nE3uVKZMdeYK1Hr2Po9zNOtFKlxqjWz6YvSpnteqLAI0GsC8Dw") == 0) {
    if ((char)payload[0] == '1') {
      digitalWrite(LEDRED, HIGH);
      LedRedState = true;
    } else {
      digitalWrite(LEDRED, LOW);
      LedRedState = false;
    }
  } else if (strcmp(topic, "X1jGdY3pRf8sQc9Vw2LzWtJqUl7Pe0yZmHFnK45kONiMb6IaTSvuo") == 0) {
    if ((char)payload[0] == '1') {
      digitalWrite(LEDGREEN, HIGH);
      LedGreenState = true;
    } else {
      digitalWrite(LEDGREEN, LOW);
      LedGreenState = false;
    }
  } else if (strcmp(topic, "aY9oJpRlF4hG7dHtO2sMnWqK6bL1xZ5cVgIuTzXeC8yE3wQvUkDf") == 0) {
    if ((char)payload[0] == '1') {
      digitalWrite(LEDBLUE, HIGH);
      LedBlueState = true;
    } else {
      digitalWrite(LEDBLUE, LOW);
      LedBlueState = false;
    }
  }
}

void setup(void) {
  Serial.begin(115200);
  pinMode(LEDRED, OUTPUT);
  pinMode(LEDGREEN, OUTPUT);
  pinMode(LEDBLUE, OUTPUT);
  pinMode(LEDConnection, OUTPUT);
  dht.begin();
  u8g2.begin();
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD, WIFI_CHANNEL);
  Serial.print("Connecting to WiFi ");
  Serial.print(WIFI_SSID);
  while (WiFi.status() != WL_CONNECTED) {
    delay(100);
    Serial.print(".");
    u8g2.clearBuffer();
    u8g2.setFont(u8g2_font_ncenB10_tr);
    u8g2.drawStr(20, 35, "Connecting...");
    u8g2.sendBuffer();
    digitalWrite(LEDConnection, HIGH);
    digitalWrite(LEDGREEN, HIGH);
    digitalWrite(LEDRED, HIGH);
  }
  digitalWrite(LEDGREEN, LOW);
  digitalWrite(LEDRED, LOW);
  digitalWrite(LEDConnection, LOW);
  server.on("/", HTTP_GET, sendHtml);

  server.on("/reload", HTTP_GET, []() {
    server.sendHeader("Location", "/", true);
    server.send(302, "text/plain", "");
  });

  server.on("/toggle/1", HTTP_GET, []() {
    LedRedState = !LedRedState;
    digitalWrite(LEDRED, LedRedState);
    server.sendHeader("Location", "/", true);
    server.send(302, "text/plain", "");
    client.publish("RJ5nE3uVKZMdeYK1Hr2Po9zNOtFKlxqjWz6YvSpnteqLAI0GsC8Dw", LedRedState ? "1" : "0");
  });

  server.on("/toggle/2", HTTP_GET, []() {
    LedGreenState = !LedGreenState;
    digitalWrite(LEDGREEN, LedGreenState);
    server.sendHeader("Location", "/", true);
    server.send(302, "text/plain", "");
    client.publish("X1jGdY3pRf8sQc9Vw2LzWtJqUl7Pe0yZmHFnK45kONiMb6IaTSvuo", LedGreenState ? "1" : "0");
  });

  server.on("/toggle/3", HTTP_GET, []() {
    LedBlueState = !LedBlueState;
    digitalWrite(LEDBLUE, LedBlueState);
    server.sendHeader("Location", "/", true);
    server.send(302, "text/plain", "");
    client.publish("aY9oJpRlF4hG7dHtO2sMnWqK6bL1xZ5cVgIuTzXeC8yE3wQvUkDf", LedBlueState ? "1" : "0");
  });

  server.begin();
  Serial.println("HTTP server started");

  client.setServer(mqtt_server, 8883);
  client.setCallback(callback);
  client.setServer(mqtt_server, 1883);
  client.setCallback(callback);
}

void BlinkLedDisconnected() {
    if (WiFi.status() != WL_CONNECTED)
    {
      digitalWrite(LEDBLUE, LOW);
      digitalWrite(LEDGREEN, LOW);

      digitalWrite(LEDConnection, HIGH);
      digitalWrite(LEDRED, HIGH);
      delay(500);
      digitalWrite(LEDConnection, LOW);
      digitalWrite(LEDRED, LOW);
      delay(500);
    }
}

void publishHumidity(float humidity) {
  if (!isnan(humidity) && client.connected()) {
    char humidityStr[10];
    dtostrf(humidity, 4, 2, humidityStr);
    for (int i = 0; i < strlen(humidityStr); i++) {
      if (humidityStr[i] == '.') {
        humidityStr[i] = ',';
      }
    }
    client.publish("KKOpWz9PoGPSzzQ39lfkCXNzXHHZm1oo1KK8fjJSKW992ZxJqWddNw", humidityStr);
  }
}

void publishTemperature(float temperature) {
  if (!isnan(temperature) && client.connected()) {
    char temperatureStr[10];
    dtostrf(temperature, 4, 2, temperatureStr);
    for (int i = 0; i < strlen(temperatureStr); i++) {
      if (temperatureStr[i] == '.') {
        temperatureStr[i] = ',';
      }
    }
    client.publish("JkxkZZAPKlpOwiIxZSpWWlPsbkeoZZ9GjKspWPnqZKlLqADdswLkMl", temperatureStr);
  }
}

bool ConnectionMQTT() {
  if (!client.connected()) {
    Serial.print("Attempting MQTT connection...");
    if (client.connect("ESP32Client")) {
      Serial.println("connected");
      client.subscribe("RJ5nE3uVKZMdeYK1Hr2Po9zNOtFKlxqjWz6YvSpnteqLAI0GsC8Dw");
      client.subscribe("X1jGdY3pRf8sQc9Vw2LzWtJqUl7Pe0yZmHFnK45kONiMb6IaTSvuo");
      client.subscribe("aY9oJpRlF4hG7dHtO2sMnWqK6bL1xZ5cVgIuTzXeC8yE3wQvUkDf");
      TrymqttConnect = true;
      return true;
    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      Serial.println(" try again in 5 seconds");
      delay(500);
      TrymqttConnect = false;
      return false;
    }
  }
  TrymqttConnect = true;
  return true;
}
void ConnectionWiFi() {
  if (WiFi.status() == WL_CONNECTED) {
    if (isFirstConnection) {
      Serial.println("┌────────────┐");
      Serial.println("| Success!!! |");
      Serial.println("└────────────┘");
      Serial.print("Connected to WiFi ");
      Serial.println(WIFI_SSID);
      Serial.print("IP-Adress: " );
      Serial.println(WiFi.localIP());
      u8g2.clearBuffer();
      u8g2.setFont(u8g2_font_ncenB10_tr);
      u8g2.drawStr(10, 30, "Connected!!!");
      BlinkLedDisconnected();
      u8g2.sendBuffer();
      delay(500);
      isFirstConnection = false;
      WiFiConnected = true;
    }

    char ipAddress[16];
    sprintf(ipAddress, "%d.%d.%d.%d", WiFi.localIP()[0], WiFi.localIP()[1], WiFi.localIP()[2], WiFi.localIP()[3]);
    u8g2.clearBuffer();
    u8g2.setFont(u8g2_font_ncenB10_tr);
    u8g2.drawStr(20, 35, ipAddress);
    u8g2.sendBuffer();
  }
}
void ReconnectWiFi() {
  if (WiFi.status() != WL_CONNECTED) {
    if (!LostConnection) {
      Serial.println("┌────────────┐");
      Serial.println("| Failure!!! |");
      Serial.println("└────────────┘");
      LostConnection = true;
    }
    Serial.println("Lost Connection");
    u8g2.clearBuffer();
    u8g2.setFont(u8g2_font_ncenB10_tr);
    u8g2.drawStr(54, 30, "Lost");
    u8g2.drawStr(25, 45, "Connection");
    u8g2.sendBuffer();
    BlinkLedDisconnected();
    WiFiConnected = true;
  } else {
    if (WiFiConnected) {
      Serial.println("┌────────────┐");
      Serial.println("| Success!!! |");
      Serial.println("└────────────┘");
      Serial.print("Connected to WiFi ");
      Serial.println(WIFI_SSID);
      Serial.print("IP-Address: ");
      Serial.println(WiFi.localIP());
      LostConnection = false;
      WiFiConnected = false;
    }
  }
}

void loop(void) {
  server.handleClient();
  ConnectionWiFi();
  ReconnectWiFi();

  if (ConnectionMQTT()) {
    float temperature = dht.readTemperature();
    float humidity = dht.readHumidity();
    publishHumidity(humidity);
    publishTemperature(temperature);
    client.loop();
  }
  delay(5000);
} 