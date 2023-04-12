// import Chart from 'chart.js'

const initSqlJs = window.initSqlJs;
const SQL = await initSqlJs({
    // Required to load the wasm binary asynchronously. Of course, you can host it wherever you want
    // You can omit locateFile completely when running in node
    locateFile: file => `https://nathang.dev/cse590/frontend/${file}`
});

const databaseBuffer = await fetch('https://nathang.dev/cse590/data/database.db').then(response => response.arrayBuffer());
const database = new SQL.Database(new Uint8Array(databaseBuffer));

const query = 'SELECT * FROM sensor WHERE clientName = "Nathan"';
let results = database.exec(query);
let rows = results[0].values;

let timestamps = rows.map(row => new Date(row[0] * 1000).toTimeString());
let temperatures = rows.map(row => row[2]);
let pressures = rows.map(row => row[3]);

for (let i = 1; i < rows.length; i++) 
{
    if (temperatures[i] == 12345678)
    {
        temperatures[i] = temperatures[i - 1];
    }

    if (pressures[i] == 12345678)
    {
        pressures[i] = pressures[i - 1];
    }
}

new Chart(
    document.getElementById('temperature-chart'),
    {
        type: 'bar',
        data: {
            labels: timestamps,
            datasets: [
                {
                    label: 'Pressure over time',
                    data: pressures,
                },
                {
                    label: 'Temperature over time',
                    data: temperatures,
                }
            ]
        }
    }
);



// async function waitUntil(conditionFunction, interval = 200, debugFunction = undefined) 
// {
//     const poll = resolve => 
//     {
//         if(debugFunction) debugFunction();
//         if(conditionFunction()) resolve();
//         else setTimeout(_ => poll(resolve), interval);
//     }

//     return new Promise(poll);
// }


// // Open a WebSocket connection
// var ws = new WebSocket('wss://nathang.dev:45224');

// ws.onerror = function(event) 
// {
//     console.error('WebSocket error:', event);
// };

// console.log('Connecting to server...');

// // Wait until the WebSocket is open
// await waitUntil(() => ws.readyState === ws.OPEN, 200, () => console.log("ws State is: " + ws.readyState));

// console.log('Connected to server!');

// // Send a message containing the request for the object array
// ws.send('get-object-array');

// console.log('Sent request for object array!');

// // Listen for incoming messages from the server
// ws.onmessage = function(event) 
// {
//     // Parse the incoming message as JSON
//     var message = JSON.parse(event.data);

//     console.log('Received message from server!');

//     // Check if the message is a response to the object array request
//     if (message.type === 'object-array') {
//         // Do something with the object array
//         var objectArray = message.data;
//         console.log(objectArray);
//     }
// };