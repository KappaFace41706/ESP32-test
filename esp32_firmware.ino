#include <Adafruit_NeoPixel.h>

const int LED_PIN = 8;
const int NUM_PIXELS = 1;

Adafruit_NeoPixel pixel(NUM_PIXELS, LED_PIN, NEO_GRB + NEO_KHZ800);

enum class LedMode {
  Off,
  On,
  Color,
  RgbCycle
};

struct RgbColor {
  uint8_t r;
  uint8_t g;
  uint8_t b;
};

LedMode currentMode = LedMode::Off;
RgbColor currentColor = {0, 0, 0};

int rgbIndex = 0;
unsigned long lastChangeTime = 0;
const unsigned long RGB_INTERVAL = 1000;

void setup() {
  Serial.begin(115200);
  pixel.begin();
  pixel.setBrightness(50);
  pixel.clear();
  pixel.show();

  delay(500);
  Serial.println("READY");
}

void loop() {
  if (currentMode == LedMode::RgbCycle) {
    unsigned long now = millis();
    if (now - lastChangeTime >= RGB_INTERVAL) {
      lastChangeTime = now;
      switch (rgbIndex) {
        case 0: pixel.setPixelColor(0, pixel.Color(255, 0, 0)); break;
        case 1: pixel.setPixelColor(0, pixel.Color(0, 255, 0)); break;
        case 2: pixel.setPixelColor(0, pixel.Color(0, 0, 255)); break;
      }
      pixel.show();
      rgbIndex = (rgbIndex + 1) % 3;
    }
  }

  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    handleCommand(command);
  }
}

void handleCommand(const String& command) {
  if (command == "ON") {
    currentMode = LedMode::On;
    pixel.setPixelColor(0, pixel.Color(255, 255, 255)); // 預設為白光
    pixel.show();
    Serial.println("STATE:ON");
  }
  else if (command == "OFF") {
    currentMode = LedMode::Off;
    pixel.clear();
    pixel.show();
    Serial.println("STATE:OFF");
  }
  else if (command == "RGB") {
    currentMode = LedMode::RgbCycle;
    rgbIndex = 0;
    lastChangeTime = millis() - RGB_INTERVAL;
    Serial.println("STATE:RGB");
  }
  else if (command.startsWith("SET COLOR=")) {
    setColor(command);
  }
  else if (command == "STATUS?") {
    sendStatus();
  }
  else {
    Serial.println("ERROR:UNKNOWN");
  }
}

void setColor(const String& command) {
  String params = command.substring(10);

  RgbColor color;
  if (!parseRgb(params, color)) {
    Serial.println("ERROR:BAD_FORMAT");
    return;
  }

  currentMode = LedMode::Color;
  currentColor = color;
  pixel.setPixelColor(0, pixel.Color(color.r, color.g, color.b));
  pixel.show();

  Serial.print("STATE:COLOR=");
  Serial.print(color.r);
  Serial.print(",");
  Serial.print(color.g);
  Serial.print(",");
  Serial.println(color.b);
}

bool parseRgb(const String& input, RgbColor& result) {
  int firstComma = input.indexOf(',');
  int secondComma = input.indexOf(',', firstComma + 1);

  if (firstComma == -1 || secondComma == -1) {
    return false;
  }

  String rStr = input.substring(0, firstComma);
  String gStr = input.substring(firstComma + 1, secondComma);
  String bStr = input.substring(secondComma + 1);

  if (!isNumeric(rStr) || !isNumeric(gStr) || !isNumeric(bStr)) {
    return false;
  }

  int r = rStr.toInt();
  int g = gStr.toInt();
  int b = bStr.toInt();

  if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255) {
    return false;
  }

  result.r = (uint8_t)r;
  result.g = (uint8_t)g;
  result.b = (uint8_t)b;
  return true;
}

bool isNumeric(const String& str) {
  if (str.length() == 0) return false;
  for (size_t i = 0; i < str.length(); i++) {
    if (!isDigit(str.charAt(i))) return false;
  }
  return true;
}

void sendStatus() {
  switch (currentMode) {
    case LedMode::Off:
      Serial.println("STATE:OFF");
      break;
    case LedMode::On:
      Serial.println("STATE:ON");
      break;
    case LedMode::RgbCycle:
      Serial.println("STATE:RGB");
      break;
    case LedMode::Color:
      Serial.print("STATE:COLOR=");
      Serial.print(currentColor.r);
      Serial.print(",");
      Serial.print(currentColor.g);
      Serial.print(",");
      Serial.println(currentColor.b);
      break;
  }
}