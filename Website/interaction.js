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

const taskTypesSelect = document.querySelector("#taskTypes")
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

settingsCog.addEventListener("click", () => slide());
closetaskpopup.addEventListener("click", () => HideAddTaskPopup());
document.querySelector("#blurBackgroundTaskPopup").addEventListener("click", () => HideAddTaskPopup());
document.querySelector("#blurBackgroundPublicationPopup").addEventListener("click", () => HidePublicationPopup());
publicationDelete.addEventListener("click", () => DeleteTaskClick());
publicationAdd.addEventListener("click", () => AddTaskClick());

/*
let availableTaskTypes;
GetTaskTypes()
    .then(json => availableTaskTypes = json)
    .then(json => 
        json.forEach(taskType => {
            var option = document.createElement('option');
            option.value = taskType;
            option.innerText = taskType;
            taskTypesSelect.appendChild(option);
        }));*/

let availableConnectors;
GetAvailableControllers()
    .then(json => availableConnectors = json)
    //.then(json => console.log(json))
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
       selectRecurrence.disabled = true;
       connectorSelect.disabled = true;
       searchPublicationQuery.disabled = true;
       
       selectPublication.replaceChildren();
       GetPublication(connectorSelect.value, searchPublicationQuery.value)
           //.then(json => console.log(json));
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
               selectRecurrence.disabled = false;
               connectorSelect.disabled = false;
               searchPublicationQuery.disabled = false;
           });
   } 
});

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
    HideAddTaskPopup();
}

function AddTaskClick(){
    CreateTask("DownloadNewChapters", selectRecurrence.value, connectorSelect.value, toEditId, "en")
    HideAddTaskPopup();
}

var slideIn = true;
function slide() {
    if (slideIn)
        settingsTab.animate(slideInRight, slideInRightTiming);
    else
        settingsTab.animate(slideInRight, slideOutRightTiming);
    slideIn = !slideIn;
}

function ResetContent(){
    tasksContent.replaceChildren();
    var add = document.createElement("div");
    add.setAttribute("id", "addPublication")
    var plus = document.createElement("p");
    plus.innerText = "+";
    add.appendChild(plus);
    add.addEventListener("click", () => ShowNewTaskWindow());
    tasksContent.appendChild(add);
}
function ShowPublicationViewerWindow(publicationId, event, add){
    publicationViewerWindow.style.top = `${event.clientY - 60}px`;
    publicationViewerWindow.style.left = `${event.clientX}px`;
    var publication = publications.filter(pub => pub.internalId === publicationId)[0];
    
    publicationViewerName.innerText = publication.sortName;
    publicationViewerDescription.innerText = publication.description;
    publicationViewerAuthor.innerText = publication.author;
    pubviewcover.src = publication.posterUrl;
    toEditId = publicationId;
    
    if(add){
        publicationAdd.style.display = "block";
        publicationDelete.style.display = "none";
    }
    else{
        publicationAdd.style.display = "none";
        publicationDelete.style.display = "block";
    }
    
    toEditId = publicationId;
    publicationViewerPopup.style.display = "block";
}

function ShowNewTaskWindow(){
    selectPublication.replaceChildren();
    addTaskPopup.style.display = "block";
}
function HideAddTaskPopup(){
    addTaskPopup.style.display = "none";
}

function HidePublicationPopup(){
    publicationViewerPopup.style.display = "none";
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

ResetContent();
GetTasks()
    //.then(json => console.log(json))
    .then(json => json.forEach(task => {
        var publication = CreatePublication(task.publication, task.connectorName);
        publication.addEventListener("click", (event) => ShowPublicationViewerWindow(task.publication.internalId, event));
        tasksContent.appendChild(publication);
        tasks.push(task);
    }));

setInterval(() => {
    var cTasks = [];
    GetTasks()
        //.then(json => console.log(json))
        .then(json => json.forEach(task => cTasks.push(task)))
        .then(() => {
            if(tasks.length != cTasks.length) {
                ResetContent();
                cTasks.forEach(task => {
                    var publication = CreatePublication(task.publication, task.connectorName);
                    publication.addEventListener("click", (event) => ShowPublicationViewerWindow(task.publication.internalId, event, true));
                    tasksContent.appendChild(publication);
                })

                tasks = cTasks;
            }
        }
    );
    
    
}, 1000);