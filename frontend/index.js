const initSqlJs = window.initSqlJs;
const SQL = await initSqlJs({locateFile: file => `https://nathang.dev/cse590/${file}`});

function timestampToTime(timestamp)
{
    let date = new Date(timestamp * 1000);
    let hours = date.getHours();
    let minutes = "0" + date.getMinutes();

    return hours + ':' + minutes.substring(minutes.length-2)
}

let tempChart = null;
let pressureChart = null;

let database = null;


class CancelationToken
{
    constructor()
    {
        this.cancelled = false;
    }

    cancel()
    {
        this.cancelled = true;
    }

    reset()
    {
        this.cancelled = false;
    }

    async waitForCompletion()
    {
        return new Promise((resolve, reject) => {
            let interval = setInterval(() => {
                if (!this.cancelled)
                {
                    clearInterval(interval);
                    resolve();
                }
            }, 100);
        });
    }
}

async function loadBarOverTime(progressBar, time, cancelationToken)
{
    progressBar.value = 0;

    let interval = setInterval(() => {
        if (cancelationToken.cancelled)
        {
            clearInterval(interval);
            return;
        }

        progressBar.value += 1;
    }, time / 1000);

    return new Promise((resolve, reject) => {
        setTimeout(() => {
            clearInterval(interval);
            resolve();
        }, time);
    });
}

let bmpCancelationToken = new CancelationToken();
let imageCancelationToken = new CancelationToken();

let graphedIntervals = 8640;

document.getElementById('graphed-hours-input').addEventListener('change', async (event) => {
    graphedIntervals = event.target.value * 60 * 60 / 10;
    document.getElementById('bmp-progress').value = 0;
    bmpCancelationToken.cancel();
    await bmpCancelationToken.waitForCompletion();
    await loadData();
});

