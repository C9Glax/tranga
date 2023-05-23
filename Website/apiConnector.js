const apiUri = "http://localhost:5177";

function GetTasks(){
    var getTaskUri = apiUri + "/Tasks/GetList";
    fetch(getTaskUri)
        .then(response => response.json())
        .catch(error => console.error('Unable to get items.', error));
}