async function waitUntil(conditionFunction, interval = 200) 
{
    const poll = resolve => {
        if(conditionFunction()) resolve();
        else setTimeout(_ => poll(resolve), interval);
    }

    return new Promise(poll);
}


// Open a WebSocket connection
var ws = new WebSocket('wss://nathang.dev:45224');

// Wait until the WebSocket is open
await waitUntil(() => ws.readyState === ws.OPEN);

// Send a message containing the request for the object array
ws.send('get-object-array');

// Listen for incoming messages from the server
ws.onmessage = function(event) 
{
    // Parse the incoming message as JSON
    var message = JSON.parse(event.data);

    // Check if the message is a response to the object array request
    if (message.type === 'object-array') {
        // Do something with the object array
        var objectArray = message.data;
        console.log(objectArray);
    }
};