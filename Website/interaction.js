let publications = [];
let tasks = [];
let toEditId;

const searchPublicationQuery = document.querySelector("#searchPublicationQuery");
const selectPublication = document.querySelector("#taskSelectOutput");
const connectorSelect = document.querySelector("#connectors");
const settingsPopup = document.querySelector("#settingsPopup");
const settingsCog = document.querySelector("#settingscog");
const selectRecurrence = document.querySelector("#selectReccurrence");
const tasksContent = document.querySelector("content");
const addTaskPopup = document.querySelector("#addTaskPopup");
const publicationViewerPopup = document.querySelector("#publicationViewerPopup");
const publicationViewerWindow = document.querySelector("publication-viewer");
const publicationViewerDescription = document.querySelector("#publicationViewerDescription");
const publicationViewerName = document.querySelector("#publicationViewerName");
const publicationViewerAuthor = document.querySelector("#publicationViewerAuthor");
const pubviewcover = document.querySelector("#pubviewcover");
const publicationDelete = document.querySelector("publication-delete");
const publicationAdd = document.querySelector("publication-add");
const closetaskpopup = document.querySelector("#closePopupImg");
const settingDownloadLocation = document.querySelector("#downloadLocation");
const settingKomgaUrl = document.querySelector("#komgaUrl");
const settingKomgaUser = document.querySelector("#komgaUsername");
const settingKomgaPass = document.querySelector("#komgaPassword");
const settingKomgaTime = document.querySelector("#komgaUpdateTime");
const settingKomgaConfigured = document.querySelector("#komgaConfigured");
const settingApiUri = document.querySelector("#settingApiUri");
const tagTasksRunning = document.querySelector("#tasksRunningTag");
const tagTasksQueued = document.querySelector("#tasksQueuedTag");
const tagTasksTotal = document.querySelector("#totalTasksTag");
const tagTaskPopup = document.querySelector("footer-tag-popup");
const tagTasksPopupContent = document.querySelector("footer-tag-content");

settingsCog.addEventListener("click", () => OpenSettings());
document.querySelector("#blurBackgroundSettingsPopup").addEventListener("click", () => HideSettings());
closetaskpopup.addEventListener("click", () => HideAddTaskPopup());
document.querySelector("#blurBackgroundTaskPopup").addEventListener("click", () => HideAddTaskPopup());
document.querySelector("#blurBackgroundPublicationPopup").addEventListener("click", () => HidePublicationPopup());
publicationDelete.addEventListener("click", () => DeleteTaskClick());
publicationAdd.addEventListener("click", () => AddTaskClick());
settingApiUri.addEventListener("keypress", (event) => {
    if(event.key === "Enter"){
        apiUri = settingApiUri.value;
        setTimeout(() => GetSettingsClick(), 100);
        document.cookie = `apiUri=${apiUri};`;
    }
});
searchPublicationQuery.addEventListener("keypress", (event) => {
    if(event.key === "Enter"){
        NewSearch();
    }
});
tagTasksRunning.addEventListener("mouseover", (event) => ShowRunningTasks(event));
tagTasksRunning.addEventListener("mouseout", () => CloseTasksPopup());
tagTasksQueued.addEventListener("mouseover", (event) => ShowQueuedTasks(event));
tagTasksQueued.addEventListener("mouseout", () => CloseTasksPopup());
tagTasksTotal.addEventListener("mouseover", (event) => ShowAllTasks(event));
tagTasksTotal.addEventListener("mouseout", () => CloseTasksPopup());

let availableConnectors;
GetAvailableControllers()
    .then(json => availableConnectors = json)
    .then(json => 
        json.forEach(connector => {
            var option = document.createElement('option');
            option.value = connector;
            option.innerText = connector;
            connectorSelect.appendChild(option);
        })
    );


function NewSearch(){
    //Disable inputs
    selectRecurrence.disabled = true;
    connectorSelect.disabled = true;
    searchPublicationQuery.disabled = true;

    //Empty previous results
    selectPublication.replaceChildren();
    GetPublication(connectorSelect.value, searchPublicationQuery.value)
        .then(json =>
            json.forEach(publication => {
                    var option = CreatePublication(publication, connectorSelect.value);
                    option.addEventListener("click", (mouseEvent) => {
                        ShowPublicationViewerWindow(publication.internalId, mouseEvent, true);
                    });
                    selectPublication.appendChild(option);
                }
            ))
        .then(() => {
            //Re-enable inputs
            selectRecurrence.disabled = false;
            connectorSelect.disabled = false;
            searchPublicationQuery.disabled = false;
        });
}

