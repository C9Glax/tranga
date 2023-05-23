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

const taskTypesSelect = document.querySelector("#taskTypes")
const searchPublicationQuery = document.querySelector("#searchPublicationQuery");
const selectPublication = document.querySelector("#taskSelectOutput");
const connectorSelect = document.querySelector("#connectors");
const settingsTab = document.querySelector("#settingstab");
const settingsCog = document.querySelector("#settingscog");
const selectRecurrence = document.querySelector("#selectReccurrence");
const tasksContent = document.querySelector("content");
const addtaskpopup = document.querySelector("addtask-popup");
const closetaskpopup = document.querySelector("#closePopupImg");

settingsCog.addEventListener("click", () => slide());
closetaskpopup.addEventListener("click", () => HideNewTaskWindow())
document.querySelector("addtask-background").addEventListener("click", () => HideNewTaskWindow());

let availableTaskTypes;
GetTaskTypes()
    .then(json => availableTaskTypes = json)
    .then(json => 
        json.forEach(taskType => {
            var option = document.createElement('option');
            option.value = taskType;
            option.innerText = taskType;
            taskTypesSelect.appendChild(option);
        }));

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
                       CreateNewMangaDownloadTask(
                           taskTypesSelect.value,
                           connectorSelect.value,
                           publication.internalId
                           );
                   });
                   selectPublication.appendChild(option);
               }
           ));
   } 
});

function CreatePublication(publication, connector){
    var option = document.createElement('publication');
    option.setAttribute("id", publication.internalId);
    var img = document.createElement('img');
    img.src = publication.posterUrl;
    option.appendChild(img);
    var info = document.createElement('publication-information');
    var connectorName = document.createElement('connector-name');
    connectorName.innerText = connector;
    connectorName.className = "pill";
    info.appendChild(connectorName);
    var publicationName = document.createElement('publication-name');
    publicationName.innerText = publication.sortName;
    info.appendChild(publicationName);
    option.appendChild(info);
    return option;
}

function CreateNewMangaDownloadTask(taskType, connectorName, publicationId){
    CreateTask(taskType, selectRecurrence.value, connectorName, publicationId, "en");
    selectPublication.innerHTML = "";
}

var slideIn = true;
function slide(){
    if(slideIn)
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

function ShowNewTaskWindow(){
    selectPublication.replaceChildren();
    addtaskpopup.style.display = "block";
    addtaskpopup.animate(fadeIn, fadeInTiming);
}
function HideNewTaskWindow(){
    addtaskpopup.style.display = "none";
}

const fadeIn = [
    { opacity: "0" },
    { opacity: "1" }
];

const fadeInTiming = {
    duration: 150,
    iterations: 1,
    fill: "forwards"
}

ResetContent();
GetTasks()
    //.then(json => console.log(json))
    .then(json => json.forEach(task => {
        var publication = CreatePublication(task.publication, task.connectorName);
        tasksContent.appendChild(publication);
    }));

setInterval(() => {
    ResetContent();
    GetTasks()
        //.then(json => console.log(json))
        .then(json => json.forEach(task => {
            var publication = CreatePublication(task.publication, task.connectorName);
            tasksContent.appendChild(publication);
        }));
}, 5000);