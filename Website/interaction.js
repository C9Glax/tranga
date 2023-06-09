let publications = [];
let tasks = [];
let toEditId;

const searchBox = document.querySelector("#searchbox");
const searchPublicationQuery = document.querySelector("#searchPublicationQuery");
const selectPublication = document.querySelector("#taskSelectOutput");
const connectorSelect = document.querySelector("#connectors");
const settingsPopup = document.querySelector("#settingsPopup");
const settingsCog = document.querySelector("#settingscog");
const selectRecurrence = document.querySelector("#selectReccurrence");
const tasksContent = document.querySelector("content");
const selectPublicationPopup = document.querySelector("#selectPublicationPopup");
const createMonitorTaskButton = document.querySelector("#createMonitorTaskButton");
const createDownloadChapterTaskButton = document.querySelector("#createDownloadChapterTaskButton");
const createMonitorTaskPopup = document.querySelector("#createMonitorTaskPopup");
const createDownloadChaptersTask = document.querySelector("#createDownloadChaptersTask");
const chapterOutput = document.querySelector("#chapterOutput");
const selectedChapters = document.querySelector("#selectedChapters");
const publicationViewerPopup = document.querySelector("#publicationViewerPopup");
const publicationViewerWindow = document.querySelector("publication-viewer");
const publicationViewerDescription = document.querySelector("#publicationViewerDescription");
const publicationViewerName = document.querySelector("#publicationViewerName");
const publicationViewerTags = document.querySelector("#publicationViewerTags");
const publicationViewerAuthor = document.querySelector("#publicationViewerAuthor");
const pubviewcover = document.querySelector("#pubviewcover");
const publicationDelete = document.querySelector("publication-delete");
const publicationTaskStart = document.querySelector("publication-starttask");
const settingDownloadLocation = document.querySelector("#downloadLocation");
const settingKomgaUrl = document.querySelector("#komgaUrl");
const settingKomgaUser = document.querySelector("#komgaUsername");
const settingKomgaPass = document.querySelector("#komgaPassword");
const settingKavitaUrl = document.querySelector("#kavitaUrl");
const settingKavitaUser = document.querySelector("#kavitaUsername");
const settingKavitaPass = document.querySelector("#kavitaPassword");
const libraryUpdateTime = document.querySelector("#libraryUpdateTime");
const settingKomgaConfigured = document.querySelector("#komgaConfigured");
const settingKavitaConfigured = document.querySelector("#kavitaConfigured");
const settingApiUri = document.querySelector("#settingApiUri");
const tagTasksRunning = document.querySelector("#tasksRunningTag");
const tagTasksQueued = document.querySelector("#tasksQueuedTag");
const downloadTasksPopup = document.querySelector("#downloadTasksPopup");
const downloadTasksOutput = downloadTasksPopup.querySelector("popup-content");

searchbox.addEventListener("keyup", (event) => FilterResults());
settingsCog.addEventListener("click", () => OpenSettings());
document.querySelector("#blurBackgroundSettingsPopup").addEventListener("click", () => settingsPopup.style.display = "none");
document.querySelector("#blurBackgroundTaskPopup").addEventListener("click", () => selectPublicationPopup.style.display = "none");
document.querySelector("#blurBackgroundPublicationPopup").addEventListener("click", () => HidePublicationPopup());
document.querySelector("#blurBackgroundCreateMonitorTaskPopup").addEventListener("click", () => createMonitorTaskPopup.style.display = "none");
document.querySelector("#blurBackgroundCreateDownloadChaptersTask").addEventListener("click", () => createDownloadChaptersTask.style.display = "none");
document.querySelector("#blurBackgroundTasksQueuePopup").addEventListener("click", () => downloadTasksPopup.style.display = "none");
selectedChapters.addEventListener("keypress", (event) => {
    if(event.key === "Enter"){
        DownloadChapterTaskClick();
    }
})
publicationDelete.addEventListener("click", () => DeleteTaskClick());
createMonitorTaskButton.addEventListener("click", () => {
    HidePublicationPopup();
    createMonitorTaskPopup.style.display = "block";
});
createDownloadChapterTaskButton.addEventListener("click", () => {
    HidePublicationPopup();
    OpenDownloadChapterTaskPopup();
});
publicationTaskStart.addEventListener("click", () => StartTaskClick());
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
    connectorSelect.disabled = true;
    searchPublicationQuery.disabled = true;
    //Waitcursor
    document.body.style.cursor = "wait";

    //Empty previous results
    selectPublication.replaceChildren();
    GetPublicationFromConnector(connectorSelect.value, searchPublicationQuery.value)
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
            connectorSelect.disabled = false;
            searchPublicationQuery.disabled = false;
            //Cursor
            document.body.style.cursor = "initial";
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

