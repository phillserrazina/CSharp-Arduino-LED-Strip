#include <FastLED.h>

#define NUM_LEDS 30
#define LED_PIN 3

CRGB leds[NUM_LEDS];

const int powerPin = 13;

static long commandChar;
static long commandValue;

bool powerOn = true;

/*
 * MODES:
 * 0: Color Wipe
 * 1: Worm
 * 2: Waveform
 */
long mode = 0;

void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  
  pinMode(powerPin, OUTPUT);
  digitalWrite(powerPin, LOW);

  FastLED.addLeds<WS2812B, LED_PIN, GRB>(leds, NUM_LEDS);
  FastLED.setBrightness(50);

  for(int i = 0; i < NUM_LEDS ; i++) {
    leds[i] = CRGB(0,0,0);
  }
  FastLED.show();
}

long redVal = 0;
long greenVal = 0;
long blueVal = 0;
long brightnessVal = 1;

float actualBrightness = 0;

void loop() {
  //digitalWrite(powerPin, powerOn ? HIGH : LOW);

  if (Serial.available() > 0) {
    commandChar = Serial.read();

    if (commandChar = 'm') {
      mode = Serial.parseInt();
      if (mode < 0) mode == 0;
      if (mode > 2) mode == 2;
    }
    
    if (commandChar = 'r') {
      redVal = Serial.parseInt();
      if (redVal >= 255) redVal = 255;
    }

    if (commandChar = 'g') {
      greenVal = Serial.parseInt();
      if (greenVal >= 255) greenVal = 255;
    }

    if (commandChar = 'b') {
      blueVal = Serial.parseInt();
      if (blueVal >= 255) blueVal = 255;
    }

    if (commandChar = 'a') {
      brightnessVal = Serial.parseInt();
      if (brightnessVal >= 255) brightnessVal = 255;
    }
  }
  else {
    brightnessVal = 0;
  }

  if (brightnessVal <= 0) brightnessVal = 1;
  actualBrightness = brightnessVal / 255.0;

  if (mode == 0)
    ColorWipeMode();
  else if (mode == 1)
    WormMode(2, 'm');
  else if (mode == 2)
    WaveformMode(0.032);
  
  FastLED.show();
}

void ColorWipeMode() {
  for(int i = 0; i < NUM_LEDS; i++) {
    leds[i] = CRGB(redVal * actualBrightness, greenVal * actualBrightness, blueVal * actualBrightness);
  }
}

void WormMode(int updateLEDs, char origin) {
  if (origin == 'r')
  {
    for(int i = NUM_LEDS - 1; i >= updateLEDs; i--) {
      leds[i] = leds[i - updateLEDs];
    }
  
    for(int i = 0; i < updateLEDs; i++) {
      leds[i] = CRGB(redVal * actualBrightness, greenVal * actualBrightness, blueVal * actualBrightness);
    }   
  }
  else if (origin == 'l')
  {
    for(int i = 0; i <= NUM_LEDS - updateLEDs - 1; i++) {
      leds[i] = leds[i + updateLEDs];
    }
    
    for(int i = NUM_LEDS - 1; i > NUM_LEDS - updateLEDs - 1; i--) {
      leds[i] = CRGB(redVal * actualBrightness, greenVal * actualBrightness, blueVal * actualBrightness);
    }
  }
  else if (origin == 'm') {
    int originIndex = (NUM_LEDS / 2);

    for(int i = 0; i < originIndex - updateLEDs; i++) {
      leds[i] = leds[i + updateLEDs];
    }

    for(int i = NUM_LEDS - 1; i >= originIndex + updateLEDs; i--) {
      leds[i] = leds[i - updateLEDs];
    }

    for(int i = originIndex - updateLEDs; i < originIndex + updateLEDs; i++) {
      leds[i] = CRGB(redVal * actualBrightness, greenVal * actualBrightness, blueVal * actualBrightness);
    }
  }
}

void WaveformMode(float falloffRate) {
  leds[0] = CRGB(redVal * actualBrightness, greenVal * actualBrightness, blueVal * actualBrightness);

  float falloff = 1;

  for (int i = 1; i < NUM_LEDS; i++) {
    falloff -= falloffRate;
    if (falloff < 0) falloff = 0;
    
    CRGB col = CRGB(redVal * actualBrightness * falloff, greenVal * actualBrightness * falloff, blueVal * actualBrightness * falloff);
    leds[i] = col;
  }
}
