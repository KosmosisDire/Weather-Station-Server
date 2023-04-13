
var listener = new BackendListener(45223, 3, 24000, "./frontend/data", "data");
listener.Start();

Console.WriteLine("Press any key to stop the server...");
Console.ReadKey();

listener.Stop();
