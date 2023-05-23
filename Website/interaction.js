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
let toRemoveId;

const taskTypesSelect = document.querySelector("#taskTypes")
const searchPublicationQuery = document.querySelector("#searchPublicationQuery");
const selectPublication = document.querySelector("#taskSelectOutput");
const connectorSelect = document.querySelector("#connectors");
const settingsTab = document.querySelector("#settingstab");
const settingsCog = document.querySelector("#settingscog");
const selectRecurrence = document.querySelector("#selectReccurrence");
const tasksContent = document.querySelector("content");
const generalPopup = document.querySelector("popup");
const addTaskWindow = document.querySelector("addtask-window");
const publicationViewer = document.querySelector("publication-viewer");
const publicationViewerDescription = document.querySelector("#publicationViewerDescription");
const publicationViewerName = document.querySelector("#publicationViewerName");
const publicationViewerAuthor = document.querySelector("#publicationViewerAuthor");
const pubviewcover = document.querySelector("#pubviewcover");
const publicationDelete = document.querySelector("publication-delete");
const closetaskpopup = document.querySelector("#closePopupImg");

settingsCog.addEventListener("click", () => slide());
closetaskpopup.addEventListener("click", () => HidePopup());
document.querySelector("blur-background").addEventListener("click", () => HidePopup());
publicationDelete.addEventListener("click", () => DeleteTaskClick());

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
       selectPublication.replaceChildren();
       GetPublication(connectorSelect.value, searchPublicationQuery.value)
           //.then(json => console.log(json));
           .then(json => 
               json.forEach(publication => {
                   var option = CreatePublication(publication, connectorSelect.value);
                   option.addEventListener("click", () => {
                       CreateTask("DownloadNewChapters", selectRecurrence.value, connectorSelect.value, publication.internalId, "en");
                       HidePopup();
                       selectPublication.replaceChildren();
                   });
                   selectPublication.appendChild(option);
               }
           ));
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
    taskToDelete = tasks.filter(tTask => tTask.publication.internalId === toRemoveId)[0];
    DeleteTask("DownloadNewChapters", taskToDelete.connectorName, toRemoveId);
    HidePopup();
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

function ShowPopup(){
    generalPopup.style.display = "block";
    generalPopup.animate(fadeIn, fadeInTiming);
}

function ShowPublicationViewerWindow(publicationId, event){
    publicationViewer.style.top = `${event.clientY - 60}px`;
    publicationViewer.style.left = `${event.clientX}px`;
    var publication = publications.filter(pub => pub.internalId === publicationId)[0];
    
    publicationViewerName.innerText = publication.sortName;
    publicationViewerDescription.innerText = publication.description;
    publicationViewerAuthor.innerText = publication.author;
    pubviewcover.src = publication.posterUrl;
    toRemoveId = publicationId;
    
    toRemoveId = publicationId;
    publicationViewer.style.display = "block";
    ShowPopup();
}

function ShowNewTaskWindow(){
    selectPublication.replaceChildren();
    addTaskWindow.style.display = "flex";
    ShowPopup();
}
function HidePopup(){
    generalPopup.style.display = "none";
    addTaskWindow.style.display = "none";
    publicationViewer.style.display = "none";
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

        if(tasks.filter(tTask => tTask.publication.internalId === task.publication.internalId) < 1)
            tasks.push(task);
    }));

setInterval(() => {
    ResetContent();
    GetTasks()
        //.then(json => console.log(json))
        .then(json => json.forEach(task => {
            var publication = CreatePublication(task.publication, task.connectorName);
            publication.addEventListener("click", (event) => ShowPublicationViewerWindow(task.publication.internalId, event));
            tasksContent.appendChild(publication);

            if(tasks.filter(tTask => tTask.publication.internalId === task.publication.internalId) < 1)
                tasks.push(task);
        }));
}, 1000);