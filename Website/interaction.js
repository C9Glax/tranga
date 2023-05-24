const slideInRight = [
    { right: "-20rem" },
    { right: "0" }
];

const slideInRightTiming = {
    duration: 200,
    iterations: 1,
    fill: "forwards",
    easing: "ease-out"
}

const slideOutRightTiming = {
    direction: "reverse",
    duration: 200,
    iterations: 1,
    fill: "forwards",
    easing: "ease-in"
}

let publications = [];
let tasks = [];
let toEditId;

const searchPublicationQuery = document.querySelector("#searchPublicationQuery");
const selectPublication = document.querySelector("#taskSelectOutput");
const connectorSelect = document.querySelector("#connectors");
const settingsTab = document.querySelector("#settingstab");
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
const settingKomgaUrl = document.querySelector("#komgaURL");
const settingKomgaUser = document.querySelector("#komgaUsername");
const settingKomgaPass = document.querySelector("#komgaPassword");
const settingKomgaTime = document.querySelector("#komgaUpdateTime");


settingsCog.addEventListener("click", () => slide());
closetaskpopup.addEventListener("click", () => HideAddTaskPopup());
document.querySelector("#blurBackgroundTaskPopup").addEventListener("click", () => HideAddTaskPopup());
document.querySelector("#blurBackgroundPublicationPopup").addEventListener("click", () => HidePublicationPopup());
publicationDelete.addEventListener("click", () => DeleteTaskClick());
publicationAdd.addEventListener("click", () => AddTaskClick());

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

searchPublicationQuery.addEventListener("keypress", (event) => {
   if(event.key === "Enter"){
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
});

//Returns a new "Publication" Item to display in the tasks section
function CreatePublication(publication, connector){
    var publicationElement = document.createElement('publication');
    publicationElement.setAttribute("id", publication.internalId);
    var img = document.createElement('img');
    img.src = publication.posterUrl;
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

let slideIn = true;
function slide() {
    if (slideIn)
        settingsTab.animate(slideInRight, slideInRightTiming);
    else
        settingsTab.animate(slideInRight, slideOutRightTiming);
    slideIn = !slideIn;
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
    //Set position to mouse-position
    publicationViewerWindow.style.top = `${event.clientY - 60}px`;
    publicationViewerWindow.style.left = `${event.clientX}px`;
    
    //Edit information inside the window
    var publication = publications.filter(pub => pub.internalId === publicationId)[0];
    publicationViewerName.innerText = publication.sortName;
    publicationViewerDescription.innerText = publication.description;
    publicationViewerAuthor.innerText = publication.author;
    pubviewcover.src = publication.posterUrl;
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
    
    //Show popup
    publicationViewerPopup.style.display = "block";
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

function GetSettingsClick(){
    
}

function UpdateSettingsClick(){
    var auth = atob(`${settingKomgaUser.value}:${settingKomgaPass.value}`)
    UpdateSettings(settingDownloadLocation.value, settingKomgaUrl.value, auth);
    CreateTask("UpdateKomgaLibrary", settingKomgaTime.value, "","","");
}

//Resets the tasks shown
ResetContent();
//Get Tasks and show them
GetTasks()
    .then(json => json.forEach(task => {
        var publication = CreatePublication(task.publication, task.connectorName);
        publication.addEventListener("click", (event) => ShowPublicationViewerWindow(task.publication.internalId, event, false));
        tasksContent.appendChild(publication);
        tasks.push(task);
    }));

setInterval(() => {
    //Tasks from API
    var cTasks = [];
    GetTasks()
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
    
    
}, 1000);