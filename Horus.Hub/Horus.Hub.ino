/*
 Name:		Horus.Hub.ino
 Created:	5/22/2021 9:06:59 PM
 Author:	Jamie
*/

#include <SPI.h>
#include <Ethernet.h>

byte mac[] = {0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED};
IPAddress ip(192, 168, 0, 120);

const byte ESCAPE_BYTE = 43;
const byte FRAME_END_BYTE = 42;

// telnet defaults to port 23
EthernetServer _server(9001);

EthernetClient _client;

byte _buffer[128];
int _bufferPos;
bool _escaped;

void setup()
{
    Ethernet.begin(mac, ip);

    // Open serial communications and wait for port to open:
    Serial.begin(115200);
    while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
    }

    for (byte i = 0; i < 128; i++) 
    {
        _buffer[i] = 0;
    }
    _bufferPos = 0;
    
    // start listening for clients
    _server.begin();

	Serial.println("Server started");
}

void processFrame(int length)
{
	Serial.print("Frame length: ");
	Serial.print(length);
	Serial.print(" Cmd: ");
	Serial.println(_buffer[0]);

	_client.write(1);
}

void loop()
{
  // check for any new client connecting, and say hello (before any incoming data)
  EthernetClient client = _server.accept();

  if (client)
  {
    if(_client)
    {
      _client.stop();
    }
  	
  	_client = client;

  	Serial.println("Client connected");
  }
	
  if (_client) 
  {
  	if (_client.available() > 0)
    {
      int data = _client.read();

      if (data == ESCAPE_BYTE)
      {
      	if (_escaped)
      	{
            // An escaped escape
      		 _buffer[_bufferPos++] = data;
      		_escaped = false;
        }
        else
        {
            _escaped = true;
        }
      } else if (data == FRAME_END_BYTE) {
      	
        if (_escaped)
      	{
            // An escaped frame end
			 _buffer[_bufferPos++] = data;
      		_escaped = false;
        }
        else
        {
            // Frame end
			processFrame(_bufferPos);
        	_bufferPos = 0;
        }
      } else {
		  _buffer[_bufferPos++] = data;
      }
    }

    if (!_client.connected())
    {
        Serial.println("Client disconnected");
    	_client.stop();
    }
  }
}
