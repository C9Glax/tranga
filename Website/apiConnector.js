let apiUri = `http://${window.location.host.split(':')[0]}:6531`

if(getCookie("apiUri") != ""){
    apiUri = getCookie("apiUri");
}
function getCookie(cname) {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for(let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

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
    var uri = apiUri + "/Connectors";
    let json = await GetData(uri);
    return json;
}

async function GetPublicationFromConnector(connectorName, title){
    var uri = apiUri + `/Publications/FromConnector?connectorName=${connectorName}&title=${title}`;
    let json = await GetData(uri);
    return json;
}

async function GetKnownPublications(){
    var uri = apiUri + "/Publications/Known";
    let json = await GetData(uri);
    return json;
}

async function GetPublication(internalId){
    var uri = apiUri + `/Publications/Known?internalId=${internalId}`;
    let json = await GetData(uri);
    return json;
}

async function GetChapters(internalId, connectorName, onlyNew, language){
    var uri = apiUri + `/Publications/Chapters?internalId=${internalId}&connectorName=${connectorName}&onlyNew=${onlyNew}&language=${language}`;
    let json = await GetData(uri);
    return json;
}

async function GetTaskTypes(){
    var uri = apiUri + "/Tasks/Types";
    let json = await GetData(uri);
    return json;
}
async function GetRunningTasks(){
    var uri = apiUri + "/Tasks/RunningTasks";
    let json = await GetData(uri);
    return json;
}

async function GetDownloadTasks(){
    var uri = apiUri + "/Tasks?taskType=DownloadNewChapters";
    let json = await GetData(uri);
    return json;
}

async function GetSettings(){
    var uri = apiUri + "/Settings";
    let json = await GetData(uri);
    return json;
}

async function GetKomgaTask(){
    var uri = apiUri + "/Tasks?taskType=UpdateLibraries";
    let json = await GetData(uri);
    return json;
}

function CreateMonitorTask(connectorName, internalId, reoccurrence, language){
    var uri = apiUri + `/Tasks/CreateMonitorTask?connectorName=${connectorName}&internalId=${internalId}&reoccurrenceTime=${reoccurrence}&language=${language}`;
    PostData(uri);
}

function CreateUpdateLibraryTask(reoccurrence){
    var uri = apiUri + `/Tasks/CreateUpdateLibraryTask?reoccurrenceTime=${reoccurrence}`;
    PostData(uri);
}

function CreateDownloadChaptersTask(connectorName, internalId, chapters, language){
    var uri = apiUri + `/Tasks/CreateDownloadChaptersTask?connectorName=${connectorName}&internalId=${internalId}&chapters=${chapters}&language=${language}`;
    PostData(uri);
}

function StartTask(taskType, connectorName, internalId){
    var uri = apiUri + `/Tasks/Start?taskType=${taskType}&connectorName=${connectorName}&internalId=${internalId}`;
    PostData(uri);
}

function EnqueueTask(taskType, connectorName, publicationId){
    var uri = apiUri + `/Queue/Enqueue?taskType=${taskType}&connectorName=${connectorName}&publicationId=${publicationId}`;
    PostData(uri);
}

function UpdateDownloadLocation(downloadLocation){
    var uri = apiUri + "/Settings/Update?"
    uri += "&downloadLocation="+downloadLocation;
    PostData(uri);
}

function UpdateKomga(komgaUrl, komgaAuth){
    var uri = apiUri + "/Settings/Update?"
    uri += `&komgaUrl=${komgaUrl}&komgaAuth=${komgaAuth}`;
    PostData(uri);
}

function UpdateKavita(kavitaUrl, kavitaUser, kavitaPass){
    var uri = apiUri + "/Settings/Update?"
    uri += `&kavitaUrl=${kavitaUrl}&kavitaUsername=${kavitaUser}&kavitaPassword=${kavitaPass}`;
    PostData(uri);
}

function UpdateGotify(gotifyUrl, gotifyAppToken){
    var uri = apiUri + "/Settings/Update?"
    uri += `&gotifyUrl=${gotifyUrl}&gotifyAppToken=${gotifyAppToken}`;
    PostData(uri);
}

function UpdateLunaSea(lunaseaWebhook){
    var uri = apiUri + "/Settings/Update?"
    uri += `&lunaseaWebhook=${lunaseaWebhook}`;
    PostData(uri);
}

function DeleteTask(taskType, connectorName, publicationId){
    var uri = apiUri + `/Tasks?taskType=${taskType}&connectorName=${connectorName}&publicationId=${publicationId}`;
    DeleteData(uri);
}

function DequeueTask(taskType, connectorName, publicationId){
    var uri = apiUri + `/Queue/Dequeue?taskType=${taskType}&connectorName=${connectorName}&publicationId=${publicationId}`;
    DeleteData(uri);
}

async function GetQueue(){
    var uri = apiUri + "/Queue/List";
    let json = await GetData(uri);
    return json;
}