function AddMonitorTask(){
    var hours = document.querySelector("#hours").value;
    var minutes = document.querySelector("#minutes").value;
    CreateMonitorTask(connectorSelect.value, toEditId, `${hours}:${minutes}:00`, "en");
    HidePublicationPopup();
    createMonitorTaskPopup.style.display = "none";
    selectPublicationPopup.style.display = "none";
}

function OpenDownloadChapterTaskPopup(){
    selectedChapters.value = "";
    chapterOutput.replaceChildren();
    createDownloadChaptersTask.style.display = "block";
    GetChapters(toEditId, connectorSelect.value, "en").then((json) => {
        var i = 0;
        json.forEach(chapter => {
            var chapterDom = document.createElement("div");
            var indexDom = document.createElement("span");
            indexDom.className = "index";
            indexDom.innerText = i++;
            chapterDom.appendChild(indexDom);
            
            var volDom = document.createElement("span");
            volDom.className = "vol";
            volDom.innerText = chapter.volumeNumber;
            chapterDom.appendChild(volDom);
            
            var chDom = document.createElement("span");
            chDom.className = "ch";
            chDom.innerText = chapter.chapterNumber;
            chapterDom.appendChild(chDom);
            
            var titleDom = document.createElement("span");
            titleDom.innerText = chapter.name;
            chapterDom.appendChild(titleDom);
            chapterOutput.appendChild(chapterDom);
        });
    });
}

function DownloadChapterTaskClick(){
    CreateDownloadChaptersTask(connectorSelect.value, toEditId, selectedChapters.value, "en");
    HidePublicationPopup();
    createDownloadChaptersTask.style.display = "none";
    selectPublicationPopup.style.display = "none";
}

function DeleteTaskClick(){
    taskToDelete = tasks.filter(tTask => tTask.publication.internalId === toEditId)[0];
    DeleteTask("DownloadNewChapters", taskToDelete.connectorName, toEditId);
    HidePublicationPopup();
}

function StartTaskClick(){
    var toEditTask = tasks.filter(task => task.publication.internalId == toEditId)[0];
    StartTask("DownloadNewChapters", toEditTask.connectorName, toEditId);
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
    publicationViewerTags.innerText = publication.tags.join(", ");
    publicationViewerDescription.innerText = publication.description;
    publicationViewerAuthor.innerText = publication.author;
    pubviewcover.src = `imageCache/${publication.coverFileNameInCache}`;
    toEditId = publicationId;
    
    //Check what action should be listed
    if(add){
        createMonitorTaskButton.style.display = "initial";
        createDownloadChapterTaskButton.style.display = "initial";
        publicationDelete.style.display = "none";
        publicationTaskStart.style.display = "none";
    }
    else{
        createMonitorTaskButton.style.display = "none";
        createDownloadChapterTaskButton.style.display = "none";
        publicationDelete.style.display = "initial";
        publicationTaskStart.style.display = "initial";
    }
}

function HidePublicationPopup(){
    publicationViewerPopup.style.display = "none";
}