//Returns a new "Publication" Item to display in the tasks section
function CreatePublication(publication, connector){
    var publicationElement = document.createElement('publication');
    publicationElement.setAttribute("id", publication.internalId);
    var img = document.createElement('img');
    img.src = `imageCache/${publication.coverFileNameInCache}`;
    publicationElement.appendChild(img);
    var info = document.createElement('publication-information');
    var connectorName = document.createElement('connector-name');
    connectorName.innerText = connector;
    connectorName.className = "pill";
    info.appendChild(connectorName);
    var publicationName = document.createElement('publication-name');
    publicationName.innerText = publication.sortName;
    info.appendChild(publicationName);
    publicationElement.appendChild(info);
    if(publications.filter(pub => pub.internalId === publication.internalId) < 1)
        publications.push(publication);
    return publicationElement;
}

function DeleteTaskClick(){
    taskToDelete = tasks.filter(tTask => tTask.publication.internalId === toEditId)[0];
    DeleteTask("DownloadNewChapters", taskToDelete.connectorName, toEditId);
    HidePublicationPopup();
}

function AddTaskClick(){
    CreateTask("DownloadNewChapters", selectRecurrence.value, connectorSelect.value, toEditId, "en")
    HideAddTaskPopup();
    HidePublicationPopup();
}

function ResetContent(){
    //Delete everything
    tasksContent.replaceChildren();
    
    //Add "Add new Task" Button
    var add = document.createElement("div");
    add.setAttribute("id", "addPublication")
    var plus = document.createElement("p");
    plus.innerText = "+";
    add.appendChild(plus);
    add.addEventListener("click", () => ShowNewTaskWindow());
    tasksContent.appendChild(add);
}
function ShowPublicationViewerWindow(publicationId, event, add){


    //Show popup
    publicationViewerPopup.style.display = "block";
    
    //Set position to mouse-position
    if(event.clientY < window.innerHeight - publicationViewerWindow.offsetHeight)
        publicationViewerWindow.style.top = `${event.clientY}px`;
    else
        publicationViewerWindow.style.top = `${event.clientY - publicationViewerWindow.offsetHeight}px`;
    
    if(event.clientX < window.innerWidth - publicationViewerWindow.offsetWidth)
        publicationViewerWindow.style.left = `${event.clientX}px`;
    else
        publicationViewerWindow.style.left = `${event.clientX - publicationViewerWindow.offsetWidth}px`;
    
    //Edit information inside the window
    var publication = publications.filter(pub => pub.internalId === publicationId)[0];
    publicationViewerName.innerText = publication.sortName;
    publicationViewerDescription.innerText = publication.description;
    publicationViewerAuthor.innerText = publication.author;
    pubviewcover.src = `imageCache/${publication.coverFileNameInCache}`;
    toEditId = publicationId;
    
    //Check what action should be listed
    if(add){
        publicationAdd.style.display = "block";
        publicationDelete.style.display = "none";
    }
    else{
        publicationAdd.style.display = "none";
        publicationDelete.style.display = "block";
    }
}

function HidePublicationPopup(){
    publicationViewerPopup.style.display = "none";
}

function ShowNewTaskWindow(){
    selectPublication.replaceChildren();
    addTaskPopup.style.display = "block";
}
function HideAddTaskPopup(){
    addTaskPopup.style.display = "none";
}


const fadeIn = [
    { opacity: "0" },
    { opacity: "1" }
];

const fadeInTiming = {
    duration: 50,
    iterations: 1,
    fill: "forwards"
}

function OpenSettings(){
    GetSettingsClick();
    settingsPopup.style.display = "flex";
}

function HideSettings(){
    settingsPopup.style.display = "none";
}

