using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using WatsonTcp;

namespace Horus.Shared
{

    public enum Commands : ushort
    {
        FocuserMoveSet,
        FocuserAbsoluteGet,
        FocuserMaxIncrementGet,
        FocuserMaxStepGet,
        FocuserHalt,
        FocuserPositionGet,
        FocuserStepSizeGet,
        FocuserTempCompGet,
        FocuserTempCompSet,
        FocuserTempCompAvailableGet,
        FocuserTemperatureGet,
    }

    public static class CommandsExtensions
    {
        public static byte[] Create(this Commands command)
        {
            return BitConverter.GetBytes((ushort) command);
        }

        public static byte[] Create<T>(this Commands command, T value)
        {
            var commandBytes = BitConverter.GetBytes((ushort)command);
            byte[] valueBytes;

            switch (value)
            {
                case bool boolValue:
                    valueBytes = BitConverter.GetBytes(boolValue);
                    break;
                case int intValue:
                    valueBytes = BitConverter.GetBytes(intValue);
                    break;
                case double doubleValue:
                    valueBytes = BitConverter.GetBytes(doubleValue);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported command payload type: " + typeof(T));
            }

            var bytes = new byte[commandBytes.Length + valueBytes.Length];

            using (var stream = new MemoryStream(bytes))
            {
                stream.Write(commandBytes, 0, commandBytes.Length);
                stream.Write(valueBytes, 0, valueBytes.Length);
            }

            return bytes;
        }
    }


    public static class WatsonTcpClientExtensions
    {
        public static int GetInt(this WatsonTcpClient client, Commands command)
        {
            var resp = client.SendAndWait(1000, command.Create());

            return BitConverter.ToInt32(resp.Data, sizeof(int));
        }

        public static double GetDouble(this WatsonTcpClient client, Commands command)
        {
            var resp = client.SendAndWait(1000, command.Create());

            return BitConverter.ToDouble(resp.Data, sizeof(double));
        }

        public static bool GetBool(this WatsonTcpClient client, Commands command)
        {
            var resp = client.SendAndWait(1000, command.Create());

            return BitConverter.ToBoolean(resp.Data, sizeof(bool));
        }

        public static void Set(this WatsonTcpClient client, Commands command)
        {
            client.SendAndWait(1000, command.Create());
        }

        public static void Set<T>(this WatsonTcpClient client, Commands command, T value)
        {
            client.SendAndWait(TimeSpan.FromMinutes(5).Milliseconds, command.Create(value));
        }
    }
}
