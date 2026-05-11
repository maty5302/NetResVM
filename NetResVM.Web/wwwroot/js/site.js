// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Check and apply the stored theme on page load
document.addEventListener("DOMContentLoaded", function () {
    var storedTheme = localStorage.getItem("theme");
    if (storedTheme) {
        document.documentElement.setAttribute("data-bs-theme", storedTheme);
        document.getElementById("flexSwitchCheckDefault").checked = storedTheme === "dark";
    }
});

// Add an event listener for the theme toggle switch
document.getElementById("btnSwitch").addEventListener("click", function () {
    var currentTheme = document.documentElement.getAttribute("data-bs-theme");
    var newTheme = currentTheme === "light" ? "dark" : "light";
    document.documentElement.setAttribute("data-bs-theme", newTheme);
    localStorage.setItem("theme", newTheme);
    document.getElementById("flexSwitchCheckDefault").checked = newTheme === "dark";

});