const apiUri = "http://localhost:5177";

function GetAvailableControllers(){
    var uri = apiUri + "/GetAvailableControllers";
    const response = await fetch(uri, {
        method: 'GET',
        headers: {
            'Accept': 'application/json'
        }
    }).catch(error => console.error('Unable to get items.', error));
    return await response.json();
}


function GetTasks(){
    var uri = apiUri + "/Tasks/GetList";
    const response = await fetch(uri, {
        method: 'GET',
        headers: {
            'Accept': 'application/json'
        }
    }).catch(error => console.error('Unable to get items.', error));
    return await response.json();
}

async function GetSettings(){
    var uri = apiUri + "/Settings/Get";
    const response = await fetch(uri, {
        method: 'GET',
        headers: {
            'Accept': 'application/json'
        }
    }).catch(error => console.error('Unable to get items.', error));
    return await response.json();
}