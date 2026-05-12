// SerialSwitchController - ESP32-C6 firmware
// 透過 UART 接收指令控制內建 WS2812 RGB LED

#include <Adafruit_NeoPixel.h>

const int LED_PIN = 8;       // YD-ESP32-C6 的 WS2812 腳位
const int NUM_PIXELS = 1;

Adafruit_NeoPixel pixel(NUM_PIXELS, LED_PIN, NEO_GRB + NEO_KHZ800);
bool ledState = false;

void setup() {
  Serial.begin(115200);
  pixel.begin();
  pixel.setBrightness(50);  // 0-255，太亮會刺眼
  pixel.clear();
  pixel.show();

  delay(500);
  Serial.println("READY");
}

void loop() {
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();

    if (command == "ON") {
      ledState = true;
      pixel.setPixelColor(0, pixel.Color(255, 255, 255));  // 白色
      pixel.show();
      Serial.println("STATE:ON");
    }
    else if (command == "GREEN") {
      ledState = true;
      pixel.setPixelColor(0, pixel.Color(0, 255, 0));  // 綠色
      pixel.show();
      Serial.println("STATE:GREEN");
    }
    else if (command == "RED") {
      ledState = true;
      pixel.setPixelColor(0, pixel.Color(255, 0, 0));  // 紅色
      pixel.show();
      Serial.println("STATE:RED");
    }
    else if (command == "BLUE") {
      ledState = true;
      pixel.setPixelColor(0, pixel.Color(0, 0, 255));  // 藍色
      pixel.show();
      Serial.println("STATE:BLUE");
    }
    else if (command == "OFF") {
      ledState = false;
      pixel.clear();
      pixel.show();
      Serial.println("STATE:OFF");
    }
    else if (command == "STATUS?") {
      Serial.println(ledState ? "STATE:ON" : "STATE:OFF");
    }
    else {
      Serial.println("ERROR:UNKNOWN");
    }
  }
}