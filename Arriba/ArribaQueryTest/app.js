/// <reference path="Scripts/knockout-2.3.0.js" />
/// <reference path="Scripts/jquery-2.0.3.js" />
/// <reference path="Scripts/jquery-2.0.3.intellisense.js" />
var App = function()
{
    var self = this;

    self.queryString = ko.observable("");
    self.tableData = ko.observable();
    self.hasTableData = ko.observable(false);
    self.tableLoaded = ko.observable(false);
    self.errorMessage = ko.observable("");
    //self.totalMs = ko.computed(function () {
    //    return self.tableData().traceTimings['global.EndToEnd'].toFixed(2);
    //});

    // Setup
    self.queryString.subscribe(function (val) {
        // Query string updated 
        var query = "http://localhost:42784/select/Dev11.Bugs?q=" + encodeURIComponent(self.queryString());
        $.getJSON(query).done(function (payload) {

            var hasResults = (payload.content.total > 0);

            self.hasTableData(hasResults);

            self.tableData(hasResults ? payload : undefined);

        }).fail(function () {
            self.hasTableData(false);
            self.errorMessage("Error")
        });
    });

    $(function()
    {
        $.getJSON("http://localhost:42784/manage/load/Dev11.Bugs").done(function () { self.tableLoaded(true); })
    })
    
}