function ShowNewTaskWindow(){
    selectPublication.replaceChildren();
    searchPublicationQuery.value = "";
    selectPublicationPopup.style.display = "flex";
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

function GetSettingsClick(){
    settingApiUri.value = "";
    settingKomgaUrl.value = "";
    settingKomgaUser.value = "";
    settingKomgaPass.value = "";
    settingKavitaUrl.value = "";
    settingKavitaUser.value = "";
    settingKavitaPass.value = "";
    settingKomgaConfigured.innerText = "❌";
    settingKavitaConfigured.innerText = "❌";
    
    settingApiUri.placeholder = apiUri;
    
    GetSettings().then(json => {
        settingDownloadLocation.innerText = json.downloadLocation;
        json.libraryManagers.forEach(lm => {
           if(lm.libraryType == 0){
               settingKomgaUrl.placeholder = lm.baseUrl;
               settingKomgaUser.placeholder = "User";
               settingKomgaPass.placeholder = "***";
               settingKomgaConfigured.innerText = "✅";
           } else if(lm.libraryType == 1){
               settingKavitaUrl.placeholder = lm.baseUrl;
               settingKavitaUser.placeholder = "User";
               settingKavitaPass.placeholder = "***";
               settingKavitaConfigured.innerText = "✅";
           }
        });
    });
    
    GetKomgaTask().then(json => {
        if(json.length > 0)
            libraryUpdateTime.value = json[0].reoccurrence;
    });
}

function UpdateLibrarySettings(){
    if(settingKomgaUser.value != "" && settingKomgaPass != ""){
        var auth = utf8_to_b64(`${settingKomgaUser.value}:${settingKomgaPass.value}`);
        console.log(auth);

        if(settingKomgaUrl.value != "")
            UpdateSettings("", settingKomgaUrl.value, auth, "", "");
        else
            UpdateSettings("", settingKomgaUrl.placeholder, auth, "", "");
    }
    
    if(settingKavitaUrl.value != "" && settingKavitaUser.value != "" && settingKavitaPass.value != ""){
        UpdateSettings("", "", "", settingKavitaUrl.value, settingKavitaUser.value, settingKavitaPass.value);
    }
    CreateUpdateLibraryTask(libraryUpdateTime.value);
    setTimeout(() => GetSettingsClick(), 200);
}

function utf8_to_b64( str ) {
    return window.btoa(unescape(encodeURIComponent( str )));
}

function FilterResults(){
    if(searchBox.value.length > 0){
        tasksContent.childNodes.forEach(publication => {
            publication.childNodes.forEach(item => {
                if(item.nodeName.toLowerCase() == "publication-information"){
                    item.childNodes.forEach(information => {
                        if(information.nodeName.toLowerCase() == "publication-name"){
                            if(!information.textContent.toLowerCase().includes(searchBox.value.toLowerCase())){
                                publication.style.display = "none";
                            }else{
                                publication.style.display = "initial";
                            }
                        }
                    });
                }
            });
        });
    }else{
        tasksContent.childNodes.forEach(publication => publication.style.display = "initial");
    }
}

function ShowTasksQueue(){

    downloadTasksOutput.replaceChildren();
    GetRunningTasks()
        .then(json => {
            tagTasksRunning.innerText = json.length;
            json.forEach(task => {
                downloadTasksOutput.appendChild(CreateProgressChild(task));

                if(task.chapter != undefined){
                    document.querySelector(`#progress${task.publication.internalId}-${task.chapter.chapterNumber}`).value = task.progress;
                    document.querySelector(`#progressStr${task.publication.internalId}-${task.chapter.chapterNumber}`).innerText = task.progress.toLocaleString(undefined,{style: 'percent', minimumFractionDigits:2});
                }else{
                    document.querySelector(`#progress${task.publication.internalId}`).value = task.progress;
                    document.querySelector(`#progressStr${task.publication.internalId}`).innerText = task.progress.toLocaleString(undefined,{style: 'percent', minimumFractionDigits:2});
                }
            });
        });

    GetQueue()
        .then(json => {
            tagTasksQueued.innerText = json.length;
            json.forEach(task => {
                downloadTasksOutput.appendChild(CreateProgressChild(task));
            });
        });
    downloadTasksPopup.style.display = "flex";
}

function CreateProgressChild(task){
    var child = document.createElement("div");
    var img = document.createElement('img');
    img.src = `imageCache/${task.publication.coverFileNameInCache}`;
    child.appendChild(img);
    
    var name = document.createElement("span");
    name.innerText = task.publication.sortName;
    name.className = "pubTitle";
    child.appendChild(name);


    var progress = document.createElement("progress");
    progress.value = 0;
    child.appendChild(progress);
    
    var progressStr = document.createElement("span");
    progressStr.innerText = "00.00%";
    progressStr.className = "progressStr";
    child.appendChild(progressStr);
    
    if(task.chapter != undefined){
        var chapterNumber = document.createElement("span");
        chapterNumber.className = "chapterNumber";
        chapterNumber.innerText = `Vol.${task.chapter.volumeNumber} Ch.${task.chapter.chapterNumber}`;
        child.appendChild(chapterNumber);
        
        var chapterName = document.createElement("span");
        chapterName.className = "chapterName";
        chapterName.innerText = task.chapter.name;
        child.appendChild(chapterName);

        progress.id = `progress${task.publication.internalId}-${task.chapter.chapterNumber}`;
        progressStr.id = `progressStr${task.publication.internalId}-${task.chapter.chapterNumber}`;
    }else{
        progress.id = `progress${task.publication.internalId}`;
        progressStr.id = `progressStr${task.publication.internalId}`;
    }
    
    
    return child;
}

//Resets the tasks shown
ResetContent();
downloadTasksOutput.replaceChildren();
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
        json.forEach(task => {
            downloadTasksOutput.appendChild(CreateProgressChild(task));
        });
    });

GetQueue()
    .then(json => {
        tagTasksQueued.innerText = json.length;
        json.forEach(task => {
            downloadTasksOutput.appendChild(CreateProgressChild(task));
        });
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
    
    GetQueue()
        .then(json => {
            tagTasksQueued.innerText = json.length;
        });
}, 1000);

setInterval(() => {
    GetRunningTasks().then((json) => {
        json.forEach(task => {
            if(task.chapter != undefined){
                document.querySelector(`#progress${task.publication.internalId}-${task.chapter.chapterNumber}`).value = task.progress;
                document.querySelector(`#progressStr${task.publication.internalId}-${task.chapter.chapterNumber}`).innerText = task.progress.toLocaleString(undefined,{style: 'percent', minimumFractionDigits:2});
            }else{
                document.querySelector(`#progress${task.publication.internalId}`).value = task.progress;
                document.querySelector(`#progressStr${task.publication.internalId}`).innerText = task.progress.toLocaleString(undefined,{style: 'percent', minimumFractionDigits:2});
            }
        });
    });
},500);