async function loadData()
{
    const databaseBuffer = await fetch('https://nathang.dev/cse590/data/database.db').then(response => response.arrayBuffer());
    database = new SQL.Database(new Uint8Array(databaseBuffer));

    // get unique client names and timestamps
    let query = `SELECT timestamp, clientName FROM sensor ORDER BY timestamp DESC LIMIT ${graphedIntervals}`;
    let results = database.exec(query);
    let rows = results[0].values;
    let uniqueClientNames = [...new Set(rows.map(row => row[1]))];

    // find the client with the most timestamps and use their timestamps
    let maxTimestamps = 0;
    let maxTimestampsClient = null;

    for (let i = 0; i < uniqueClientNames.length; i++)
    {
        // finding max timestamp
        if (rows.length > maxTimestamps)
        {
            maxTimestamps = rows.length;
            maxTimestampsClient = uniqueClientNames[i];
        }
    }

    // get timestamps for the client with the most timestamps
    query = `SELECT timestamp FROM sensor WHERE clientName = '${maxTimestampsClient}' ORDER BY timestamp DESC LIMIT ${graphedIntervals}`;
    results = database.exec(query);
    rows = results[0].values;
    let timestampsRaw = rows.map(row => row[0] * 1000);
    timestampsRaw = timestampsRaw.reverse();
    let timestamps = timestampsRaw.map(timestamp => timestampToTime(timestamp));

    function getClosest(target, inside)
    {
        let closest = inside.reduce(function(prev, curr) {
            return (Math.abs(curr - target) < Math.abs(prev - target) ? curr : prev);
        });

        return closest;
    }

    // select data for each client
    let pressureData = [];
    let temperatureData = [];

    for (let i = 0; i < uniqueClientNames.length; i++)
    {
        query = `SELECT * FROM sensor WHERE clientName = '${uniqueClientNames[i]}' ORDER BY timestamp DESC LIMIT ${graphedIntervals}`;
        results = database.exec(query);
        rows = results[0].values;
        rows = rows.reverse();

        
        let temperaturesIncomplete = rows.map(row => ({x:getClosest(row[0]*1000, timestampsRaw), y:row[2]}));
        let pressuresIncomplete = rows.map(row => ({x:getClosest(row[0]*1000, timestampsRaw), y:row[3]/100}));

        let temperatures = timestampsRaw.map(timestamp => ({x:timestamp, y:null}));
        let pressures = timestampsRaw.map(timestamp => ({x:timestamp, y:null}));

        // insert values into the correct place
        for(let i = 0; i < timestamps.length; i++)
        {
            for(let j = 0; j < temperaturesIncomplete.length; j++)
            {
                if (temperaturesIncomplete[j].x == timestampsRaw[i])
                {
                    temperatures[i].y = temperaturesIncomplete[j].y;
                }

                if (pressuresIncomplete[j].x == timestampsRaw[i])
                {
                    pressures[i].y = pressuresIncomplete[j].y;
                }
            }
        }


        for (let i = 1; i < temperatures.length; i++) 
        {
            if (temperatures[i].y == 0)
            {
                temperatures[i].y = null;
            }

            if (pressures[i].y == 0)
            {
                pressures[i].y = null;
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

    Chart.defaults.interaction.mode = 'nearest';
    Chart.defaults.interaction.axis = 'x';
    Chart.defaults.interaction.intersect = false;
    Chart.defaults.datasets.line.pointRadius = 0;
    Chart.defaults.datasets.line.tension = 0.2;
    Chart.defaults.maintainAspectRatio = false;
    Chart.defaults.normalized = true;
    Chart.defaults.color = hexToRgb(getCssVariableValue('--text-color'));
    Chart.defaults.scale.grid.color = hexToRgb(getCssVariableValue('--text-color-muted'));

    if (tempChart == null)
    {
        tempChart = new Chart(
            document.getElementById('temperature-chart'),
            {
                type: 'line',
                data: {
                    labels: timestampsRaw,
                    datasets: temperatureData
                },
                options: {
                    plugins: {
                        title: {
                            display: true,
                            text: 'Temperature over 24 hours'
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) 
                                {
                                    return context.dataset.label + ': ' + (+context.parsed.y.toFixed(2)) + '°C';
                                }
                            }
                        },
                    },
                    scales: {
                        y: {
                            ticks: {
                                // Include a dollar sign in the ticks
                                callback: function(value, index, ticks) {
                                    return value + '°C';
                                }
                            }
                        },
                        x: {
                            type: 'time',
                            time: {
                                unit: 'hour',
                                displayFormats: {
                                    minute: 'hh:mm',
                                    hour: 'hh:mm'
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
        tempChart.data.datasets = temperatureData;
        tempChart.data.labels = timestampsRaw;
        tempChart.options.transitions = false;

        let isHiddenList = [];

        for (let s in tempChart._sortedMetasets)
        {
            isHiddenList.push(tempChart._sortedMetasets[s].hidden);
        }
        
        await tempChart.update();

        for (let s in tempChart._sortedMetasets)
        {
            tempChart._sortedMetasets[s].hidden = isHiddenList[s];
        }
    }

    console.log(tempChart);

    if (pressureChart == null)
    {
        pressureChart = new Chart(
            document.getElementById('pressure-chart'),
            {
                type: 'line',
                data: {
                    labels: timestampsRaw,
                    datasets: pressureData
                },
                options: {
                    plugins: {
                        title: {
                            display: true,
                            text: 'Pressure over 24 hours'
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) 
                                {
                                    return context.dataset.label + ': ' + (+context.parsed.y.toFixed(2)) + ' hPa';
                                }
                            }
                        },
                    },
                    scales: {
                        y: {
                            ticks: {
                                // Include a dollar sign in the ticks
                                callback: function(value, index, ticks) {
                                    return value + ' hPa';
                                }
                            }
                        },
                        x: {
                            type: 'time',
                            time: {
                                unit: 'hour',
                                displayFormats: {
                                    minute: 'hh:mm',
                                    hour: 'hh:mm'
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
        pressureChart.data.labels = timestampsRaw;
        pressureChart.options.transitions = false;

        let isHiddenList = [];

        for (let s in pressureChart._sortedMetasets)
        {
            isHiddenList.push(pressureChart._sortedMetasets[s].hidden);
        }
        
        await pressureChart.update();

        for (let s in pressureChart._sortedMetasets)
        {
            pressureChart._sortedMetasets[s].hidden = isHiddenList[s];
        }
    }

    loadBarOverTime(document.getElementById('bmp-progress'), 10000, bmpCancelationToken).then(()=>
    {
        if(!bmpCancelationToken.cancelled)
        {
            loadData();
        }
        else
        {
            bmpCancelationToken.reset();
        }
    });
}

setTimeout(()=>
{
    pressureChart.options.transitions = false;
    tempChart.options.transitions = false;
}, 500)


await loadData();
await loadImages();

async function loadImages()
{
    let query = `SELECT i.*
    FROM images i
    INNER JOIN (
      SELECT clientName, MAX(timestamp) AS max_timestamp
      FROM images
      GROUP BY clientName
    ) t ON i.clientName = t.clientName AND i.timestamp = t.max_timestamp
    `;
    let result = database.exec(query);
    let images = result[0].values;

    let imageRow = document.getElementById('image-row');
    if (!imageRow)
    {
        imageRow = document.body.appendChild(document.createElement('div'));
        imageRow.classList.add('row');
        imageRow.id = 'image-row';
    }


    // get accent colors for each client
    let clientColors = {};
    for (let dataset in tempChart.config._config.data.datasets)
    {
        let clientName = tempChart.config._config.data.datasets[dataset].label;
        let color = tempChart.config._config.data.datasets[dataset].borderColor;
        clientColors[clientName] = color.replace('rgb(', '').replace(')', ''); 
    }

    for (let i = 0; i < images.length; i++)
    {
        let imageContainer = document.getElementById("webcam-image-container-" + i);
        if (!imageContainer)
        {
            imageContainer = imageRow.appendChild(document.createElement('div'));
            imageContainer.classList.add('webcam-image-container', 'tooltip');
            imageContainer.id = "webcam-image-container-" + i;
        }

        imageContainer.setAttribute('style', '--client-color: ' + clientColors[images[i][1]] + ';');

        let imageEl = document.getElementById("webcam-image-" + i);
        if (!imageEl)
        {
            imageEl = imageContainer.appendChild(document.createElement('img'));
            imageEl.classList.add('webcam-image', 'tooltip');
            imageEl.id = "webcam-image-" + i;
            
            let tooltip = document.getElementById("webcam-image-tooltip-" + i);
            if (!tooltip)
            {
                tooltip = imageContainer.appendChild(document.createElement('span'));
                tooltip.classList.add('tooltiptext');
            }

            tooltip.innerText = "Taken at: " + timestampToTime(images[i][0]);

            let clientName = document.getElementById("webcam-image-client-name-" + i);
            if (!clientName)
            {
                clientName = imageContainer.appendChild(document.createElement('span'));
                clientName.classList.add('client-name');
            }

            clientName.innerText = images[i][1];
        }

        imageEl.src = "https://nathang.dev/cse590/" + images[i][2];
    }

    loadBarOverTime(document.getElementById('image-progress'), 30000, imageCancelationToken).then(()=>
    {
        if(!imageCancelationToken.cancelled)
        {
            loadImages();
        }
        else
        {
            imageCancelationToken.reset();
        }
    });
}


// Theme toggle:

// load saved theme state
if (localStorage.getItem("theme_toggle") != null)
{
    setThemeToggle(localStorage.getItem("theme_toggle") == "true");
}

var lastScheme = "dark-theme";
// change theme to match current system theme
if (localStorage.getItem("theme_toggle") == null && window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches)
{
    setThemeToggle(true);
    lastScheme = "light-theme";
}
if (localStorage.getItem("theme_toggle") == null && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches)
{
    setThemeToggle(true);
    lastScheme = "dark-theme";
}

// set initial toggle state based on body theme class
if ($("body").hasClass("light-theme"))
{
    setThemeToggle(true);
}
else
{
    setThemeToggle(false);
}

function setThemeToggle(state, instant = false)
{
    $(".toggle__input").each(function()
    {
        $(this).prop("checked", state);
    });

    if(!$(".toggle__input").hasClass("is-checked") && state)
    {
        $(".toggle__input").addClass("is-checked");
    }
    else if ($(".toggle__input").hasClass("is-checked") && !state)
    {
        $(".toggle__input").removeClass("is-checked");
    }

    if(!state)
    {
        if ($("body").hasClass("light-theme"))
        {
            $("body").removeClass("light-theme");
        }

        if (!$("body").hasClass("dark-theme"))
        {
            $("body").addClass("dark-theme");
        }
    }
    else
    {
        if ($("body").hasClass("dark-theme"))
        {
            $("body").removeClass("dark-theme");
        }

        if (!$("body").hasClass("light-theme"))
        {
            $("body").addClass("light-theme");
        }
    }

    localStorage.setItem("theme_toggle", state ? "true" : "false");

    console.log(tempChart);
    tempChart.update();
    pressureChart.update();
}

$(".toggle__input").on("click", function()
{
    setThemeToggle(!(localStorage.getItem("theme_toggle") == "true"));
});

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', event => 
{
    // return if we are printing
    if (window.matchMedia('print').matches)
    {
        printing = true;
        return;
    }

    let newColorScheme = event.matches ? "dark-theme" : "light-theme";

    if (newColorScheme == lastScheme) return;

    if (newColorScheme == "dark-theme")
    {
        setThemeToggle(false);
    }

    if (newColorScheme == "light-theme")
    {
        setThemeToggle(true);
    }

    lastScheme = newColorScheme;
});