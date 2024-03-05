var Slider     = document.getElementById("Slider");
var Highlight  = document.getElementById("Highlight");
var MenuScroll = document.getElementById("MenuBar");
var Headers    = document.getElementById("PageInfo");
var Contents   = document.getElementById("Contents"); 
var Toggle     = document.getElementById("HideContents");  
var Chapters   = Headers.children;

var title = document.location.href.split('/');
title = title[title.length - 2];
document.title = title.replace('.github.io','').replaceAll('%20',' ');

document.getElementById('logohover').src = '/include/' + getComputedStyle(document.querySelector(':root')).getPropertyValue('--theme').slice(1).replaceAll(',','%2C') + '.png';

function getGitURL() {
    document.location = 'https://github.com/xandprojects/xandprojects.github.io/tree/main/' + document.location.href.split('.io/')[1].replace('index','').replace('recent','').replace('.html','');
}
function getBackURL() {
    document.location = document.location.href.split('/').slice(0, -2).join('/');
}

function moveSlider(event) {
    var y = event.clientY;
    var preSlider = Slider.style.top;
    Slider.style.top    = Math.min(MenuScroll.scrollTop + Math.max(120 - MenuScroll.scrollTop, Math.min(y, topp+20)), MenuScroll.scrollTop + 940) + 'px';
    Highlight.style.top = 60 * Math.floor( parseInt(Slider.style.top) / 60 )  + 'px';
    if (y-60 < parseInt(Slider.style.top) && parseInt(Slider.style.top) < y+30) {
        Slider.style.visibility    = "visible";
        Highlight.style.visibility = "visible";
    } 
    else hideSlider();
}

function hideSlider() {
    Slider.style.visibility    = "hidden";
    Highlight.style.visibility = "hidden";
}

function toggleContents() {
    if (Toggle.textContent == ">") {
        Toggle.textContent  = "<";
        Toggle.style.left   = "98%";
        Headers.style.width = "85%";
        Contents.style.left = "100%";
    } else {
        Toggle.textContent = ">";
        Toggle.style.left   = "83%";
        Headers.style.width = "70%";
        Contents.style.left = "85%";
    }
}

function togglePDF(index) {
    if (Headers.children[index].children[3].textContent == "Open below") {
        Headers.children[index].children['pdf'].style.transition = "0s";
        Headers.children[index].children[3].textContent          = "Close below";
        Headers.children[index].children['pdf'].style.height     = window.innerHeight - 30 + 'px';
        Headers.scrollTop                                        = Headers.children[index].children['pdf'].offsetTop - 20;
    } else {
        Headers.children[index].children['pdf'].style.transition = "0.75s";
        Headers.children[index].children['pdf'].style.height     = '0px';
        Headers.children[index].children[3].textContent          = "Open below";
    }
}

for (i=0; i < Chapters.length; i++) {
    var div = document.createElement('div');
    div.innerHTML = Chapters[i].querySelectorAll("h1")[0].textContent;
    div.className = "Chapter";
    div.onclick   = function (){Headers.scrollTop = Chapters[Array.prototype.indexOf.call(Contents.children, this)-2].offsetTop - 30};
    Contents.appendChild(div);
}

var Options = MenuScroll.children;
var topp = 70;
for (i=3; i < Options.length; i++) {
    topp += 60;
    Options[i].children[0].style.top = topp + "px";
}

const sq_size = 160;
const canvas = document.getElementById('canv');
const ctx = canvas.getContext('2d');
const w = canvas.width = window.innerWidth;
const h = canvas.height = window.innerHeight;
const theme = getComputedStyle(document.querySelector(':root')).getPropertyValue('--theme');
const colours = [theme.slice(0,4) + 'a' + theme.slice(4, -1) + ', 0.1)', theme.slice(0,4) + 'a' + theme.slice(4, -1) + ', 0.5)'];
var indexA = 0;
var indexB = 0;
var speed  = 3;
ctx.fillStyle = '#000';


function slidingGrids () {
    
    ctx.fillStyle = '#000';
    ctx.fillRect(0, 0, w, h);
    
    for (i=0; i < 2; i++) {
        ctx.beginPath();

        ctx.strokeStyle = colours[i];
        const init = [sq_size - indexA, indexB][i];

        for (x=init; x < w; x+=sq_size) {
            ctx.moveTo(x, 0);
            ctx.lineTo(x, h);
            ctx.stroke();
        } for (y=init; y < h; y+=sq_size) {
            ctx.moveTo(0, y);
            ctx.lineTo(w, y);
            ctx.stroke();
        }

    }
    indexA = (indexA + speed*0.75) % sq_size;
    indexB = (indexB + speed) % sq_size;

}

setInterval(slidingGrids, 1000/20);