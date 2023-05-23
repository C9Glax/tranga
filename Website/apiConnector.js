const apiUri = "http://localhost:5177";

async function GetData(uri){
    let request = await fetch(uri, {
        method: 'GET',
        headers: {
            'Accept': 'application/json'
        }
    });
    let json = await request.json();
    return json;
}

function PostData(uri){
    fetch(uri, {
        method: 'POST'
    });
}

function DeleteData(uri){
    fetch(uri, {
        method: 'DELETE'
    });
}

async function GetAvailableControllers(){
    var uri = apiUri + "/Tranga/GetAvailableControllers";
    let json = await GetData(uri);
    return json;
}

async function GetPublication(connectorName, title){
    var uri = apiUri + `/Tranga/GetPublicationsFromConnector?connectorName=${connectorName}&title=${title}`;
    let json = await GetData(uri);
    return json;
}

async function GetKnownPublications(){
    var uri = apiUri + "/Tranga/GetKnownPublications";
    let json = await GetData(uri);
    return json;
}

async function GetTaskTypes(){
    var uri = apiUri + "/Tasks/GetTaskTypes";
    let json = await GetData(uri);
    return json;
}
async function GetRunningTasks(){
    var uri = apiUri + "/Tranga/GetRunningTasks";
    let json = await GetData(uri);
    return json;
}

async function GetTasks(){
    var uri = apiUri + "/Tasks/GetList";
    let json = await GetData(uri);
    return json;
}

async function GetSettings(){
    var uri = apiUri + "/Settings/Get";
    let json = await GetData(uri);
    return json;
}

function CreateTask(taskType, reoccurrence, connectorName, publicationId, language){
    var uri = apiUri + `/Tasks/Create?taskType=${taskType}&connectorName=${connectorName}&publicationId=${publicationId}&reoccurenceTime=${reoccurrence}&language=${language}`;
    PostData(uri);
}

function StartTask(taskType, connectorName, publicationId){
    var uri = apiUri + `/Tasks/Start?taskType=${taskType}&connectorName=${connectorName}&publicationId=${publicationId}`;
    PostData(uri);
}

function EnqueueTask(taskType, connectorName, publicationId){
    var uri = apiUri + `/Queue/Enqueue?taskType=${taskType}&connectorName=${connectorName}&publicationId=${publicationId}`;
    PostData(uri);
}

function UpdateSettings(downloadLocation, komgaUrl, komgaAuth){
    var uri = apiUri + `/Settings/Update?downloadLocation=${downloadLocation}&komgaUrl=${komgaAuth}&komgaAuth=${komgaAuth}`;
    PostData(uri);
}

function DeleteTask(taskType, connectorName, publicationId){
    var uri = apiUri + `/Tasks/Delete?taskType=${taskType}&connectorName=${connectorName}&publicationId=${publicationId}`;
    DeleteData(uri);
}

function DequeueTask(taskType, connectorName, publicationId){
    var uri = apiUri + `/Queue/Dequeue?taskType=${taskType}&connectorName=${connectorName}&publicationId=${publicationId}`;
    DeleteData(uri);
}