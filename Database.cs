using System.Data.SQLite;


public struct SensorData
{
    public bool isValid = false;
    public long timestamp;
    public string clientName;
    public float? temperature;
    public float? pressure;

    public SensorData(long timestamp, string clientName, float? temperature, float? pressure)
    {
        this.isValid = true;
        this.timestamp = timestamp;
        this.clientName = clientName;
        this.temperature = temperature;
        this.pressure = pressure;
    }
}

public class WeatherDatabase
{
    SQLiteConnection connection;

    public WeatherDatabase(string databasePath)
    {
        connection = Connect(databasePath);
        CreateTables();
    }

    SQLiteConnection Connect(string databasePath)
    {
        var newConnection = new SQLiteConnection($"Data Source={Path.Join(databasePath, "database.db")}; Version = 3; New = True; Compress = True;");
        
        try
        {
            newConnection.Open();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        return newConnection;
    }

    void CreateTables()
    {
        SQLiteCommand createTablesCommand;

        string createBMPTableCommand = 
        @"CREATE TABLE IF NOT EXISTS sensor (
            timestamp INTEGER, 
            clientName VARCHAR(256), 
            temperature REAL, 
            pressure REAL,
            PRIMARY KEY (timestamp, clientName)
        )";

        string createCameraTableCommand = 
        @"CREATE TABLE IF NOT EXISTS images (
            timestamp INTEGER, 
            clientName VARCHAR(256), 
            filePath VARCHAR(256),
            PRIMARY KEY (timestamp, clientName)
        )";

        createTablesCommand = connection.CreateCommand();
        createTablesCommand.CommandText = createBMPTableCommand;
        createTablesCommand.ExecuteNonQuery();
        createTablesCommand.CommandText = createCameraTableCommand;
        createTablesCommand.ExecuteNonQuery();
    }

    public SensorData GetClosestSensorData(string clientName, long timestamp, int maxDistance = int.MaxValue)
    {
        SQLiteCommand selectCommand;
        SensorData sensorData = new();

        selectCommand = new SQLiteCommand(
            @"SELECT * FROM sensor WHERE clientName = ? ORDER BY ABS(timestamp - ?) LIMIT ?", connection);

        selectCommand.Parameters.AddWithValue("clientName", clientName);
        selectCommand.Parameters.AddWithValue("timestamp", timestamp);
        selectCommand.Parameters.AddWithValue("limit", 1);

        try
        {
            using SQLiteDataReader reader = selectCommand.ExecuteReader();
            if (reader.Read())
            {
                sensorData.timestamp = reader.GetInt64(0);
                sensorData.clientName = reader.GetString(1);
                sensorData.temperature = reader.IsDBNull(2) ? null : reader.GetFloat(2);
                sensorData.pressure = reader.IsDBNull(3) ? null : reader.GetFloat(3);
                sensorData.isValid = true;
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        if (Math.Abs(sensorData.timestamp - timestamp) > maxDistance)
            sensorData.isValid = false;

        return sensorData;
    }


    // when inserting either temp or pressure, if the closest entry is missing temp or pressure, fill it in rather than creating a new entry
    public void InsertTemperatureData(float temperature, string clientName)
    {
        long timestamp = Utils.Timestamp;
        var closestSensorData = GetClosestSensorData(clientName, timestamp, 5);
        SQLiteCommand insertCommand;

        if (closestSensorData.isValid && closestSensorData.temperature == null)
        {
            insertCommand = new SQLiteCommand(
                @"UPDATE sensor SET temperature = ? WHERE timestamp = ? AND clientName = ?", connection);

            insertCommand.Parameters.AddWithValue("temperature", temperature);
            insertCommand.Parameters.AddWithValue("timestamp", closestSensorData.timestamp);
            insertCommand.Parameters.AddWithValue("clientName", clientName);
        }
        else
        {
            insertCommand = new SQLiteCommand(
                @"INSERT INTO sensor (
                    timestamp, 
                    clientName, 
                    temperature,
                    pressure) VALUES (?,?,?,?)", connection);

            insertCommand.Parameters.AddWithValue("timestamp", timestamp);
            insertCommand.Parameters.AddWithValue("clientName", clientName);
            insertCommand.Parameters.AddWithValue("temperature", temperature);
            insertCommand.Parameters.AddWithValue("pressure", DBNull.Value);
        }

        try
        {
            insertCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }

    public void InsertPressureData(float pressure, string clientName)
    {
        long timestamp = Utils.Timestamp;
        var latestSensorData = GetClosestSensorData(clientName, timestamp, 5);
        SQLiteCommand insertCommand;

        if (latestSensorData.isValid && latestSensorData.pressure == null)
        {
            insertCommand = new SQLiteCommand(
                @"UPDATE sensor SET pressure = ? WHERE timestamp = ? AND clientName = ?", connection);

            insertCommand.Parameters.AddWithValue("pressure", pressure);
            insertCommand.Parameters.AddWithValue("timestamp", latestSensorData.timestamp);
            insertCommand.Parameters.AddWithValue("clientName", clientName);
        }
        else
        {
            insertCommand = new SQLiteCommand(
                @"INSERT INTO sensor (
                    timestamp, 
                    clientName, 
                    temperature,
                    pressure) VALUES (?,?,?,?)", connection);

            insertCommand.Parameters.AddWithValue("timestamp", timestamp);
            insertCommand.Parameters.AddWithValue("clientName", clientName);
            insertCommand.Parameters.AddWithValue("temperature", DBNull.Value);
            insertCommand.Parameters.AddWithValue("pressure", pressure);
        }

        try
        {
            insertCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }


    public void InsertCameraData(string filePath, string clientName)
    {
        SQLiteCommand insertCommand;

        insertCommand = new SQLiteCommand(
            @"INSERT INTO images (
                timestamp, 
                clientName, 
                filePath) VALUES (?,?,?)", connection);

        insertCommand.Parameters.AddWithValue("timestamp", Utils.Timestamp);
        insertCommand.Parameters.AddWithValue("clientName", clientName);
        insertCommand.Parameters.AddWithValue("filePath", filePath);

        try
        {
            insertCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

}

