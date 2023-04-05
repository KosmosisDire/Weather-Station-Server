using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.SQLite;

public enum DataType : byte
{
    Image = 0,
    Temperature = 1,
    Humidity = 2,
    Pressure = 3,
    Name = 4,
    Other = 5
}

public class BackendListener
{
    public int port;
    public int maxConnections;
    public int bufferSize;
    public string relativePath;
    const int headerSize = 9;

    private TcpListener? listener;
    public WeatherDatabase database = new();

    public BackendListener(int port, int maxConnections, int bufferSize, string relativePath)
    {
        this.port = port;
        this.maxConnections = maxConnections;
        this.bufferSize = bufferSize;
        this.relativePath = relativePath;
    }

    public void Start()
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        listener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), listener);
    }

    private void AcceptTcpClientCallback(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)(ar.AsyncState ?? throw new ArgumentNullException(nameof(ar)));
        TcpClient client = listener.EndAcceptTcpClient(ar);
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[bufferSize];

        stream.BeginRead(buffer, 0, bufferSize, new AsyncCallback(ReadCallback), new object[] { client, stream, buffer, ""});
        listener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), listener);
    }

    private void ReadCallback(IAsyncResult ar)
    {
        object[] objects = (object[])(ar.AsyncState ?? throw new ArgumentNullException(nameof(ar)));
        TcpClient client = (TcpClient)objects[0];
        NetworkStream stream = (NetworkStream)objects[1];
        byte[] readBuffer = (byte[])objects[2];
        string clientName = (string)objects[3];

        int bytesRead = stream.EndRead(ar);

        if (bytesRead > 0)
        {
            // read packet size and type
            if(bytesRead < headerSize) bytesRead += stream.Read(readBuffer, bytesRead, headerSize - bytesRead);
            int packetSize = 0;
            try
            {
                packetSize = Convert.ToInt32(BitConverter.ToUInt64(readBuffer, 0));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(string.Join(", ", readBuffer.Take(8).Select(x => x.ToString())));
                Console.Error.WriteLine($"Error: could not decode packet size. Closing connection. {e.Message}");
                stream.Dispose();
                client.Close();
                return;
            }

            DataType dataType = (DataType)readBuffer[8];
            
            int bytesLeft = packetSize;
            bytesLeft -= Math.Min(bytesRead - headerSize, bytesLeft);

            Console.WriteLine($"\n\nReceived packet size: {packetSize}, bytes left: {bytesLeft}, bytes read: {bytesRead}");
            
            // start building packet
            byte[] packet = new byte[packetSize];
            Array.Copy(readBuffer, headerSize, packet, 0, Math.Min(packetSize, bytesRead - headerSize));

            // shift leftover bytes to beginning of buffer
            int bytesLeftover = bytesRead - Math.Min(packetSize + headerSize, bytesRead);
            if (bytesLeftover > 0)
            {
                Console.WriteLine($"Leftover bytes: {bytesLeftover}");
                Array.Copy(readBuffer, Math.Max(bytesRead - bytesLeftover, 0), readBuffer, 0, bytesLeftover);
            }

            // read packet
            while (bytesLeft > 0)
            {
                bytesRead = stream.Read(readBuffer, bytesLeftover, Math.Min(readBuffer.Length, bytesLeft));
                Array.Copy(readBuffer, 0, packet, Math.Max(packetSize - bytesLeft, 0), Math.Min(bytesRead, bytesLeft));

                if (bytesLeft < bytesRead)
                {
                    Console.Error.WriteLine($"Error: read too many bytes on client {clientName}. Closing connection.");
                    stream.Dispose();
                    client.Close();
                    return;
                }

                bytesLeft -= Math.Min(bytesRead, bytesLeft);
            }

            bool handleSuccess = HandlePacket(packet, dataType, ref clientName, client);

            if (!handleSuccess) return;

            // Start reading again
            stream.BeginRead(readBuffer, (int)bytesLeftover, (int)(readBuffer.Length - bytesLeftover), new AsyncCallback(ReadCallback), new object[] { client, stream, readBuffer, clientName });
        }
        else
        {
            // Client disconnected
            Console.WriteLine("Client disconnected: " + clientName);
            stream.Dispose();
            client.Close();
        }

    }

    bool HandlePacket(byte[] packet, DataType dataType, ref string clientName, TcpClient client)
    {
        Console.WriteLine($"Received packet of type {dataType} from {clientName}");

        switch (dataType)
        {
            case DataType.Image:
                string filePath = Path.Join(relativePath, clientName, Utils.Timestamp.ToString() + ".jpg");
                System.IO.File.WriteAllBytes(filePath, packet);
                break;
            case DataType.Temperature:
                database.InsertTemperatureData(BitConverter.ToSingle(packet, 0), clientName);
                break;
            case DataType.Pressure:
                database.InsertPressureData(BitConverter.ToSingle(packet, 0), clientName);
                break;
            case DataType.Name:
                clientName = Encoding.UTF8.GetString(packet, 0, packet.Length);
                Console.WriteLine("Updated client name to: " + clientName);
                break;
            case DataType.Other:
                Console.WriteLine($"Received other data from {clientName}:");
                Console.WriteLine(Encoding.UTF8.GetString(packet, 0, packet.Length));
                break;
            default:
                Console.Error.Write($"Error: unknown data type {dataType}. Closing connection.");
                client.GetStream().Dispose();
                client.Close();
                return false;
        }

        return true;
    }

    public void Stop()
    {
        if (listener == null) return;
        listener.Stop();
    }
}
