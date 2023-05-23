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
    .then(json => console.log(json));
    /*.then(json => 
        json.forEach(connector => {
        var option = document.createElement('option');
        option.value = connector.name;
        option.innerText = connector.name;
        taskTypesSelect.appendChild(option);
    }));*/



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