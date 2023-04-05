using System.Data.SQLite;


public struct SensorData
{
    public long timestamp;
    public string clientName;
    public float temperature;
    public float pressure;
}

public class WeatherDatabase
{
    SQLiteConnection connection;
    const long UNDEFINED = -1234567;

    public WeatherDatabase()
    {
        connection = Connect();
        CreateTables();
    }

    SQLiteConnection Connect()
    {
        var newConnection = new SQLiteConnection("Data Source=./data/database.db; Version = 3; New = True; Compress = True;");
        
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
            timestamp INTEGER PRIMARY KEY, 
            clientName VARCHAR(256), 
            temperature REAL, 
            pressure REAL
        )";

        string createCameraTableCommand = 
        @"CREATE TABLE IF NOT EXISTS images (
            timestamp INTEGER PRIMARY KEY, 
            clientName VARCHAR(256), 
            filePath VARCHAR(256)
        )";

        createTablesCommand = connection.CreateCommand();
        createTablesCommand.CommandText = createBMPTableCommand;
        createTablesCommand.ExecuteNonQuery();
        createTablesCommand.CommandText = createCameraTableCommand;
        createTablesCommand.ExecuteNonQuery();
    }

    public SensorData GetLatestSensorData(string clientName)
    {
        SQLiteCommand selectCommand;
        SensorData sensorData = new();

        selectCommand = new SQLiteCommand(
            @"SELECT * FROM sensor WHERE clientName = ? ORDER BY timestamp DESC LIMIT 1", connection);

        selectCommand.Parameters.Add(clientName);

        try
        {
            using SQLiteDataReader reader = selectCommand.ExecuteReader();
            if (reader.Read())
            {
                sensorData.timestamp = reader.GetInt64(0);
                sensorData.clientName = reader.GetString(1);
                sensorData.temperature = reader.GetFloat(2);
                sensorData.pressure = reader.GetFloat(3);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        return sensorData;
    }


    // when inserting either temp or pressure, if the previous entry is missing temp or pressure, fill it in rather than creating a new entry
    // unless the previous timestamp is more than 1 second ago
    public void InsertTemperatureData(float temperature, string clientName)
    {
        var latestSensorData = GetLatestSensorData(clientName);
        SQLiteCommand insertCommand;

        if (latestSensorData.temperature != UNDEFINED || latestSensorData.timestamp + 1 < Utils.Timestamp || latestSensorData.clientName != clientName)
        {
            insertCommand = new SQLiteCommand(
                @"INSERT INTO sensor (
                    timestamp, 
                    clientName, 
                    temperature,
                    pressure) VALUES (?,?,?,?)", connection);

            SQLiteParameter timestampParam = new SQLiteParameter();
            timestampParam.Value = Utils.Timestamp;
            insertCommand.Parameters.Add(timestampParam);

            SQLiteParameter clientNameParam = new SQLiteParameter();
            clientNameParam.Value = clientName;
            insertCommand.Parameters.Add(clientNameParam);

            SQLiteParameter temperatureParam = new SQLiteParameter();
            temperatureParam.Value = temperature;
            insertCommand.Parameters.Add(temperatureParam);

            SQLiteParameter pressureParam = new SQLiteParameter();
            pressureParam.Value = UNDEFINED;
            insertCommand.Parameters.Add(pressureParam);
        }
        else
        {
            insertCommand = new SQLiteCommand(
                @"UPDATE sensor SET temperature = ? WHERE timestamp = ?", connection);

            SQLiteParameter temperatureParam = new SQLiteParameter();
            temperatureParam.Value = temperature;
            insertCommand.Parameters.Add(temperatureParam);

            SQLiteParameter timestampParam = new SQLiteParameter();
            timestampParam.Value = latestSensorData.timestamp;
            insertCommand.Parameters.Add(timestampParam);
        }

        try
        {
            insertCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public void InsertPressureData(float pressure, string clientName)
    {
        var latestSensorData = GetLatestSensorData(clientName);
        SQLiteCommand insertCommand;

        if (latestSensorData.pressure != UNDEFINED || latestSensorData.timestamp + 1 < Utils.Timestamp || latestSensorData.clientName != clientName)
        {
            insertCommand = new SQLiteCommand(
                @"INSERT INTO sensor (
                    timestamp, 
                    clientName, 
                    temperature,
                    pressure) VALUES (?,?,?,?)", connection);

            SQLiteParameter timestampParam = new SQLiteParameter();
            timestampParam.Value = Utils.Timestamp;
            insertCommand.Parameters.Add(timestampParam);

            SQLiteParameter clientNameParam = new SQLiteParameter();
            clientNameParam.Value = clientName;
            insertCommand.Parameters.Add(clientNameParam);

            SQLiteParameter temperatureParam = new SQLiteParameter();
            temperatureParam.Value = UNDEFINED;
            insertCommand.Parameters.Add(temperatureParam);

            SQLiteParameter pressureParam = new SQLiteParameter();
            pressureParam.Value = pressure;
            insertCommand.Parameters.Add(pressureParam);
        }
        else
        {
            insertCommand = new SQLiteCommand(
                @"UPDATE sensor SET pressure = ? WHERE timestamp = ?", connection);

            SQLiteParameter pressureParam = new SQLiteParameter();
            pressureParam.Value = pressure;
            insertCommand.Parameters.Add(pressureParam);

            SQLiteParameter timestampParam = new SQLiteParameter();
            timestampParam.Value = latestSensorData.timestamp;
            insertCommand.Parameters.Add(timestampParam);
        }

        try
        {
            insertCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
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

        SQLiteParameter timestampParam = new SQLiteParameter();
        timestampParam.Value = Utils.Timestamp;
        insertCommand.Parameters.Add(timestampParam);

        SQLiteParameter clientNameParam = new SQLiteParameter();
        clientNameParam.Value = clientName;
        insertCommand.Parameters.Add(clientNameParam);

        SQLiteParameter filePathParam = new SQLiteParameter();
        filePathParam.Value = filePath;
        insertCommand.Parameters.Add(filePathParam);

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

