; PlatformIO Project Configuration File
;
;   Build options: build flags, source filter
;   Upload options: custom upload port, speed and extra flags
;   Library options: dependencies, extra library storages
;   Advanced options: extra scripting
;
; Please visit documentation for the other options and examples
; https://docs.platformio.org/page/projectconf.html

[env:upesy_wroom]
platform = espressif32
board = upesy_wroom
framework = arduino
monitor_speed = 115200
lib_deps = 
	esphome/ESPAsyncWebServer-esphome@^3.1.0
	adafruit/Adafruit GFX Library@^1.11.9
	adafruit/Adafruit SSD1306@^2.5.9
	adafruit/DHT sensor library@^1.4.6
	moononournation/GFX Library for Arduino@^1.4.5
	olikraus/U8g2@^2.35.9
	knolleary/PubSubClient@^2.8
	bblanchon/ArduinoJson@^7.0.4
