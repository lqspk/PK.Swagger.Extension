/*
 * By PK
 * 
 * 将接口导出到MarkDown格式文件
*/


$(function () {
    if ($("#exportToMarkDown").length == 0) {
        var div =
            "<div class=\"input\"><a id=\"exportToMarkDown\" class=\"header__btn\" href=\"#\" data-sw-translate onclick=\"exportToMarkDown();\">WebApi导出</a> <div class=\"input\"><a id=\"exportToWebMvcMarkDown\" class=\"header__btn\" href=\"#\" data-sw-translate onclick=\"exportToWebMvcMarkDown();\">WebMvc导出</a></div>";
        $("#explore").parent().after(div);

        $(".swagger-ui-wrap").css("max-width", "1130px");

        $("#exportToMarkDown").css("color", "white");
        $("#exportToMarkDown").css("background-color", "#547f00");
        $("#exportToMarkDown").css("font-size", "0.9em");
        $("#exportToMarkDown").css("text-decoration", "none");
        $("#exportToMarkDown").css("font-weight", "bold");
        $("#exportToMarkDown").css("padding", "6px 8px");
        $("#exportToMarkDown").css("border-radius", "4px");

        $("#exportToWebMvcMarkDown").css("color", "white");
        $("#exportToWebMvcMarkDown").css("background-color", "#547f00");
        $("#exportToWebMvcMarkDown").css("font-size", "0.9em");
        $("#exportToWebMvcMarkDown").css("text-decoration", "none");
        $("#exportToWebMvcMarkDown").css("font-weight", "bold");
        $("#exportToWebMvcMarkDown").css("padding", "6px 8px");
        $("#exportToWebMvcMarkDown").css("border-radius", "4px");
    }
});

//导出MarkDown
function exportToMarkDown() {
    var reg = new RegExp('/', "g");
    var path = encodeURI(swashbuckleConfig.discoveryPaths[0]);
    path = path.replace(reg, "|");
    location.href = '/Swagger/Export/ToMarkdown/' + path;
}

//导出MarkDown
function exportToWebMvcMarkDown() {
    var reg = new RegExp('/', "g");
    var path = encodeURI(swashbuckleConfig.discoveryPaths[0]);
    path = path.replace(reg, "|");
    location.href = '/Swagger/Export/ToWebMvcMarkdown/' + path;
}