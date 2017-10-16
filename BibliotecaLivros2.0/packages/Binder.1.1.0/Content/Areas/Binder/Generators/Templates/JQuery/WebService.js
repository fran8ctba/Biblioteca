//Service address
//Dependent on jquery!
var serviceAddress = "http://" + window.location.hostname + "/";
if (window.location.hostname == 'localhost') { //If working locally, include port number
    serviceAddress = "http://" + window.location.hostname + ":" + window.location.port + "/";
}

<#JQueryMethods#>

function request(requestUrl, type, data, success, error){
    //Make Request
    $.ajax({
        url: requestUrl,
        type: type,
        dataType: 'json',
        data: data,
        async: true,
        success: success,
        error: error});

}