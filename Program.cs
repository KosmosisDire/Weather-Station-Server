
// var listener = new BackendListener(45223, 3, 5840, "/home/cse-user/backend/data/");
// listener.Start();

WebSocketServer.RunServer("https://192.168.1.2:45224/");

Console.WriteLine("Press any key to stop the server...");
Console.ReadKey();

// listener.Stop();
