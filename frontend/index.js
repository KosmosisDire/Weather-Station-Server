async function waitUntil(conditionFunction, interval = 200, debugFunction = undefined) 
{
    const poll = resolve => 
    {
        if(debugFunction) debugFunction();
        if(conditionFunction()) resolve();
        else setTimeout(_ => poll(resolve), interval);
    }

    return new Promise(poll);
}


// Open a WebSocket connection
var ws = new WebSocket('wss://nathang.dev:45224');

console.log('Connecting to server...');

// Wait until the WebSocket is open
await waitUntil(() => ws.readyState === ws.OPEN, 200, () => console.log("ws State is: " + ws.readyState));

console.log('Connected to server!');

// Send a message containing the request for the object array
ws.send('get-object-array');

console.log('Sent request for object array!');

// Listen for incoming messages from the server
ws.onmessage = function(event) 
{
    // Parse the incoming message as JSON
    var message = JSON.parse(event.data);

    console.log('Received message from server!');

    // Check if the message is a response to the object array request
    if (message.type === 'object-array') {
        // Do something with the object array
        var objectArray = message.data;
        console.log(objectArray);
    }
};