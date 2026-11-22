function updateThemeIcon(theme) {
    var icon = document.getElementById("themeIcon");
    if (!icon) return;
    icon.className = theme === "dark" ? "bi bi-moon-stars-fill fs-5" : "bi bi-sun-fill fs-5";
}

document.addEventListener("DOMContentLoaded", function () {
    var currentTheme = localStorage.getItem("theme") || document.documentElement.getAttribute("data-bs-theme") || "light";
    document.documentElement.setAttribute("data-bs-theme", currentTheme);
    updateThemeIcon(currentTheme);

    var btnSwitch = document.getElementById("btnSwitch");
    if (btnSwitch) {
        btnSwitch.addEventListener("click", function () {
            var activeTheme = document.documentElement.getAttribute("data-bs-theme");
            var newTheme = activeTheme === "light" ? "dark" : "light";
            document.documentElement.setAttribute("data-bs-theme", newTheme);
            localStorage.setItem("theme", newTheme);
            updateThemeIcon(newTheme);
        });
    }
});
