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

const connectorSelect = document.querySelector("#connectors");
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

const searchPublicationQuery = document.querySelector("#searchPublicationQuery");
const selectPublication = document.querySelector("#taskSelectOutput");
searchPublicationQuery.addEventListener("keypress", (event) => {
   if(event.key === "Enter"){
       GetPublication(connectorSelect.value, searchPublicationQuery.value)
           //.then(json => console.log(json));
           .then(json => 
               json.forEach(publication => {
                   var option = CreatePublication(publication);
                   option.addEventListener("click", () => {
                       CreateNewMangaDownloadTask(
                           taskTypesSelect.select,
                           connectorSelect.value,
                           publication.internalId
                           );
                   });
                   selectPublication.appendChild(option);
               }
           ));
   } 
});

function CreatePublication(publcationData){
    var option = document.createElement('publication');
    var img = document.createElement('img');
    img.src = publication.posterUrl;
    option.appendChild(img);
    var info = document.createElement('publication-information');
    var connectorName = document.createElement('connector-name');
    connectorName.innerText = connectorSelect.value;
    connectorName.className = "pill";
    info.appendChild(connectorName);
    var publicationName = document.createElement('publication-name');
    publicationName.innerText = publication.sortName;
    info.appendChild(publicationName);
    option.appendChild(info);
    return option;
}

const selectRecurrence = document.querySelector("#selectReccurrence");
function CreateNewMangaDownloadTask(taskType, connectorName, publicationId){
    CreateTask(taskType, selectRecurrence.value, connectorName, publicationId, "en");
    selectPublication.innerHTML = "";
}

const settingsTab = document.querySelector("#settingstab");
const settingsCog = document.querySelector("#settingscog");
var slideIn = true;
function slide(){
    if(slideIn)
        settingsTab.animate(slideInRight, slideInRightTiming);
    else
        settingsTab.animate(slideInRight, slideOutRightTiming);
    slideIn = !slideIn;
}

settingsCog.addEventListener("click", () => slide());

const addTask = document.querySelector("addPublication");

setInterval(() => {
    GetTasks().then(json => {
        //TODO
    });
}, 1000);