function GetSettingsClick(){
    settingApiUri.value = "";
    settingKomgaUrl.value = "";
    settingKomgaUser.value = "";
    settingKomgaPass.value = "";
    
    settingApiUri.placeholder = apiUri;
    
    GetSettings().then(json => {
        settingDownloadLocation.innerText = json.downloadLocation;
        if(json.komga != null)
            settingKomgaUrl.placeholder = json.komga.baseUrl;
    });
    
    GetKomgaTask().then(json => {
        if(json.length > 0)
            settingKomgaConfigured.innerText = "✅";
        else
            settingKomgaConfigured.innerText = "❌";
    });
}

function UpdateKomgaSettings(){
    var auth = utf8_to_b64(`${settingKomgaUser.value}:${settingKomgaPass.value}`);
    console.log(auth);
    UpdateSettings("", settingKomgaUrl.value, auth);
    CreateTask("UpdateKomgaLibrary", settingKomgaTime.value, "","","");
    setTimeout(() => GetSettingsClick(), 500);
}

function utf8_to_b64( str ) {
    return window.btoa(unescape(encodeURIComponent( str )));
}


function ShowRunningTasks(event){
    GetRunningTasks()
        .then(json => {
            tagTasksPopupContent.replaceChildren();
            json.forEach(task => {
                var taskname = document.createElement("footer-tag-task-name");
                taskname.innerText = task.publication.sortName;
                tagTasksPopupContent.appendChild(taskname);
            });
            if(json.length > 0){
                tagTaskPopup.style.display = "block";
                tagTaskPopup.style.left = `${tagTasksRunning.offsetLeft - 20}px`;
            }
                
        });
}

function ShowQueuedTasks(event){
    GetQueue()
        .then(json => {
            tagTasksPopupContent.replaceChildren();
            json.forEach(task => {
                var taskname = document.createElement("footer-tag-task-name");
                taskname.innerText = task.publication.sortName;
                tagTasksPopupContent.appendChild(taskname);
            });
            if(json.length > 0){
                tagTaskPopup.style.display = "block";
                tagTaskPopup.style.left = `${tagTasksQueued.offsetLeft- 20}px`;
            }
        });
}

function ShowAllTasks(event){
    GetDownloadTasks()
        .then(json => {
            tagTasksPopupContent.replaceChildren();
            json.forEach(task => {
                var taskname = document.createElement("footer-tag-task-name");
                taskname.innerText = task.publication.sortName;
                tagTasksPopupContent.appendChild(taskname);
            });
            if(json.length > 0){
                tagTaskPopup.style.display = "block";
                tagTaskPopup.style.left = `${tagTasksTotal.offsetLeft - 20}px`;
            }
        });
}

function CloseTasksPopup(){
    tagTaskPopup.style.display = "none";
}

//Resets the tasks shown
ResetContent();
//Get Tasks and show them
GetDownloadTasks()
    .then(json => json.forEach(task => {
        var publication = CreatePublication(task.publication, task.connectorName);
        publication.addEventListener("click", (event) => ShowPublicationViewerWindow(task.publication.internalId, event, false));
        tasksContent.appendChild(publication);
        tasks.push(task);
    }));

GetRunningTasks()
    .then(json => {
        tagTasksRunning.innerText = json.length;
    });

GetDownloadTasks()
    .then(json => {
        tagTasksTotal.innerText = json.length;
    });

GetQueue()
    .then(json => {
        tagTasksQueued.innerText = json.length;
    })

setInterval(() => {
    //Tasks from API
    var cTasks = [];
    GetDownloadTasks()
        .then(json => json.forEach(task => cTasks.push(task)))
        .then(() => {
            //Only update view if tasks-amount has changed
            if(tasks.length != cTasks.length) {
                //Resets the tasks shown
                ResetContent();
                //Add all currenttasks to view
                cTasks.forEach(task => {
                    var publication = CreatePublication(task.publication, task.connectorName);
                    publication.addEventListener("click", (event) => ShowPublicationViewerWindow(task.publication.internalId, event, false));
                    tasksContent.appendChild(publication);
                })

                tasks = cTasks;
            }
        }
    );

    GetRunningTasks()
        .then(json => {
           tagTasksRunning.innerText = json.length;
        });

    GetDownloadTasks()
        .then(json => {
            tagTasksTotal.innerText = json.length;
        });
    
    GetQueue()
        .then(json => {
            tagTasksQueued.innerText = json.length;
        })
    
}, 1000);