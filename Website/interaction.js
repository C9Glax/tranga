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