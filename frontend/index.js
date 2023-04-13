const initSqlJs = window.initSqlJs;
const SQL = await initSqlJs({locateFile: file => `https://nathang.dev/cse590/frontend/${file}`});

let tempChart = null;
let pressureChart = null;

async function loadData()
{
    const databaseBuffer = await fetch('https://nathang.dev/cse590/data/database.db').then(response => response.arrayBuffer());
    const database = new SQL.Database(new Uint8Array(databaseBuffer));

    // get unique client names and timestamps
    let query = 'SELECT timestamp, clientName FROM sensor LIMIT 8640';
    let results = database.exec(query);
    let rows = results[0].values;
    let uniqueClientNames = [...new Set(rows.map(row => row[1]))];
    let timestamps = rows.map(row => new Date(row[0] * 1000).toTimeString().split(' ')[0].split(':').slice(0, 2).join(':'));



    // select data for each client
    let pressureData = [];
    let temperatureData = [];

    for (let i = 0; i < uniqueClientNames.length; i++)
    {
        query = `SELECT * FROM sensor WHERE clientName = '${uniqueClientNames[i]}'`;
        results = database.exec(query);
        rows = results[0].values;

        
        let temperatures = rows.map(row => row[2]);
        let pressures = rows.map(row => row[3]/100);

        for (let i = 1; i < rows.length; i++) 
        {
            if (temperatures[i] == -1234567)
            {
                temperatures[i] = temperatures[i - 1];
            }

            if (pressures[i] == -1234567/100)
            {
                pressures[i] = pressures[i - 1];
            }
        }

        pressureData.push({
            label: uniqueClientNames[i],
            data: pressures
        });

        temperatureData.push({
            label: uniqueClientNames[i],
            data: temperatures
        });
    }


    function hexToRgb(hex) 
    {
        hex = hex.toLowerCase().trim();
        var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);

        if (!result) {
            console.error("Invalid hex color code: " + hex);
            return null;
        }

        return "rgb(" + parseInt(result[1], 16) + "," + parseInt(result[2], 16) + "," + parseInt(result[3], 16) + ")";
    }


    function getCssVariableValue(variableName) 
    {
        return getComputedStyle(document.body).getPropertyValue(variableName).trim();
    }

    console.log(Chart.defaults);
    Chart.defaults.interaction.mode = 'nearest';
    Chart.defaults.interaction.axis = 'x';
    Chart.defaults.interaction.intersect = false;
    Chart.defaults.datasets.line.pointRadius = 0;
    Chart.defaults.maintainAspectRatio = false;
    Chart.defaults.color = hexToRgb(getCssVariableValue('--text-color'));
    Chart.defaults.scale.grid.color = hexToRgb(getCssVariableValue('--text-color-muted'));

    if (tempChart == null)
    {
        tempChart = new Chart(
            document.getElementById('temperature-chart'),
            {
                type: 'line',
                data: {
                    labels: timestamps,
                    datasets: temperatureData
                },
                options: {
                    plugins: {
                        title: {
                            display: true,
                            text: 'Temperature over 24 hours'
                        }
                    },
                    scales: {
                        y: {
                            ticks: {
                                // Include a dollar sign in the ticks
                                callback: function(value, index, ticks) {
                                    return value + '°C';
                                }
                            }
                        }
                    }
                }
            }
        );

        console.log(tempChart);
    }
    else
    {
        tempChart.data.datasets = temperatureData;
        tempChart.data.labels = timestamps;
        tempChart.options.animation = false;
        tempChart.update();
    }

    if (pressureChart == null)
    {
        pressureChart = new Chart(
            document.getElementById('pressure-chart'),
            {
                type: 'line',
                data: {
                    labels: timestamps,
                    datasets: pressureData
                },
                options: {
                    plugins: {
                        title: {
                            display: true,
                            text: 'Pressure over 24 hours'
                        }
                    },
                    scales: {
                        y: {
                            ticks: {
                                // Include a dollar sign in the ticks
                                callback: function(value, index, ticks) {
                                    return value + ' hPa';
                                }
                            }
                        }
                    }
                }
            }
        );
    }
    else
    {
        pressureChart.data.datasets = pressureData;
        pressureChart.data.labels = timestamps;
        pressureChart.options.animation = false;
        pressureChart.update();
    }
}

await loadData();

setInterval(async () => 
{
    await loadData();
}, 10000);