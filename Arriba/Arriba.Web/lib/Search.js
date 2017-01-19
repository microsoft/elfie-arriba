var PatchComplianceSummary = React.createClass({
    displayName: "PatchComplianceSummary",

    render: function () {
        if (this.props.data["IsPatchCompliant"]) return React.createElement(
            "div",
            null,
            React.createElement("span", { className: "good-icon icon", title: "IsPatchCompliant:1" }),
            "Patch Compliant"
        );

        return React.createElement(
            "div",
            null,
            React.createElement(
                "div",
                null,
                React.createElement("span", { className: "error-icon icon" }),
                React.createElement(
                    "b",
                    null,
                    "Not Patch Compliant"
                ),
                React.createElement(DetailReportLink, { url: "https://insights/Report?id=07e846a5-a100-498b-8fe0-68a907b695cc&isPublished=true#assetName={ID}&_tab=0", id: this.props.data["Name"], title: "Patch Compliance Summary" })
            ),
            React.createElement(
                "table",
                { className: "smallTable" },
                React.createElement(
                    "thead",
                    null,
                    React.createElement(
                        "tr",
                        null,
                        React.createElement(
                            "td",
                            null,
                            "\xA0"
                        ),
                        React.createElement(
                            "td",
                            null,
                            "Sev 5"
                        ),
                        React.createElement(
                            "td",
                            null,
                            "Sev 4"
                        ),
                        React.createElement(
                            "td",
                            null,
                            "Sev 3"
                        )
                    )
                ),
                React.createElement(
                    "tbody",
                    null,
                    React.createElement(
                        "tr",
                        null,
                        React.createElement(
                            "td",
                            null,
                            "Out-of-Cycle"
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(BadUnlessZero, { value: this.props.data["Sev5_OutCycleCount"], columnName: "Sev5_OutCycleCount" })
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(BadUnlessZero, { value: this.props.data["Sev4_OutCycleCount"], columnName: "Sev4_OutCycleCount" })
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(BadUnlessZero, { value: this.props.data["Sev3_OutCycleCount"], columnName: "Sev3_OutCycleCount" })
                        )
                    ),
                    React.createElement(
                        "tr",
                        null,
                        React.createElement(
                            "td",
                            null,
                            "Third Party"
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(BadUnlessZero, { value: this.props.data["Sev5_ThirdPartyCount"], columnName: "Sev5_ThirdPartyCount" })
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(BadUnlessZero, { value: this.props.data["Sev4_ThirdPartyCount"], columnName: "Sev4_ThirdPartyCount" })
                        ),
                        React.createElement(
                            "td",
                            { className: "n" },
                            React.createElement(BadUnlessZero, { value: this.props.data["Sev3_ThirdPartyCount"], columnName: "Sev3_ThirdPartyCount" })
                        )
                    )
                )
            )
        );
    }
});

var AVSummary = React.createClass({
    displayName: "AVSummary",

    render: function () {
        // If no AntiVirus, just report that
        if (!this.props.data["HasAV"]) return React.createElement(RiskMarker, { condition: !this.props.data["HasAV"], columnName: "HasAV", label: "AV Missing" });

        // Otherwise, consider details
        return React.createElement(
            "div",
            null,
            React.createElement(DetailsField, { value: this.props.data["AVName"], columnName: "AVName", label: "AV", id: this.props.data["Name"], url: "https://insights/Report?id=07e846a5-a100-498b-8fe0-68a907b695cc&isPublished=true#assetName={ID}&_tab=2", "url-title": "AV Details" }),
            React.createElement(RelativeDateTime, { value: this.props.data["LatestScanDate"], columnName: "LatestScanDate", label: "Last Scan" }),
            React.createElement(RiskMarker, { condition: !this.props.data["IsScanned"], columnName: "IsScanned", label: "Unscanned" }),
            React.createElement(RiskMarker, { condition: !this.props.data["HasAuthenticatedScan"], columnName: "HasAuthenticatedScan", label: "Unauthenticated Scan" })
        );
    }
});

var MonitoringSummary = React.createClass({
    displayName: "MonitoringSummary",

    render: function () {
        // If not monitored, just say that
        if (!this.props.data["IsMonitored"]) return React.createElement(RiskMarker, { condition: !this.props.data["IsMonitored"], columnName: "IsMonitored", label: "Unmonitored" });

        // Otherwise, consider details
        return React.createElement(RelativeDateTime, { value: this.props.data["LatestMonitoredDate"], columnName: "LatestMonitoredDate", label: "Last Monitored" });
    }
});

// AssetDetails shows details for an asset specifically when clicked.
var AssetDetails = React.createClass({
    displayName: "AssetDetails",

    getInitialState: function () {
        return { allTableShown: true };
    },
    handleClose: function () {
        this.props.onClose();
    },
    toggle: function () {
        this.setState({ allTableShown: !this.state.allTableShown });
    },
    render: function () {
        if (!this.props.itemId) return null;
        if (!this.props.data) return null;

        var fullTeam = [];

        fullTeam.push(React.createElement(DetailsField, { value: this.props.data["S1"], columnName: "S1", inline: "true" }));
        if (this.props.data["S2"]) {
            fullTeam.push(" | ");
            fullTeam.push(React.createElement(DetailsField, { value: this.props.data["S2"], columnName: "S2", inline: "true" }));
        }

        if (this.props.data["S3"]) {
            fullTeam.push(" | ");
            fullTeam.push(React.createElement(DetailsField, { value: this.props.data["S3"], columnName: "S3", inline: "true" }));
        }

        if (this.props.data["S4"]) {
            fullTeam.push(" | ");
            fullTeam.push(React.createElement(DetailsField, { value: this.props.data["S4"], columnName: "S4", inline: "true" }));
        }

        var allTableToggle = React.createElement("span", { className: "icon-chevron-down-bold clickable", onClick: this.toggle });
        var detailsTable = null;

        if (this.state.allTableShown) {
            allTableToggle = React.createElement("span", { className: "icon-chevron-up-bold clickable", onClick: this.toggle });
            detailsTable = React.createElement(DetailsTable, {
                data: this.props.data,
                table: this.props.table,
                query: this.props.query,
                onAddClause: this.props.onAddClause
            });
        }

        return React.createElement(
            "div",
            { className: "details" },
            React.createElement(
                "div",
                { className: "commands" },
                React.createElement(DetailReportLink, { url: "https://insights/Report?id=5337be64-6142-4dec-8191-711c3043d2bb&isPublished=true#assetName={ID}&_tab=0", id: this.props.data["Name"], title: "Insights Asset Detail" }),
                React.createElement("span", { className: "icon icon-cancel", onClick: this.handleClose })
            ),
            React.createElement(
                "div",
                { className: "detailHeader" },
                React.createElement(
                    "div",
                    { className: "detailTitle font-light" },
                    this.props.itemId
                ),
                React.createElement(DetailsField, { value: "." + this.props.data["Domain"], columnName: "Domain", inline: "true" }),
                React.createElement(DetailsField, { value: this.props.data["OU"], columnName: "OU" }),
                React.createElement(
                    "div",
                    { className: "overviewBoxes" },
                    React.createElement(
                        "div",
                        { className: "box" },
                        "Asset",
                        React.createElement(
                            "div",
                            { className: "indent" },
                            React.createElement(DetailsField, { value: this.props.data["IP"], columnName: "IP", label: "IP" }),
                            React.createElement(DetailsField, { value: this.props.data["Type"], columnName: "Type" }),
                            React.createElement(DetailsField, { value: this.props.data["OSVersion"], columnName: "OSVersion", label: "OS" }),
                            React.createElement(DetailsField, { value: this.props.data["Environment"], columnName: "Environment", label: "Env" }),
                            React.createElement(DetailsField, { value: this.props.data["Asset_Source"], columnName: "Asset_Source", label: "Source" }),
                            React.createElement(DetailsField, { value: this.props.data["XpertRole"], columnName: "XPertRole", label: "XPert Role" })
                        ),
                        React.createElement("br", null),
                        "Ownership",
                        React.createElement(
                            "div",
                            { className: "indent" },
                            React.createElement(
                                "div",
                                null,
                                React.createElement(
                                    "span",
                                    null,
                                    "Org:"
                                ),
                                " ",
                                fullTeam
                            ),
                            React.createElement(MailLink, { to: this.props.data["ServiceOwner"], columnName: "ServiceOwner", subject: this.props.itemId, label: "FTE" }),
                            React.createElement(MailLink, { to: this.props.data["ServiceOwnerOperations"], columnName: "ServiceOwnerOperations", subject: this.props.itemId, label: "Ops" })
                        )
                    ),
                    React.createElement(
                        "div",
                        { className: "box" },
                        "Status",
                        React.createElement(
                            "div",
                            { className: "indent" },
                            React.createElement(DetailsField, { value: this.props.data["SecurityRisk"], columnName: "SecurityRisk", label: "Security Risk" }),
                            React.createElement(AVSummary, { data: this.props.data }),
                            React.createElement(MonitoringSummary, { data: this.props.data }),
                            React.createElement(RiskMarker, { condition: this.props.data["IsEdge"], columnName: "IsEdge", label: "Edge" }),
                            React.createElement(RiskMarker, { condition: this.props.data["IsDualHomed"], columnName: "IsDualHomed", label: "Dual-Homed" }),
                            React.createElement(PatchComplianceSummary, { data: this.props.data })
                        )
                    )
                ),
                React.createElement(
                    "div",
                    { className: "box" },
                    allTableToggle,
                    detailsTable
                )
            )
        );
    }
});
// zConfiguration.jsx contains configuration settings to configure this instance.
//  Until the site is fixed to use a proper require() model, this must be after the components [ResultListing] and before the modes [zGrid, zSearch] 

var configuration = {
    // Arriba Service URL to query
    //url: window.location.protocol + '//' + window.location.hostname + ":42784",
    url: "https://confluxsearch:42785",

    // Name of tool to show [top right and elsewhere]
    toolName: "Conflux",

    // Theme to use [see theme.css for options and to define others]
    theme: "theme-green",

    // E-Mail addresses to whom feedback should go
    feedbackEmailAddresses: "v-scolo@microsoft.com;mikefan@microsoft.com",

    // Listing: Default Columns and Sort Order to show per table
    listingDefaults: {
        "Asset": { columns: ["Name", "IP"], sortColumn: "Name", sortOrder: "asc" },
        "Employee": { columns: ["Alias", "Name"], sortColumn: "Alias", sortOrder: "asc" }
    },

    // Listing: Default details
    customDetailsProviders: {
        "Asset": AssetDetails
    },

    // Grid: Pre-built Grid queries
    gridDefaultQueries: {
        "Clear": {},
        "Scorecard": {
            query: "IsBaseline=1",
            currentTable: "Asset",
            rows: ["HasAuthenticatedScan=1", "IsPatchCompliant=1", "LatestScanDate > today-7", "IsMFA=1", "IsJIT=1", "IsDualHomed=0", "IsMonitored=1"],
            rowLabels: ["1.2 Vulnerability Scanning Coverage", "1.3 Scanned Servers with no Critical Vulns", "1.5 Servers with AV Installed & Scanned < 24 hrs", "2.2 Machines with MFA Implemented", "2.3 Environment Requiring JIT", "2.5 Non-Dual-Homed Assets", "3.1 Monitoring Coverage"],
            cols: ["S1=\"Universal Store\"", "S1=Xbox", "S1=Core", "S1=MDG"],
            colLabels: ["Uni Store All", "XSO All", "Core All", "Devices All"],
            showPortionOf: "col"
        },
        "Fixcard": {
            query: "IsBaseline=1",
            currentTable: "Asset",
            rows: ["!HasAuthenticatedScan=1", "!IsPatchCompliant=1", "!LatestScanDate > today-7", "!IsMFA=1", "!IsJIT=1", "!IsDualHomed=0", "!IsMonitored=1"],
            rowLabels: ["NOT 1.2 Vulnerability Scanning Coverage", "NOT 1.3 Scanned Servers with no Critical Vulns", "NOT 1.5 Servers with AV Installed & Scanned < 24 hrs", "NOT 2.2 Machines with MFA Implemented", "NOT 2.3 Environment Requiring JIT", "NOT 2.5 Non-Dual-Homed Assets", "NOT 3.1 Monitoring Coverage"],
            cols: ["S1=\"Universal Store\"", "S1=Xbox", "S1=Core", "S1=MDG"],
            colLabels: ["Uni Store All", "XSO All", "Core All", "Devices All"],
            showPortionOf: "col"
        }
    },

    // Content to show before a query is typed
    splashContent: React.createElement(
        "div",
        null,
        "ConfluxSearch is an instant-searchable index of Asset, Employee, and Administrator data.",
        React.createElement(
            "div",
            { className: "smaller" },
            "This information is highly sensitive. You may see a limited view of the information depending on permissions."
        )
    ),

    // Content to show if query fails due to access denied (401)
    accessDeniedContent: React.createElement(
        "span",
        null,
        "Access to ConfluxSearch denied. To request access to the overall site, please join ",
        React.createElement(
            "a",
            { target: "_blank", href: "https://idweb/IdentityManagement/aspx/common/GlobalSearchResult.aspx?searchtype=e0c132db-08d8-4258-8bce-561687a8a51e&content=Conflux Report Reader" },
            "Conflux Report Reader"
        ),
        ". Some content is restricted to other groups."
    ),

    // Content to show if query fails because service is unavailable (404, 500)
    serviceUnavailableContent: React.createElement(
        "span",
        null,
        "ConfluxSearch is currently unavailable. Please ",
        React.createElement(
            "a",
            { href: "mailto:v-scolo@microsoft.com;mikefan@microsoft.com&subject=ConfluxSearch%20Unavailable" },
            "report"
        ),
        " this issue."
    )
};
// Create a styled link to another report
var DetailReportLink = React.createClass({
    displayName: "DetailReportLink",

    urlReplace: function (urlFormat, id) {
        var idForUrl = encodeURIComponent(id);
        return urlFormat.replace(/{ID}/, idForUrl);
    },
    render: function () {
        return React.createElement(
            "a",
            { className: "detailLink", target: "_blank", href: this.urlReplace(this.props.url, this.props.id), title: this.props.title },
            React.createElement("span", { className: "icon-open-new-windows" })
        );
    }
});

// Creates a mailto link for the given 'to' address and optional subject, safely escaping and stripping highlight
var MailLink = React.createClass({
    displayName: "MailLink",

    render: function () {
        var mailToUrl = "mailto:" + encodeURIComponent(stripHighlight(this.props.to));
        var parameters = {
            subject: stripHighlight(this.props.subject),
            body: window.location.href
        };
        mailToUrl += buildUrlParameters(parameters);

        var content = React.createElement("a", { href: mailToUrl, title: this.props.columnName + ':' + stripHighlight(this.props.to), dangerouslySetInnerHTML: highlight(this.props.to) });
        if (this.props.bold !== "false") content = React.createElement(
            "b",
            null,
            content
        );

        var label = null;
        if (this.props.label) label = React.createElement(
            "span",
            null,
            this.props.label + ": "
        );

        return React.createElement(
            "div",
            null,
            label,
            " ",
            content
        );
    }
});

// Writes out a field with label and highlighting, but only if the value is set
var DetailsField = React.createClass({
    displayName: "DetailsField",

    render: function () {
        if (!this.props.value || this.props.value === "N/A" || this.props.value === "Unknown") return null;

        var link = null;
        if (this.props.url) link = React.createElement(DetailReportLink, { url: this.props.url, id: this.props.id, title: this.props["url-title"] });

        var content = React.createElement("span", { dangerouslySetInnerHTML: highlight(this.props.value), title: this.props.columnName + ':' + stripHighlight(this.props.value) });
        if (this.props.bold) content = React.createElement(
            "b",
            null,
            content
        );

        var label = null;
        if (this.props.label) label = React.createElement(
            "span",
            null,
            this.props.label + ": "
        );

        if (this.props.inline === "true") {
            return React.createElement(
                "span",
                null,
                label,
                " ",
                content,
                link
            );
        } else {
            return React.createElement(
                "div",
                null,
                label,
                " ",
                content,
                link
            );
        }
    }
});

// RiskMarker emits an inline value with a red exclamation point if the given condition is
var RiskMarker = React.createClass({
    displayName: "RiskMarker",

    render: function () {
        if (!this.props.condition) return null;
        return React.createElement(
            "div",
            null,
            React.createElement("span", { className: "error-icon icon", title: this.props.columnName }),
            React.createElement(
                "b",
                null,
                this.props.label
            )
        );
    }
});

// RiskNumber emits a number which is red if not zero
var BadUnlessZero = React.createClass({
    displayName: "BadUnlessZero",

    render: function () {
        if (!this.props.value || this.props.value.toString() === "0") return React.createElement(
            "span",
            { title: this.props.columnName + ':0' },
            "0"
        );
        return React.createElement(
            "b",
            { title: this.props.columnName + ':' + this.props.value },
            this.props.value
        );
    }
});

// RelativeDateTime writes a DateTime in terms of now (ex: 3 days ago), and highlights it yellow if over 2.5 days and red if over 6.5 days.
var RelativeDateTime = React.createClass({
    displayName: "RelativeDateTime",

    render: function () {
        var label = null;
        if (this.props.label) label = React.createElement(
            "span",
            null,
            this.props.label + " "
        );

        if (!this.props.value) {
            return React.createElement(
                "div",
                null,
                React.createElement("span", { className: "error-icon icon" }),
                label,
                " Unknown"
            );
        }

        var dateTime = moment(this.props.value);
        var howLongSince = moment.duration(moment().diff(dateTime));

        var prefix = null;
        if (howLongSince.asDays() > 6.5) {
            prefix = React.createElement("span", { className: "error-icon icon" });
        } else if (howLongSince.asDays() > 2.5) {
            prefix = React.createElement("span", { className: "warn-icon icon" });
        }

        var content = React.createElement(
            "span",
            null,
            dateTime.fromNow()
        );
        if (prefix) content = React.createElement(
            "b",
            null,
            dateTime.fromNow()
        );

        return React.createElement(
            "div",
            { title: this.props.columnName + ':' + this.props.value },
            prefix,
            label,
            content
        );
    }
});
// AddColumnList is the list of unselected columns which shows up in the listing when the '+' is clicked.
var AddColumnList = React.createClass({
    displayName: "AddColumnList",

    getInitialState: function () {
        return { filter: null, filteredColumns: this.filterColumns(null), selectedIndex: 0 };
    },
    filterColumns: function (filter) {
        if (!filter) filter = "";
        filter = filter.toLowerCase();

        var filteredColumns = [];
        for (var i = 0; i < this.props.allColumns.length; ++i) {
            var column = this.props.allColumns[i];

            // Filter columns already added and those not starting with the filter
            if (this.props.currentColumns.indexOf(column.name) !== -1) continue;
            if (filter && column.name.toLowerCase().indexOf(filter) !== 0) continue;

            filteredColumns.push(column.name);
        }

        return filteredColumns;
    },
    handleKeyDown: function (e) {
        if (e.keyCode === 27) {
            // ESC - Close AddColumnList
            this.setState(this.getInitialState());
            this.props.onAddColumn(null);
            e.stopPropagation();
        } else if (e.keyCode === 13 || e.keyCode === 9) {
            // ENTER/TAB - commit highlighted match
            var currentColumn = this.state.filteredColumns[this.state.selectedIndex];
            if (currentColumn) this.props.onAddColumn(currentColumn);
            e.stopPropagation();
        } else if (e.keyCode === 38) {
            // Up Arrow - select previous
            this.setState({ selectedIndex: this.state.selectedIndex <= 0 ? 0 : this.state.selectedIndex - 1 });
            e.stopPropagation();
        } else if (e.keyCode === 40) {
            // Down Arrow - select next
            this.setState({ selectedIndex: this.state.selectedIndex >= this.state.filteredColumns.length ? this.state.filteredColumns.length - 1 : this.state.selectedIndex + 1 });
            e.stopPropagation();
        }
    },
    handleAddColumn: function (e) {
        this.props.onAddColumn(e.target.getAttribute("data-name"));
        e.stopPropagation();
    },
    handleFilterChanged: function (e) {
        var newFilter = e.target.value;
        var newFilteredColumns = this.filterColumns(newFilter);
        this.setState({ filter: newFilter, filteredColumns: newFilteredColumns, selectedIndex: 0 });
    },
    render: function () {
        // Write an add column list (shown only once the '+' is clicked)
        if (!this.props.showing) return null;

        var addFunction = this.handleAddColumn;
        var addColumns = [];
        for (var i = 0; i < this.state.filteredColumns.length; ++i) {
            var name = this.state.filteredColumns[i];
            var className = i === this.state.selectedIndex ? "add-list-selected" : "";
            addColumns.push(React.createElement(
                "div",
                { key: "add_" + name, "data-name": name, onClick: addFunction, className: className },
                name
            ));
        }

        return React.createElement(
            "div",
            { style: { position: "absolute" } },
            React.createElement(
                "div",
                { className: "add-list", onKeyDown: this.handleKeyDown },
                React.createElement("input", { type: "text", autoFocus: true, placeholder: "Filter", value: this.state.filter, onChange: this.handleFilterChanged, onKeyDown: this.handleKeyDown }),
                addColumns
            )
        );
    }
});
// InfiniteScroll provides infinite scrolling support.
/* Usage:
    <InfiniteScroll page={this.state.page} hasMoreData={this.state.hasMoreData} loadMore={this.getResultsPage}>
        {content}
    </InfiniteScroll>

    Page identifies the integer page of items loaded so far, starting at zero.
    When scrolling is required, loadMore will be called for page+1.
    Page updating on the InfiniteScroll class tells it that it can call loadMore again (the previous loadMore is complete).
    hasMoreData is set to false when there are no more items to load and loadMore should no longer be called.
*/
var InfiniteScroll = React.createClass({
    displayName: "InfiniteScroll",

    handleScroll: function (e) {
        var element = e.target;
        if (e.target === this.refs.scrollContainer) {
            var pixelsFromBottom = element.scrollHeight - element.clientHeight - element.scrollTop;

            if (pixelsFromBottom < 200) {
                // Remove this listener to avoid multiple loadMore calls
                // "page" changing in the caller will cause this to re-render and be re-attached
                e.target.removeEventListener("onScroll", this.handleScroll);

                // Load the next page of items
                if (this.props.hasMoreData) {
                    this.props.loadMore(this.props.page + 1);
                }
            }
        }
    },
    render: function () {
        return React.createElement(
            "div",
            { ref: "scrollContainer", className: "scrollable", onScroll: this.handleScroll },
            this.props.children
        );
    }
});
// QueryStats is a bar just under the SearchHeader. It shows errors, if any, or the current query, returned count, and runtime.
var QueryStats = React.createClass({
    displayName: "QueryStats",

    onTableTileClick: function (name, e) {
        this.props.onSelectedTableChange(name);
    },
    render: function () {
        if (this.props.error) {
            // If the query didn't return, show an error
            return React.createElement(
                "div",
                { className: "queryStats" },
                React.createElement("span", { className: "error-icon icon" }),
                React.createElement(
                    "span",
                    { className: "error" },
                    this.props.error
                )
            );
        } else {
            // If there's no query, keep this area empty
            if (!this.props.allCountData || !this.props.allCountData.content) return null;
            var allCountContent = this.props.allCountData.content;

            // Write tiles with results per table
            var tiles = [];
            for (var i = 0; i < allCountContent.length; ++i) {
                var tableResult = allCountContent[i];

                var cssClasses = "";
                var parts = [];

                if (tableResult.tableName === this.props.currentTable) cssClasses += " current";

                if (!tableResult.allowedToRead) {
                    parts.push(React.createElement("span", { className: "lock-icon icon" }));
                } else if (!tableResult.succeeded) {
                    parts.push(React.createElement("span", { className: "error-icon icon" }));
                } else {
                    parts.push(React.createElement(
                        "span",
                        { className: "countValue" },
                        tableResult.count.toLocaleString()
                    ));
                }

                tiles.push(React.createElement(
                    "span",
                    { key: "tableTile_" + tableResult.tableName, className: cssClasses + " statsForTable clickable", onClick: this.onTableTileClick.bind(this, tableResult.tableName) },
                    parts,
                    React.createElement(
                        "span",
                        null,
                        tableResult.tableName
                    )
                ));
            }

            // Write details for selected table
            var selectedDetails = [];
            if (this.props.selectedData && this.props.selectedData.content) {
                var selectedContent = this.props.selectedData.content;

                if (selectedContent.query.where) {
                    selectedDetails.push(React.createElement(
                        "span",
                        null,
                        "for ",
                        React.createElement(
                            "span",
                            { className: "h" },
                            selectedContent.query.where
                        )
                    ));
                }

                if (selectedContent.details.errors) {
                    selectedDetails.push(React.createElement(
                        "span",
                        null,
                        "\xA0",
                        React.createElement("span", { className: "error-icon icon" }),
                        React.createElement(
                            "b",
                            null,
                            selectedContent.details.errors
                        )
                    ));
                }

                if (selectedContent.details.warnings) {
                    selectedDetails.push(React.createElement(
                        "span",
                        null,
                        "\xA0",
                        React.createElement("span", { className: "icon-warning icon" }),
                        React.createElement(
                            "b",
                            null,
                            selectedContent.details.warnings
                        )
                    ));
                }

                if (selectedContent.details.accessDeniedColumns) {
                    var deniedColumnList = "Denied Columns: ";
                    for (var i = 0; i < selectedContent.details.accessDeniedColumns.length; ++i) {
                        if (i > 0) deniedColumnList += ", ";
                        deniedColumnList += selectedContent.details.accessDeniedColumns[i];
                    }

                    selectedDetails.push(React.createElement(
                        "span",
                        null,
                        "\xA0",
                        React.createElement("span", { className: "icon-lock icon", title: deniedColumnList })
                    ));
                }

                if (this.props.rssUrl && selectedContent.details.succeeded) {
                    selectedDetails.push(React.createElement(
                        "a",
                        { title: "RSS Link", target: "_blank", href: this.props.rssUrl },
                        React.createElement("img", { className: "rss", src: "icons/feed-icon-14x14.png", style: { position: "relative", top: "1px" }, alt: "RSS" })
                    ));
                }

                if (this.props.csvUrl && selectedContent.details.succeeded) {
                    selectedDetails.push(React.createElement(
                        "a",
                        { title: "Download CSV", target: "_blank", href: this.props.csvUrl },
                        React.createElement("span", { className: "icon-download" })
                    ));
                }
            }
        }

        return React.createElement(
            "div",
            { className: "queryStats" },
            tiles,
            selectedDetails
        );
    }
});
// ResultDetails shows (default formatted) details for an item when it was clicked.
var ResultDetails = React.createClass({
    displayName: "ResultDetails",

    handleClose: function () {
        this.props.onClose();
    },
    render: function () {
        if (!this.props.itemId) return null;
        if (!this.props.data) return null;

        return React.createElement(
            "div",
            { className: "details" },
            React.createElement(
                "div",
                { className: "commands" },
                React.createElement("span", { className: "icon icon-cancel", onClick: this.handleClose })
            ),
            React.createElement(
                "div",
                null,
                React.createElement(
                    "div",
                    { className: "detailHeader" },
                    React.createElement(
                        "div",
                        { className: "detailTitle" },
                        this.props.itemId
                    )
                ),
                React.createElement(
                    "div",
                    { className: "detailBody" },
                    React.createElement(
                        "div",
                        { className: "box" },
                        React.createElement(DetailsTable, {
                            data: this.props.data,
                            table: this.props.table,
                            query: this.props.query,
                            onAddClause: this.props.onAddClause })
                    )
                )
            )
        );
    }
});
// ResultListing shows a table of items matching a query, with sortable columns
var ResultListing = React.createClass({
    displayName: "ResultListing",

    getInitialState: function () {
        return { selectedIndex: -1, addColumnsShowing: false };
    },
    handleResort: function (e) {
        // If a column heading was clicked, re-sort the table
        var columnNameClicked = e.target.getAttribute("data-name");
        var sortOrder = "asc";

        // If this column is already the sort column, toggle ascending/descending
        if (this.props.sortColumn === columnNameClicked) {
            sortOrder = this.props.sortOrder === "asc" ? "desc" : "asc";
        }

        // Store the sort column and order and raise the "onResort" event
        this.props.onResort(columnNameClicked, sortOrder);

        e.stopPropagation();
    },
    handleSelect: function (e) {
        var rowElement = e;
        this.setState({ selectedIndex: rowElement.props.itemIndex, addColumnsShowing: false });
        this.props.onSelectionChanged(rowElement.props.itemId);
    },
    handleAdd: function (e) {
        if (e.target === this.refs.addButton) {
            this.setState({ addColumnsShowing: !this.state.addColumnsShowing });
        }
        e.stopPropagation();
    },
    onAddColumn: function (name) {
        if (name) {
            var columns = this.props.data.content.query.columns;
            columns.push(name);

            this.setState({ addColumnsShowing: false }, this.props.onSetColumns(columns));
        } else {
            this.setState({ addColumnsShowing: false });
        }
    },
    handleRemoveColumn: function (e) {
        var columnName = e.target.getAttribute("data-column");
        var newColumns = this.props.data.content.query.columns.filter(function (name) {
            return name !== columnName;
        });
        this.props.onSetColumns(newColumns);

        e.stopPropagation();
    },
    handlePivot: function (e) {
        var element = e.target;

        this.props.onPivot(element.getAttribute("data-table"), element.getAttribute("data-query"));
        e.stopPropagation();
    },
    selectByRelativeIndex: function (i) {
        // Figure out the current row count
        var count = 0;
        if (this.props.data.content) count = this.props.data.content.values.rows.length;

        // See what index the caller wants selected
        var newSelectedIndex = this.state.selectedIndex + i;

        // If nothing was selected, let down open the first item
        if (!this.props.selectedId) newSelectedIndex = -1 + i;

        // If out-of-bounds, do nothing
        if (newSelectedIndex < 0 || newSelectedIndex >= count) return;

        // Otherwise, trigger a selection change
        var idColumnIndex = this.props.data.content.query.columns.indexOf(this.props.idColumn);
        var row = this.props.data.content.values.rows[newSelectedIndex];
        this.setState({ selectedIndex: newSelectedIndex });
        this.props.onSelectionChanged(stripHighlight(row[idColumnIndex]));
    },
    render: function () {
        var content = this.props.data.content;
        if (!content || !content.details.succeeded || !this.props.idColumn) return null;

        var idColumn = this.props.idColumn;
        var idColumnIndex = content.query.columns.indexOf(this.props.idColumn);

        // Write a column heading row - click to sort, remove/add columns
        var columnCells = [];
        for (var i = 0; i < content.values.columns.length; ++i) {
            var column = content.values.columns[i];

            var commands = [];

            // Non-ID column commands
            if (column.name !== idColumn) {
                // Remove button
                commands.push(React.createElement("div", { key: "remove_" + column.name, "data-column": column.name, className: "icon-cancel icon-column-heading", title: "Remove Column", onClick: this.handleRemoveColumn }));

                // Add 'Pivot to Grid' URL
                var gridUrl = "Grid.html" + buildUrlParameters({ q: content.query.where, t: content.query.tableName, R1: column.name + ">" });
                commands.push(React.createElement("a", { href: gridUrl, className: "icon-view-all-albums icon-column-heading", title: "Grid By " + column.name }));
            }

            // Last column
            if (i == content.values.columns.length - 1) {
                // Add column button
                commands.push(React.createElement(
                    "div",
                    { ref: "addButton", className: "add-column-button icon-add icon-column-heading", title: "Add Column", onClick: this.handleAdd },
                    React.createElement(AddColumnList, { showing: this.state.addColumnsShowing,
                        onAddColumn: this.onAddColumn,
                        allColumns: this.props.allColumns,
                        currentColumns: content.query.columns })
                ));
            }

            columnCells.push(React.createElement(
                "td",
                { key: "heading_" + column.name, "data-name": column.name, onClick: this.handleResort },
                React.createElement(
                    "div",
                    { className: "commands" },
                    commands
                ),
                column.name
            ));
        }

        // Write a row for each item
        var index = 0;
        var selectFunction = this.handleSelect;
        var selectedId = this.props.selectedId;
        var listingItems = content.values.rows.map(function (row) {
            var id = stripHighlight(row[idColumnIndex]);
            return React.createElement(ResultListingItem, { key: id, itemId: id, itemIndex: index++, data: row, onSelectionChanged: selectFunction, selected: selectedId === id });
        });

        return React.createElement(
            "table",
            { className: "resultTable", tabIndex: "2" },
            React.createElement(
                "thead",
                null,
                React.createElement(
                    "tr",
                    null,
                    columnCells
                )
            ),
            React.createElement(
                "tbody",
                null,
                listingItems
            )
        );
    }
});

var ResultListingItem = React.createClass({
    displayName: "ResultListingItem",

    handleClick: function (e) {
        this.props.onSelectionChanged(this);
    },
    render: function () {
        var id = this.props.itemId;

        // Write a cell for each property of each item with highlighting
        var cells = [];
        for (var i = 0; i < this.props.data.length; ++i) {
            var item = this.props.data[i];
            cells[i] = React.createElement("td", { key: id + "_" + i, title: stripHighlight(item), dangerouslySetInnerHTML: highlight(item) });
        }

        return React.createElement(
            "tr",
            { onClick: this.handleClick, className: this.props.selected ? "selected theme-background-light" : "theme-hover-light" },
            cells
        );
    }
});
// SearchHeader contains the top bar - branching, the search box, and top-level buttons
var SearchHeader = React.createClass({
    displayName: "SearchHeader",

    componentDidMount: function () {
        ReactDOM.findDOMNode(this.refs.searchBox).focus();
    },
    render: function () {
        var placeholderText = "Search for ";
        if (this.props.tables) {
            for (var i = 0; i < this.props.tables.length; ++i) {
                if (i > 0) placeholderText += ", ";
                placeholderText += this.props.tables[i];
            }
        }
        placeholderText += "...";

        return React.createElement(
            "div",
            { className: "header theme-background-medium" },
            React.createElement(
                "div",
                { className: "title font-light theme-background-vdark" },
                React.createElement(
                    "a",
                    { href: "/" },
                    this.props.name
                )
            ),
            React.createElement(
                "div",
                { className: "searchBar" },
                React.createElement("input", { id: "searchBox", ref: "searchBox", type: "text", placeholder: placeholderText, tabIndex: "1", onInput: this.props.onSearchChange, value: this.props.query }),
                React.createElement(
                    "div",
                    { className: "searchIcon" },
                    React.createElement("i", { className: "icon-find" })
                )
            ),
            React.createElement(
                "div",
                { className: "buttons" },
                React.createElement(
                    "a",
                    { className: "theme-background-dark", href: "mailto:?subject=" + encodeURIComponent(this.props.name) + ": " + encodeURIComponent(this.props.query) + "&body=" + encodeURIComponent(window.location.href) },
                    React.createElement("i", { className: "icon-mail", title: "Mail" })
                ),
                React.createElement(
                    "a",
                    { className: "theme-background-dark", href: "mailto:" + encodeURIComponent(this.props.feedbackEmailAddresses) + "?subject=" + encodeURIComponent(this.props.name) + " Feedback" },
                    React.createElement("i", { className: "icon-emoji2", title: "Feedback" })
                ),
                React.createElement(
                    "a",
                    { className: "theme-background-dark", href: "/?help=true" },
                    React.createElement("i", { className: "icon-help", title: "Help" })
                )
            )
        );
    }
});
// Inspired by https://github.com/tomkp/react-split-pane [MIT License]
var SplitPane = React.createClass({
    displayName: 'SplitPane',

    defaultProps: { isFirstVisible: true, isSecondVisible: true },
    onMouseUp: function () {
        document.removeEventListener('mouseup', this.onMouseUp);
        document.removeEventListener('mousemove', this.onMouseMove);
    },
    onMouseDown: function (e) {
        document.addEventListener('mouseup', this.onMouseUp);
        document.addEventListener('mousemove', this.onMouseMove);

        this.setState({ totalWidth: this.refs.container.getBoundingClientRect().width });
    },
    onMouseMove: function (e) {
        var node = ReactDOM.findDOMNode(this.refs.first);
        var newWidth = e.clientX - node.offsetLeft;

        if (this.props.minSize) {
            if (newWidth < this.props.minSize) newWidth = this.props.minSize;
            if (newWidth > this.state.totalWidth - this.props.minSize) newWidth = this.state.totalWidth - this.props.minSize;
        }

        node.style.width = newWidth + 'px';
        node.style.flex = 'none';
    },
    render: function () {
        if (!this.props.isFirstVisible) return this.props.children[1];
        if (!this.props.isSecondVisible) return this.props.children[0];

        return React.createElement(
            'div',
            { ref: 'container', className: 'splitpane' },
            React.createElement(
                'div',
                { ref: 'first', className: 'pane' },
                this.props.children[0]
            ),
            React.createElement('div', { ref: 'handle',
                className: 'resizer vertical',
                onMouseDown: this.onMouseDown }),
            React.createElement(
                'div',
                { ref: 'second', className: 'pane' },
                this.props.children[1]
            )
        );
    }
});
var SyntaxHelp = React.createClass({
    displayName: "SyntaxHelp",

    getInitialState: function () {
        return { showHelp: this.props.showHelp };
    },
    toggle: function () {
        this.setState({ showHelp: !this.state.showHelp });
    },
    render: function () {
        if (!this.state.showHelp) {
            return React.createElement(
                "div",
                { className: "splash" },
                React.createElement(
                    "div",
                    { style: { width: "100%" } },
                    this.props.splashContent,
                    React.createElement(
                        "div",
                        { className: "buttons" },
                        React.createElement(
                            "a",
                            { className: "theme-background-dark", href: "/?help=true" },
                            React.createElement("i", { className: "icon-help", title: "Help" })
                        )
                    )
                )
            );
        }

        return React.createElement(
            "div",
            { className: "helpContent" },
            React.createElement(
                "div",
                { className: "helpIntro" },
                this.props.splashContent
            ),
            React.createElement(
                "div",
                { className: "features" },
                React.createElement(
                    "h2",
                    null,
                    "Features"
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click items to see details."
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click column headings to sort."
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click ",
                    React.createElement("span", { className: "icon-add" }),
                    " to add columns to listing, ",
                    React.createElement("span", { className: "icon-cancel" }),
                    " to remove them."
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click ",
                    React.createElement("span", { className: "icon-download" }),
                    " to download listing (same columns, query, and order) as CSV."
                ),
                React.createElement(
                    "div",
                    null,
                    " - Click ",
                    React.createElement("img", { src: "icons/feed-icon-14x14.png", alt: "RSS" }),
                    " for URL to subscribe to query."
                )
            ),
            React.createElement(
                "div",
                { className: "syntaxIntro" },
                React.createElement(
                    "h2",
                    null,
                    "Syntax"
                ),
                React.createElement(
                    "table",
                    { className: "syntaxTable" },
                    React.createElement(
                        "thead",
                        null,
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "th",
                                { style: { width: "25%" } },
                                "Rule"
                            ),
                            React.createElement(
                                "th",
                                { style: { width: "25%" } },
                                "Example"
                            ),
                            React.createElement(
                                "th",
                                { style: { width: "50%" } },
                                "Meaning"
                            )
                        )
                    ),
                    React.createElement(
                        "tbody",
                        null,
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Type anything to search across all columns."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "halo Pris"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Find items with words starting with \"halo\" and \"Pris\" anywhere, case insensitive."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use <column> <operator> <value> to search one column."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "Team=Central AccessLevel > 3"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team equals \"Central\" (case sensitive) and AccessLevel is over 3."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use 'AND', 'OR', 'NOT', and parens for subexpressions."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "NOT Team=Central AND (AccessLevel > 3 OR Role=Administrator)"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team is not Central and (AccessLevel is over 3 or Role is Administrator."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use any operators from Web, SQL, or C# syntax."
                            ),
                            React.createElement(
                                "td",
                                null,
                                React.createElement(
                                    "table",
                                    { className: "syntaxTable" },
                                    React.createElement(
                                        "tbody",
                                        null,
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "&&"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "&"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "AND"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "aNd"
                                            )
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "||"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "|"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "OR"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "Or"
                                            )
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "!"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "-"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "NOT"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "noT"
                                            )
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "="
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "=="
                                            ),
                                            React.createElement("td", null),
                                            React.createElement("td", null)
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "<>"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "!="
                                            ),
                                            React.createElement("td", null),
                                            React.createElement("td", null)
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "|>"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "STARTSWITH"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "UNDER"
                                            ),
                                            React.createElement("td", null)
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                ":"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "MATCH"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "FREETEXT"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "CONTAINS"
                                            )
                                        ),
                                        React.createElement(
                                            "tr",
                                            null,
                                            React.createElement(
                                                "td",
                                                null,
                                                "::"
                                            ),
                                            React.createElement(
                                                "td",
                                                null,
                                                "MATCHEXACT"
                                            ),
                                            React.createElement("td", null),
                                            React.createElement("td", null)
                                        )
                                    )
                                )
                            ),
                            React.createElement("td", null)
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use ':' for \"has a word starting with\" or \"::\" for \"has the exact word\""
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "Team:Centr && Name::Will"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team contains words starting with \"Centr\" and Name has the full word \"Will\" (\"William\" would not match) (case insensitive)."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use '|>' for starts with."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "Team |> Centr"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team starts with \"Centr\" (\"Grand Central\" would not match) (case sensitive)."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use \"\" to look for empty values."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "Team=\"\""
                            ),
                            React.createElement(
                                "td",
                                null,
                                "Team is empty."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use \"today-n\" (no spaces) for relative dates."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "HireDate < today-60"
                            ),
                            React.createElement(
                                "td",
                                null,
                                "HireDate is more than 60 days ago [UTC]."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Use any .NET DateTime.Parse-able formats."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "HireDate > \"2016-10-01 10:00AM\""
                            ),
                            React.createElement(
                                "td",
                                null,
                                "HireDate is after Oct 1, 2016 10:00 AM [UTC]."
                            )
                        ),
                        React.createElement(
                            "tr",
                            null,
                            React.createElement(
                                "td",
                                null,
                                "Wrap column names with braces and values with quotes if they contain spaces. Escape braces and quotes by doubling them."
                            ),
                            React.createElement(
                                "td",
                                { className: "font-example" },
                                "[Owner [Ops]]]=\"Bilbo \"\"Ringbearer\"\" Baggins\""
                            ),
                            React.createElement(
                                "td",
                                null,
                                "The {Owner [Ops]} column equals {Bilbo \"Ringbearer\" Baggins}."
                            )
                        )
                    )
                )
            ),
            React.createElement(
                "div",
                { className: "syntaxExamples" },
                React.createElement(
                    "h2",
                    null,
                    "Examples"
                ),
                React.createElement(
                    "div",
                    { className: "exampleBox" },
                    React.createElement(
                        "div",
                        { className: "font-example" },
                        "Team=\"Central\" AND IP:10.194"
                    ),
                    React.createElement(
                        "div",
                        null,
                        "Find items where Team equals \"Central\", the IP address starts with \"10.194\"."
                    ),
                    React.createElement(
                        "div",
                        { className: "indent" },
                        "The word index operators (':', '::') are not case sensitive, but all other operators ('=', '!=', '|>') are case sensitive.",
                        React.createElement("br", null),
                        "The word index operators only match from word boundaries, so \"Team:ent\" will not match a Team of \"Central\".",
                        React.createElement("br", null),
                        "Text is split into alphanumeric words and dotted phrases. (\"10.194.155.11\" splits to \"10\", \"194\", \"155\", \"11\", \"10.194.155.11\")",
                        React.createElement("br", null),
                        "Therefore, \"IP:10.194\" will match \"10.194.155.11\" but not \"11.10.194.155\".",
                        React.createElement("br", null)
                    )
                ),
                React.createElement(
                    "div",
                    { className: "exampleBox" },
                    React.createElement(
                        "div",
                        { className: "font-example" },
                        "[Team]=\"Central\" && (HireDate < today-60 || IsManager == 1)"
                    ),
                    React.createElement(
                        "div",
                        null,
                        "Find items with Team \"Central\" which were hired in the last 60 days or are managers."
                    ),
                    React.createElement(
                        "div",
                        { className: "indent" },
                        "\"Today\" is midnight UTC, so \"today-1\" will match 11:59p the day before yesterday (UTC).",
                        React.createElement("br", null),
                        "DateTimes in queries are interpreted at UTC.",
                        React.createElement("br", null)
                    )
                )
            )
        );
    }
});
// zDefaultConfiguration.jsx contains default configuration settings for this site instance.
// Add a copy in configuration\configuration.jsx to override these settings.

if (!configuration) {
    var configuration = {
        // Arriba Service URL to query
        // Hardcode to the name of your service. Use port 42784 for HTTP, 42785 for HTTPS.
        url: "https://" + window.location.hostname + ":42785",

        // Name of tool to show [top right and elsewhere]
        toolName: "Arriba",

        // Theme to use [see theme.css for options and to define others]
        theme: "theme-teal",

        // E-Mail addresses to whom feedback should go
        feedbackEmailAddresses: "",

        // Listing: Default Columns and Sort Order to show per table
        listingDefaults: {
            /* "<TableName">: { columns: ["<Col1>", "Col2", "..."], sortColumn: "<SortColumnName>", sortOrder: "asc" } */
        },

        // Listing: Default details rendering per table
        customDetailsProviders: {
            /* "<TableName>": <ReactClassNameUnquoted> */
        },

        // Grid: Pre-defined Grid queries
        gridDefaultQueries: {
            "Clear": {}
        },

        // Content to show before a query is typed
        splashContent: React.createElement(
            "div",
            null,
            "Arriba is a data exploration, free text search, and structured search engine."
        ),

        // Content to show if query fails due to access denied (401)
        accessDeniedContent: React.createElement(
            "span",
            null,
            "Access to Arriba Search denied."
        ),

        // Content to show if query fails because service is unavailable (404, 500)
        serviceUnavailableContent: React.createElement(
            "span",
            null,
            "Arriba Search is currently unavailable."
        )
    };
}
// NOTE: Depends on configuration from zConfiguration.jsx.

// The GridFunctionCell is the corner cell which shows the aggregation function and column [COUNT(*)].
var GridFunctionCell = React.createClass({
    displayName: "GridFunctionCell",

    getInitialState: function () {
        return {
            isEditing: false,
            aggregationFunction: this.props.aggregationFunction,
            aggregateColumn: this.props.aggregateColumn
        };
    },
    handleEdit: function () {
        this.setState({ isEditing: true });
    },
    handleChange: function () {
        this.setState({ aggregationFunction: this.refs.aggregationFunction.value, aggregateColumn: this.refs.aggregateColumn.value });
    },
    handleCancel: function () {
        this.setState(this.getInitialState());
    },
    handleAccept: function () {
        this.setState({ isEditing: false });
        this.props.onChange(this.refs.aggregationFunction.value, this.refs.aggregateColumn.value);
    },
    isNumericColumn: function (t) {
        t = t.toLowerCase();
        return t.indexOf("int") === 0 || t.indexOf("uint") === 0 || t === "short" || t === "byte" || t === "float" || t === "double";
    },
    render: function () {
        if (this.state.isEditing) {
            var columns = [];

            if (this.state.aggregationFunction === "COUNT") {
                columns.push(React.createElement("option", { key: "C*", value: "" }));
            } else {
                for (var i = 0; i < this.props.allColumns.length; ++i) {
                    if (!this.isNumericColumn(this.props.allColumns[i].type)) continue;
                    var columnName = this.props.allColumns[i].name;
                    columns.push(React.createElement(
                        "option",
                        { key: columnName, value: columnName },
                        columnName
                    ));
                }
            }

            return React.createElement(
                "td",
                { className: "grid-heading" },
                React.createElement("span", { className: "icon-cancel icon-column-heading clickable", title: "Cancel", onClick: this.handleCancel }),
                React.createElement("span", { className: "icon-accept icon-column-heading clickable", title: "Done", onClick: this.handleAccept }),
                React.createElement(
                    "div",
                    { className: "cell" },
                    React.createElement(
                        "select",
                        { ref: "aggregationFunction", value: this.state.aggregationFunction, onChange: this.handleChange },
                        React.createElement(
                            "option",
                            null,
                            "COUNT"
                        ),
                        React.createElement(
                            "option",
                            null,
                            "SUM"
                        ),
                        React.createElement(
                            "option",
                            null,
                            "MIN"
                        ),
                        React.createElement(
                            "option",
                            null,
                            "MAX"
                        )
                    ),
                    "\xA0of\xA0",
                    React.createElement(
                        "select",
                        { ref: "aggregateColumn", value: this.state.aggregateColumn, onChange: this.handleChange },
                        columns
                    )
                )
            );
        }

        return React.createElement(
            "td",
            { className: "grid-heading", onClick: this.handleEdit },
            React.createElement(
                "div",
                null,
                this.props.aggregationFunction + "(" + this.props.aggregateColumn + ")"
            )
        );
    }
});

// GridHeadingCell renders the cells used in the first row and column
var GridHeadingCell = React.createClass({
    displayName: "GridHeadingCell",

    getInitialState: function () {
        return {
            isEditing: false,
            label: this.props.label,
            query: this.props.value
        };
    },
    componentDidMount: function () {
        if (this.props.value === "All") document.body.addEventListener("keydown", this.handleKeyDown);
    },
    handleKeyDown: function (e) {
        if (e.altKey) {
            // Alt+Right Arrow -> new column
            if (e.keyCode == 39 && this.props.type === "column") {
                this.handleEdit();
            }

            // Alt+Down Arrow -> new row
            if (e.keyCode == 40 && this.props.type === "row") {
                this.handleEdit();
            }
        }
    },
    handleRemove: function (e) {
        this.props.onChange(this.props.type, this.props.index, "", "");
        e.stopPropagation();
    },
    handleEdit: function (e) {
        var editQuery = this.props.value === "All" ? "" : this.props.value;
        this.setState({ isEditing: true, query: editQuery });
        e.stopPropagation();
    },
    handleQueryChange: function (e) {
        this.setState({ query: e.target.value });
    },
    handleLabelChange: function (e) {
        this.setState({ label: e.target.value });
    },
    handleCancel: function (e) {
        this.setState({ isEditing: false, query: this.props.value, label: this.props.label });
    },
    handleAccept: function (e) {
        this.props.onChange(this.props.type, this.props.index, this.refs.query.value, this.refs.label.value);
        this.setState({ isEditing: false });
    },
    handleKey: function (e) {
        // ESC: Cancel change
        if (e.keyCode === 27) {
            this.setState({ isEditing: false, query: this.props.value, label: this.props.label });
        }

        // Enter: Change query
        if (e.keyCode === 13) {
            this.props.onChange(this.props.type, this.props.index, this.refs.query.value, this.refs.label.value);
            this.setState({ isEditing: false });
            return false;
        }
    },
    render: function () {
        if (this.state.isEditing) {
            return React.createElement(
                "td",
                { className: "grid-heading editing" },
                React.createElement("span", { className: "icon-cancel icon-column-heading clickable", title: "Cancel", onClick: this.handleCancel }),
                React.createElement("span", { className: "icon-accept icon-column-heading clickable", title: "Done", onClick: this.handleAccept }),
                React.createElement(
                    "div",
                    null,
                    React.createElement("span", { className: "icon-find" }),
                    React.createElement("input", {
                        ref: "query",
                        autoFocus: true,
                        tabIndex: "2",
                        type: "text",
                        placeholder: "Query",
                        value: this.state.query,
                        onChange: this.handleQueryChange,
                        onKeyDown: this.handleKey })
                ),
                React.createElement(
                    "div",
                    null,
                    React.createElement("span", { className: "icon-rename" }),
                    React.createElement("input", {
                        ref: "label",
                        tabIndex: "3",
                        type: "text",
                        placeholder: "Label",
                        value: this.state.label,
                        onChange: this.handleLabelChange,
                        onKeyDown: this.handleKey })
                )
            );
        }

        var label = this.state.label || this.state.query;

        var rowOrColumn = this.props.type === "row" ? "Row" : "Column";
        var control;
        if (this.props.value !== "All") {
            control = React.createElement("span", { key: "removeButton", className: "icon-cancel icon-column-heading", title: "Remove " + rowOrColumn, onClick: this.handleRemove });
        } else {
            control = React.createElement("span", { ref: "addButton", className: "add-column-button icon-add icon-column-heading", title: "Add " + rowOrColumn, onClick: this.handleEdit });
        }

        return React.createElement(
            "td",
            { onClick: this.handleEdit, title: this.state.query, className: "grid-heading" },
            control,
            React.createElement(
                "div",
                null,
                label
            )
        );
    }
});

// GridValueCell renders the cells in the grid, showing their value formatted according to settings.
var GridValueCell = React.createClass({
    displayName: "GridValueCell",

    renderPie: function (percentage, emptyColor, filledColor) {
        if (!emptyColor) emptyColor = "rgba(181, 231, 247, 0.5)";
        if (!filledColor) filledColor = "rgb(61, 195, 246)";

        // For technique, see: https://www.smashingmagazine.com/2015/07/designing-simple-pie-charts-with-css/
        return React.createElement(
            "svg",
            { className: "pie", viewBox: "0 0 32 32", style: { transform: "rotate(-90deg)", background: emptyColor, borderRadius: "50%" } },
            React.createElement("circle", { r: "16", cx: "16", cy: "16", style: { fill: emptyColor, stroke: filledColor, strokeWidth: "32", strokeDasharray: (100 * percentage).toFixed(0) + " 100" } })
        );
    },
    render: function () {
        var nullCell = React.createElement(
            "td",
            { key: "R" + this.props.rowIndex + "C" + this.props.colIndex },
            React.createElement(
                "div",
                { className: "cell" },
                "-"
            )
        );
        if (!this.props.content.values) return nullCell;

        var colors = ["rgba(88, 207, 119, 0.5)", "rgba(96, 117, 194, 0.5)", "rgba(81, 164, 183, 0.5)", "rgba(255, 181, 108, 0.5)", "rgba(255, 125, 108, 0.5)", "rgba(170, 84, 191, 0.5)"];
        var darkColors = ["rgb(13, 186, 58)", "rgb(17, 42, 134)", "rgb(15, 127, 154)", "rgb(248, 132, 17)", "rgb(245, 44, 17)", "rgb(135, 16, 164)"];

        var dataBlockRowIndex = this.props.rowIndex * this.props.columnCount + this.props.colIndex;
        var dataBlockValueColumnIndex = this.props.content.values.columnCount - 1;

        // Bail out if we don't find the expected cell values
        if (this.props.content.values.rows.length <= dataBlockRowIndex || this.props.content.values.rows[dataBlockRowIndex].length <= dataBlockValueColumnIndex) {
            return nullCell;
        }

        // Get the numeric value
        var value = this.props.content.values.rows[dataBlockRowIndex][dataBlockValueColumnIndex] || 0;

        // Determine the denominator to compare against
        var denominatorRowIndex;
        if (this.props.showPortionOf === "row") {
            denominatorRowIndex = (this.props.rowIndex + 1) * this.props.columnCount - 1;
        } else if (this.props.showPortionOf === "col") {
            denominatorRowIndex = (this.props.rowCount - 1) * this.props.columnCount + this.props.colIndex;
        } else {
            denominatorRowIndex = this.props.content.values.rows.length - 1;
        }

        var denominator = this.props.content.values.rows[denominatorRowIndex][dataBlockValueColumnIndex] || 0;
        var percentage = value / denominator;
        if (denominator === 0) percentage = 0;

        // Write the value in the requested form - number with commas or percentage with three significant digits.
        var valueFormatted = (percentage * 100).toFixed(1) + "%";
        if (valueFormatted === "100.0%") valueFormatted = "100%";

        if (this.props.show === "number") {
            valueFormatted = value.toLocaleString();
        } else if (this.props.show === "both") {
            valueFormatted = React.createElement(
                "span",
                null,
                React.createElement(
                    "span",
                    null,
                    valueFormatted
                ),
                React.createElement(
                    "span",
                    { style: { float: "right" } },
                    value.toLocaleString()
                )
            );
        }

        // Compute the Query URL for this cell
        var cellQuery = this.props.content.query.where;

        for (var columnIndex = 0; columnIndex < dataBlockValueColumnIndex; ++columnIndex) {
            var filter = this.props.content.values.rows[dataBlockRowIndex][columnIndex];
            if (filter) {
                if (cellQuery) cellQuery += " AND ";
                cellQuery += filter;
            }
        }

        if (this.props.content.query.aggregator === "MIN" || this.props.content.query.aggregator === "MAX") {
            if (cellQuery) cellQuery += " AND ";
            cellQuery += this.props.content.query.aggregationColumns[0] + " = " + value.toString();
        }

        var cellUrl = buildUrlParameters({ t: this.props.content.query.tableName, q: cellQuery });

        // Choose a color for the relative value rendering
        var colorIndex = 0;
        if (this.props.showPortionOf === "row") {
            colorIndex = this.props.rowIndex % colors.length;
        } else if (this.props.showPortionOf === "col") {
            colorIndex = this.props.colIndex % colors.length;
        }

        // Write the element or style needed to show the relative value
        var portionElement = null;
        if (this.props.showPortionAs === "pie") {
            portionElement = this.renderPie(percentage, colors[colorIndex], darkColors[colorIndex]);
        }

        var portionStyle = null;
        if (this.props.showPortionAs === "bar") {
            // Target Width: 150px + (2 x 8x padding) = 166px.
            portionStyle = { boxShadow: colors[colorIndex] + " " + 166 * percentage + "px 0px 0px inset" };

            // Fills 100% of the cell width, but doesn't animate [in Chrome, 12/2016]. The animation is a higher priority until we can find a way to get both.
            //portionStyle = { background: "linear-gradient(to right, " + colors[colorIndex] + " 0, " + colors[colorIndex] + " " + (100 * percentage).toFixed(0) + "%, rgba(0, 0, 0, 0) " + (100 * percentage).toFixed(0) + "%)" };
        }

        return React.createElement(
            "td",
            { key: "R" + this.props.rowIndex + "C" + this.props.colIndex, className: "c" },
            React.createElement(
                "div",
                { style: portionStyle },
                portionElement,
                React.createElement(
                    "a",
                    { href: "/" + cellUrl },
                    valueFormatted
                )
            )
        );
    }
});

// GridMain wraps the overall grid UI
var GridMain = React.createClass({
    displayName: "GridMain",

    getInitialState: function () {
        return {
            blockingErrorTitle: null,
            blockingErrorContent: null,

            query: this.props.params.q || "",
            pivotQueries: [],
            currentTable: this.props.params.t,
            currentTableAllColumns: [],

            aggregationFunction: this.props.params.af || "COUNT",
            aggregateColumn: this.props.params.ac || "",
            rows: getParameterArrayForPrefix(this.props.params, "r"),
            rowLabels: getParameterArrayForPrefix(this.props.params, "rl"),
            cols: getParameterArrayForPrefix(this.props.params, "c"),
            colLabels: getParameterArrayForPrefix(this.props.params, "cl"),

            show: this.props.params.show || "both",
            showPortionOf: this.props.params.of || "total",
            showPortionAs: this.props.params.as || "bar",

            gridData: null
        };
    },
    getClearedUserSelections: function () {
        return {
            /* Query is kept to allow "re-exploring" the query. All other state is cleared. */

            aggregationFunction: "COUNT",
            aggregateColumn: "",
            rows: [],
            rowLabels: [],
            cols: [],
            colLabels: [],

            show: "both",
            showPortionOf: "total",
            showPortionAs: "bar",

            userSelectedTable: "",

            gridData: null
        };
    },
    componentDidMount: function () {
        if (this.props.params.p) {
            this.selectDefaultQuery(this.props.params.p);
        } else {
            this.runSearch();
        }
    },
    handleSelectDefaultQuery: function (e) {
        var name = e.target.value;
        this.selectDefaultQuery(name);
    },
    selectDefaultQuery: function (name) {
        var query = this.props.gridDefaultQueries[name];
        if (query) {
            this.setState(this.getClearedUserSelections());
            this.setState(query, this.runSearch);
        }
    },
    handleChangeAggregation: function (aggregationFunction, aggregateColumn) {
        this.setState({ aggregationFunction: aggregationFunction, aggregateColumn: aggregateColumn, userSelectedTable: this.state.currentTable }, this.runSearch);
    },
    handleChangeShow: function (e) {
        this.setState({ show: e.target.value }, this.setHistory);
    },
    handleChangeShowPortionOf: function (e) {
        this.setState({ showPortionOf: e.target.value }, this.setHistory);
    },
    handleChangeShowPortionAs: function (e) {
        this.setState({ showPortionAs: e.target.value }, this.setHistory);
    },
    handleQueryChange: function (type, index, value, label) {
        var newState = { userSelectedTable: this.state.currentTable, gridData: null, addColumn: false, addRow: false };

        // NOTE: When a column or row is changed, we lock the current table and clear the grid data.
        //  We lock the table because the rows/cols are cleared when the active table is changed and we don't want "top query" changes to lose the cols/rows you've picked
        //  We clear Grid data because after the change but before the query, the data is inconsistent with the cols/rows and we don't want React to draw it like that
        if (type === "column") {
            var columns = this.state.cols.slice();
            var colLabels = this.state.colLabels.slice();

            if (value) {
                columns[index] = value;
                colLabels[index] = label;
            } else {
                columns.splice(index, 1);
                colLabels.splice(index, 1);
            }

            newState.cols = columns;
            newState.colLabels = colLabels;
        } else if (type === "row") {
            var rows = this.state.rows.slice();
            var rowLabels = this.state.rowLabels.slice();

            if (value) {
                rows[index] = value;
                rowLabels[index] = label;
            } else {
                rows.splice(index, 1);
                rowLabels.splice(index, 1);
            }

            newState.rows = rows;
            newState.rowLabels = rowLabels;
        }

        this.setState(newState, this.runSearch);
    },
    onSelectedTableChange: function (name) {
        if (this.state.currentTable === name) {
            // If the selected table is clicked, just mark it actively selected and fix the URL
            this.setState({ userSelectedTable: name }, this.setHistory);
        } else {
            // Otherwise, clear the columns/sort/sortOrder and query the new selected table
            var cleared = this.getClearedUserSelections();
            cleared.userSelectedTable = name;
            cleared.currentTable = name;
            this.setState(cleared, this.runSearch);
        }
    },
    onSearchChange: function (e) {
        this.setState({ query: e.target.value }, this.delayedRunSearch);
    },
    delayedRunSearch: function () {
        // Only query every 250 milliseconds while typing
        if (!this.timer) {
            this.timer = window.setTimeout(this.runSearch, 250);
        }
    },
    runSearch: function () {
        this.timer = null;
        this.getAllCounts();
        this.setHistory();
    },
    getAllCounts: function () {
        // On query, ask for the count from every table.
        var params = { q: this.state.query };
        this.addPivotClauses(params);

        // Get the count of matches from each accessible table
        jsonQuery(this.props.url + "/allCount", function (data) {
            var tableToShow = this.state.userSelectedTable;
            if (!tableToShow) tableToShow = data.content[0].tableName;

            this.setState({ allCountData: data, currentTable: tableToShow, error: null }, this.getTableBasics.bind(this, this.getGrid));
        }.bind(this), function (xhr, status, err) {
            this.setState({ allCountData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });

            if (status === 500 || status === 404) {
                this.setState({ blockingErrorTitle: "Service Unavailable", blockingErrorContent: this.props.serviceUnavailableContent });
            } else {
                this.setState({ blockingErrorTitle: "Access Denied", blockingErrorContent: this.props.accessDeniedContent });
            }
            console.error(xhr.url, status, err.toString());
        }.bind(this), params);
    },
    getTableBasics: function (next) {
        // Once a table is selected, find out the columns
        jsonQuery(this.props.url + "/table/" + this.state.currentTable, function (data) {
            this.setState({
                currentTableAllColumns: data.content.columns,
                error: null
            }, next);
        }.bind(this), function (xhr, status, err) {
            this.setState({ gridData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    getGrid: function () {
        // Once the counts query runs and table basics are loaded, get a page of results

        // Get a page of matches for the given query for the desired columns and sort order, with highlighting.
        jsonQuery(this.buildQueryUrl(), function (data) {
            var state = { gridData: data, error: null };

            // If the rows or columns were expanded by the query, use the expanded values so subsequent editing works
            // NOTE: Track the dimension for rows and columns; if only columns were passed, dimensions[0] is the column.
            if (data.content && data.content.query && data.content.query.dimensions) {
                var dimensions = data.content.query.dimensions;
                var dimensionIndex = 0;

                if (this.state.rows && this.state.rows.length === 1) {
                    var lastRow = this.state.rows[0];
                    if (lastRow.indexOf(">") === lastRow.length - 1) {
                        state.rows = dimensions[dimensionIndex] ? dimensions[dimensionIndex].groupByWhere : [];
                        state.rowLabels = [];
                    }

                    dimensionIndex++;
                }

                if (this.state.cols && this.state.cols.length === 1) {
                    var lastColumn = this.state.cols[0];
                    if (lastColumn.indexOf(">") === lastColumn.length - 1) {
                        state.cols = dimensions[dimensionIndex] ? dimensions[dimensionIndex].groupByWhere : [];
                        state.colLabels = [];
                    }

                    dimensionIndex++;
                }
            }

            this.setState(state);
        }.bind(this), function (xhr, status, err) {
            this.setState({ gridData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    setHistory: function () {
        var url = this.buildThisUrl(true);
        if (url !== window.location.href) {
            history.pushState("", "", url);
        }
    },
    buildQueryUrl: function () {
        var parameters = {
            action: "aggregate",
            q: this.state.query,
            a: this.state.aggregationFunction,
            col: this.state.aggregateColumn
        };

        // Only pass defined dimensions. Pass Rows first, so draw order is correct.
        var dimension = "d";
        if (this.state.rows && this.state.rows.length > 0) {
            addArrayParameters(parameters, dimension, this.state.rows);
            dimension = String.fromCharCode(dimension.charCodeAt(0) + 1);
        }

        if (this.state.cols && this.state.cols.length > 0) {
            addArrayParameters(parameters, dimension, this.state.cols);
            dimension = String.fromCharCode(dimension.charCodeAt(0) + 1);
        }

        var queryString = buildUrlParameters(parameters);
        return this.props.url + "/table/" + this.state.currentTable + queryString;
    },
    buildThisUrl: function (includeOpen) {
        var relevantParams = {};

        if (this.state.query) relevantParams.q = this.state.query;
        if (this.state.currentTable) relevantParams.t = this.state.currentTable;

        if (this.state.aggregationFunction !== "COUNT") {
            relevantParams.af = this.state.aggregationFunction;
            relevantParams.ac = this.state.aggregateColumn;
        }

        if (this.state.show !== "both") relevantParams.show = this.state.show;
        if (this.state.showPortionOf !== "total") relevantParams.of = this.state.showPortionOf;
        if (this.state.showPortionAs !== "bar") relevantParams.as = this.state.showPortionAs;

        if (this.state.rows && this.state.rows.length > 0) addArrayParameters(relevantParams, "r", this.state.rows);
        if (this.state.cols && this.state.cols.length > 0) addArrayParameters(relevantParams, "c", this.state.cols);
        if (this.state.rowLabels && this.state.rowLabels.length > 0) addArrayParameters(relevantParams, "rl", this.state.rowLabels);
        if (this.state.colLabels && this.state.colLabels.length > 0) addArrayParameters(relevantParams, "cl", this.state.colLabels);

        return window.location.protocol + '//' + window.location.host + window.location.pathname + buildUrlParameters(relevantParams);
    },
    addPivotClauses: function (set) {
        for (var i = 0; i < this.state.pivotQueries.length; ++i) {
            set["q" + (i + 1)] = this.state.pivotQueries[i].q;
            set["t" + (i + 1)] = this.state.pivotQueries[i].t;
        }
    },
    render: function () {
        if (this.state.blockingErrorTitle) return React.createElement(ErrorPage, { title: this.state.blockingErrorTitle, message: this.state.blockingErrorContent });

        var headings = [];
        var gridRows = [];

        var mainContent = null;

        if (this.state.gridData && this.state.gridData.content) {
            var content = this.state.gridData.content;

            var rows = this.state.rows.slice();
            var rowLabels = this.state.rowLabels;
            rows.push("All");

            var columns = this.state.cols.slice();
            var colLabels = this.state.colLabels;
            columns.push("All");

            headings.push(React.createElement(GridFunctionCell, { key: "AC", aggregationFunction: this.state.aggregationFunction, aggregateColumn: this.state.aggregateColumn, allColumns: this.state.currentTableAllColumns, onChange: this.handleChangeAggregation }));
            for (var columnIndex = 0; columnIndex < columns.length; ++columnIndex) {
                headings.push(React.createElement(GridHeadingCell, { key: "HC" + columns[columnIndex], type: "column", index: columnIndex, value: columns[columnIndex], label: colLabels[columnIndex], onChange: this.handleQueryChange }));
            }

            for (var rowIndex = 0; rowIndex < rows.length; ++rowIndex) {
                var cells = [];
                cells.push(React.createElement(GridHeadingCell, { key: "HR" + rows[rowIndex], type: "row", index: rowIndex, value: rows[rowIndex], label: rowLabels[rowIndex], onChange: this.handleQueryChange }));

                for (var colIndex = 0; colIndex < columns.length; ++colIndex) {
                    cells.push(React.createElement(GridValueCell, {
                        key: "C" + colIndex + "R" + rowIndex,
                        colIndex: colIndex,
                        rowIndex: rowIndex,
                        content: content,
                        columnCount: columns.length, rowCount: rows.length,
                        show: this.state.show, showPortionAs: this.state.showPortionAs, showPortionOf: this.state.showPortionOf }));
                }

                gridRows.push(React.createElement(
                    "tr",
                    { key: "R" + rowIndex },
                    cells
                ));
            }

            var defaultQueries = [];
            defaultQueries.push(React.createElement("option", { key: "SQNone", value: "" }));

            for (var name in this.props.gridDefaultQueries) {
                defaultQueries.push(React.createElement(
                    "option",
                    { key: "SQ" + name, value: name },
                    name
                ));
            }

            mainContent = React.createElement(
                "div",
                { className: "grid" },
                React.createElement(
                    "table",
                    { className: this.state.showPortionOf },
                    React.createElement(
                        "thead",
                        null,
                        React.createElement(
                            "tr",
                            null,
                            headings
                        )
                    ),
                    React.createElement(
                        "tbody",
                        null,
                        gridRows
                    )
                ),
                React.createElement(
                    "div",
                    { className: "options" },
                    "Show\xA0",
                    React.createElement(
                        "select",
                        { value: this.state.show, onChange: this.handleChangeShow },
                        React.createElement(
                            "option",
                            { value: "number" },
                            "Number"
                        ),
                        React.createElement(
                            "option",
                            { value: "percentage" },
                            "Percentage"
                        ),
                        React.createElement(
                            "option",
                            { value: "both" },
                            "Both"
                        )
                    ),
                    "\xA0of\xA0",
                    React.createElement(
                        "select",
                        { value: this.state.showPortionOf, onChange: this.handleChangeShowPortionOf },
                        React.createElement(
                            "option",
                            { value: "total" },
                            "Total"
                        ),
                        React.createElement(
                            "option",
                            { value: "row" },
                            "Row"
                        ),
                        React.createElement(
                            "option",
                            { value: "col" },
                            "Column"
                        )
                    ),
                    "\xA0as\xA0",
                    React.createElement(
                        "select",
                        { value: this.state.showPortionAs, onChange: this.handleChangeShowPortionAs },
                        React.createElement(
                            "option",
                            { value: "bar" },
                            "Bar"
                        ),
                        React.createElement(
                            "option",
                            { value: "pie" },
                            "Pie"
                        )
                    ),
                    "\xA0\xA0\xA0\xA0 Load\xA0",
                    React.createElement(
                        "select",
                        { value: "", onChange: this.handleSelectDefaultQuery },
                        defaultQueries
                    )
                )
            );
        }

        var listingUrl = "/" + buildUrlParameters({ t: this.state.currentTable, q: this.state.query });

        return React.createElement(
            "div",
            { className: "viewport " + configuration.theme, onKeyDown: this.handleKeyDown },
            React.createElement(SearchHeader, { name: configuration.toolName,
                feedbackEmailAddresses: configuration.feedbackEmailAddresses,
                query: this.state.query,
                allColumns: this.state.currentTableAllColumns,
                onSearchChange: this.onSearchChange }),
            React.createElement(
                "div",
                { className: "middle" },
                React.createElement(
                    "div",
                    { className: "mode theme-background-dark" },
                    React.createElement(
                        "a",
                        { href: listingUrl },
                        React.createElement("i", { className: "icon-details", title: "Listing" })
                    ),
                    React.createElement(
                        "a",
                        { className: "selected" },
                        React.createElement("i", { className: "icon-view-all-albums", title: "Grid" })
                    )
                ),
                React.createElement(
                    "div",
                    { className: "center" },
                    React.createElement(QueryStats, { error: this.state.error,
                        allCountData: this.state.allCountData,
                        selectedData: this.state.gridData,
                        currentTable: this.state.currentTable,
                        onSelectedTableChange: this.onSelectedTableChange }),
                    React.createElement(
                        "div",
                        { className: "scrollable" },
                        mainContent
                    )
                )
            )
        );
    }
});

if (document.getElementById("gridContainer")) {
    var params = getQueryStringParameters();
    ReactDOM.render(React.createElement(GridMain, {
        url: configuration.url,
        gridDefaultQueries: configuration.gridDefaultQueries,
        accessDeniedContent: configuration.accessDeniedContent,
        serviceUnavailableContent: configuration.serviceUnavailableContent,
        params: params }), document.getElementById("gridContainer"));

    document.title = configuration.toolName;
}
// NOTE: Depends on configuration from zConfiguration.jsx.

// SearchMain wraps the overall search UI
var SearchMain = React.createClass({
    displayName: "SearchMain",

    getInitialState: function () {
        return {
            blockingErrorTitle: null,
            blockingErrorContent: null,

            tables: [],
            allCountData: [],
            listingData: [],
            selectedItemData: null,

            allItemCount: 0,

            page: 0,
            hasMoreData: false,

            query: this.props.params.q || "",
            pivotQueries: [],

            currentTable: this.props.params.t,
            currentTableIdColumn: "",
            currentTableAllColumns: [],
            currentListingColumns: [],
            currentSortColumn: "",
            currentSortOrder: "",

            userSelectedTable: this.props.params.t,
            userSelectedColumns: getParameterArrayForPrefix(this.props.params, "c"),
            userSelectedSortColumn: this.props.params.ob,
            userSelectedSortOrder: this.props.params.so,
            userSelectedId: this.props.params.open
        };
    },
    getClearedUserSelections: function () {
        return {
            userSelectedTable: null,
            userSelectedColumns: [],
            userSelectedSortColumn: null,
            userSelectedSortOrder: null,
            userSelectedId: null,
            pivotQueries: []
        };
    },
    componentDidMount: function () {
        // Load table details
        this.getTables(function () {
            if (this.state.query) {
                // If there's a query, run it
                this.runSearch();
            } else if (this.state.userSelectedId) {
                // If there's an item to open, open it
                this.getTableBasics();
            }
        });
    },
    handleKeyDown: function (e) {
        // Backspace: Clear state *if query empty*
        if (e.keyCode === 8) {
            if (!this.state.query) {
                var cleared = this.getClearedUserSelections();
                cleared.allCountData = [];
                cleared.listingData = [];
                cleared.selectedItemData = null;

                this.setState(cleared, this.setHistory);
            }
        }

        // ESC: Close
        if (e.keyCode === 27) {
            this.onClose();
            e.stopPropagation();
        }

        // Up/Down: Open Previous/Next
        if (e.keyCode === 40 || e.keyCode === 38) {
            var indexChange = e.keyCode === 40 ? 1 : -1;
            this.refs.list.selectByRelativeIndex(indexChange);
            e.stopPropagation();
        }
    },
    onSelectionChanged: function (e) {
        this.setState({ userSelectedId: e }, this.getDetails);
    },
    onClose: function () {
        this.setState({ userSelectedId: null }, this.setHistory);
    },
    onResort: function (sortColumn, sortOrder) {
        // If a column heading was clicked, re-query with a new sort order
        this.setState({ userSelectedTable: this.state.currentTable, userSelectedSortColumn: sortColumn, userSelectedSortOrder: sortOrder }, this.runSearch);
    },
    onAddClause: function (name, value) {
        this.setState({ query: this.state.query + " AND [" + name + "]=\"" + value + "\"" }, this.runSearch);
    },
    onSetColumns: function (columns) {
        this.setState({ userSelectedTable: this.state.currentTable, userSelectedColumns: columns }, this.runSearch);
    },
    onSelectedTableChange: function (name) {
        if (this.state.currentTable === name) {
            // If the selected table is clicked, just mark it actively selected and fix the URL
            this.setState({ userSelectedTable: name }, this.setHistory);
        } else {
            // Otherwise, clear the columns/sort/sortOrder and query the new selected table
            var cleared = this.getClearedUserSelections();
            cleared.userSelectedTable = name;
            cleared.currentTable = name;
            this.setState(cleared, this.runSearch);
        }
    },
    onPivot: function (table, baseQuery) {
        var pivots = this.state.pivotQueries;
        pivots.push({ q: this.state.query, t: this.state.currentTable });
        this.setState({ pivotQueries: pivots, query: baseQuery, currentTable: table, userSelectedTable: table }, this.runSearch);
    },
    onSearchChange: function (e) {
        this.setState({ query: e.target.value, userSelectedId: null }, this.delayedRunSearch);
    },
    delayedRunSearch: function () {
        // Only query every 250 milliseconds while typing
        if (!this.timer) {
            this.timer = window.setTimeout(this.runSearch, 250);
        }
    },
    runSearch: function () {
        this.timer = null;
        this.getAllCounts();
        this.setHistory();
    },
    getTables: function (callback) {
        // On Page load, find the list of known table names
        jsonQuery(this.props.url, function (data) {
            this.setState({ tables: data.content, error: null }, callback);
        }.bind(this), function (xhr, status, err) {
            if (status === 500 || status === 404) {
                this.setState({ blockingErrorTitle: "Service Unavailable", blockingErrorContent: this.props.serviceUnavailableContent });
            } else {
                this.setState({ blockingErrorTitle: "Access Denied", blockingErrorContent: this.props.accessDeniedContent });
            }
            console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    getAllCounts: function () {
        // On query, ask for the count from every table.

        // If there's no query, clear results and do nothing else
        if (!this.state.query) {
            var cleared = {};
            cleared.allCountData = [];
            cleared.listingData = [];
            cleared.selectedItemData = null;

            this.setState(cleared);
            return;
        }

        var params = { q: this.state.query };
        this.addPivotClauses(params);

        // Get the count of matches from each accessible table
        jsonQuery(this.props.url + "/allCount", function (data) {
            var tableToShow = this.state.userSelectedTable;
            if (!tableToShow) tableToShow = data.content[0].tableName;

            this.setState({ allCountData: data, currentTable: tableToShow, error: null }, this.getTableBasics);
        }.bind(this), function (xhr, status, err) {
            this.setState({ allCountData: [], listingData: [], selectedItemData: null, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this), params);
    },
    getTableBasics: function () {
        // Once a table is selected, find out the columns and primary key column for the table
        jsonQuery(this.props.url + "/table/" + this.state.currentTable, function (data) {
            var idColumn = "";

            // Find the ID column
            for (var j = 0; j < data.content.columns.length; ++j) {
                if (data.content.columns[j].isPrimaryKey) {
                    idColumn = data.content.columns[j].name;
                    break;
                }
            }

            // Choose columns, sort column, sort order
            var defaultsForTable = this.props.listingDefaults ? this.props.listingDefaults[this.state.currentTable] : null;
            if (!defaultsForTable) defaultsForTable = {};

            var columns = firstNonEmptyArray(this.state.userSelectedColumns, defaultsForTable.columns, [idColumn]);
            var sortColumn = this.state.userSelectedSortColumn || defaultsForTable.sortColumn || idColumn;
            var sortOrder = this.state.userSelectedSortOrder || defaultsForTable.sortOrder || "asc";

            var next = function () {
                if (this.state.query) this.getResultsPage();
                if (this.state.userSelectedId) this.getDetails();
            };

            // Set the ID column, all columns, and listing columns
            this.setState({
                currentTableIdColumn: idColumn,
                currentTableAllColumns: data.content.columns,
                currentListingColumns: columns,
                currentSortColumn: sortColumn,
                currentSortOrder: sortOrder,
                error: null
            }, next);
        }.bind(this), function (xhr, status, err) {
            this.setState({ allCountData: [], listingData: [], selectedItemData: null, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    getResultsPage: function (i) {
        // Once the counts query runs and table basics are loaded, get a page of results

        // If there's no table, id column, or query, don't do anything yet
        if (!this.state.query || !this.state.currentTable || !this.state.currentTableIdColumn) return;

        // Get enough items to fill the requested page number (rather than trying to append one page)
        if (!i) i = 0;
        var pageSize = 50 * (i + 1);

        // Get a page of matches for the given query for the desired columns and sort order, with highlighting.
        jsonQuery(this.buildQueryUrl() + "&h=%CF%80&t=" + pageSize, function (data) {
            this.setState({ listingData: data, hasMoreData: data.content.total > pageSize, page: i, error: null });
        }.bind(this), function (xhr, status, err) {
            this.setState({ allCountData: [], listingData: [], selectedItemData: null, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this));
    },
    getDetails: function () {
        // When an item is selected, get details for it

        // If there's no table or id column don't do anything yet
        if (!this.state.currentTable || !this.state.currentTableIdColumn) return;

        var detailsQuery = this.state.currentTableIdColumn + '="' + this.state.userSelectedId + '"';
        if (this.state.query) detailsQuery = detailsQuery + " AND " + this.state.query;

        var params = {
            q: detailsQuery,
            c1: "*",
            action: "select",
            h: "π",
            s: 0,
            t: 1
        };
        this.addPivotClauses(params);

        // Select all columns for the selected item, with highlighting
        jsonQuery(this.props.url + "/table/" + this.state.currentTable, function (data) {
            if (data.content.values) {
                this.setState({ selectedItemData: arribaRowToObject(data.content.values, 0) });
            } else {
                if (!this.state.query) {
                    this.setState({ selectedItemData: null, error: "Item '" + this.state.userSelectedId + "' not found." });
                } else {
                    this.setState({ selectedItemData: null, userSelectedId: null }, this.setHistory);
                }
            }
        }.bind(this), function (xhr, status, err) {
            this.setState({ allCountData: [], listingData: [], selectedItemData: null, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            console.error(xhr.url, status, err.toString());
        }.bind(this), params);

        this.setHistory();
    },
    setHistory: function () {
        var url = this.buildThisUrl(true);
        if (url !== window.location.href) {
            history.pushState("", "", url);
        }
    },
    buildQueryUrl: function () {
        var parameters = {
            action: "select",
            q: this.state.query,
            ob: this.state.currentSortColumn,
            so: this.state.currentSortOrder,
            s: 0
        };

        addArrayParameters(parameters, "c", this.state.currentListingColumns);
        this.addPivotClauses(parameters);

        var queryString = buildUrlParameters(parameters);
        return this.props.url + "/table/" + this.state.currentTable + queryString;
    },
    buildThisUrl: function (includeOpen) {
        var relevantParams = {};
        this.addPivotClauses(relevantParams);

        if (this.state.userSelectedTable) relevantParams.t = this.state.userSelectedTable;
        if (this.state.query) relevantParams.q = this.state.query;
        if (this.state.userSelectedSortColumn) relevantParams.ob = this.state.userSelectedSortColumn;
        if (this.state.userSelectedSortOrder === "desc") relevantParams.so = this.state.userSelectedSortOrder;

        for (var i = 0; i < this.state.userSelectedColumns.length; ++i) {
            relevantParams["c" + (i + 1).toString()] = this.state.userSelectedColumns[i];
        }

        if (includeOpen && this.state.userSelectedId) {
            relevantParams.open = this.state.userSelectedId;
        }

        return window.location.protocol + '//' + window.location.host + window.location.pathname + buildUrlParameters(relevantParams);
    },
    addPivotClauses: function (set) {
        for (var i = 0; i < this.state.pivotQueries.length; ++i) {
            set["q" + (i + 1)] = this.state.pivotQueries[i].q;
            set["t" + (i + 1)] = this.state.pivotQueries[i].t;
        }
    },
    render: function () {
        if (this.state.blockingErrorTitle) return React.createElement(ErrorPage, { title: this.state.blockingErrorTitle, message: this.state.blockingErrorContent });

        var detailsView = null;
        var customDetailsView = ResultDetails;
        if (this.props.customDetailsProviders) customDetailsView = this.props.customDetailsProviders[this.state.currentTable] || customDetailsView;

        detailsView = React.createElement(customDetailsView, { itemId: this.state.userSelectedId, table: this.state.currentTable, query: this.state.query, data: this.state.selectedItemData, onClose: this.onClose, onAddClause: this.onAddClause });

        var mainContent = React.createElement(SyntaxHelp, { showHelp: this.props.params.help, splashContent: configuration.splashContent });
        if (this.state.query) {
            mainContent = React.createElement(
                SplitPane,
                { split: "horizontal", minSize: "300", isFirstVisible: this.state.listingData.content, isSecondVisible: this.state.userSelectedId },
                React.createElement(
                    InfiniteScroll,
                    { page: this.state.page, hasMoreData: this.state.hasMoreData, loadMore: this.getResultsPage },
                    React.createElement(ResultListing, { ref: "list",
                        data: this.state.listingData,
                        idColumn: this.state.currentTableIdColumn,
                        allColumns: this.state.currentTableAllColumns,
                        sortColumn: this.state.currentSortColumn,
                        sortOrder: this.state.currentSortOrder,
                        selectedId: this.state.userSelectedId,
                        onResort: this.onResort,
                        onSelectionChanged: this.onSelectionChanged,
                        onSetColumns: this.onSetColumns,
                        onPivot: this.onPivot })
                ),
                React.createElement(
                    "div",
                    { className: "scrollable" },
                    detailsView
                )
            );
        }

        var queryUrl = this.buildQueryUrl();
        var baseUrl = this.buildThisUrl(false);
        var rssUrl = queryUrl + "&fmt=rss&t=100&iURL=" + encodeURIComponent(baseUrl + "&open=");
        var csvUrl = queryUrl + "&fmt=csv&t=50000";

        var gridUrl = "/Grid.html" + buildUrlParameters({ t: this.state.currentTable, q: this.state.query });
        if (!this.state.query) gridUrl = "/Grid.html?p=Scorecard";

        return React.createElement(
            "div",
            { className: "viewport " + configuration.theme, onKeyDown: this.handleKeyDown },
            React.createElement(SearchHeader, { name: configuration.toolName,
                feedbackEmailAddresses: configuration.feedbackEmailAddresses,
                query: this.state.query,
                tables: this.state.tables,
                allColumns: this.state.currentTableAllColumns,
                onSearchChange: this.onSearchChange }),
            React.createElement(
                "div",
                { className: "middle" },
                React.createElement(
                    "nav",
                    { className: "mode theme-background-dark" },
                    React.createElement(
                        "a",
                        { className: "selected" },
                        React.createElement("i", { className: "icon-details" }),
                        React.createElement(
                            "span",
                            null,
                            "Listing"
                        )
                    ),
                    React.createElement(
                        "a",
                        { href: gridUrl },
                        React.createElement("i", { className: "icon-view-all-albums" }),
                        React.createElement(
                            "span",
                            null,
                            "Grid"
                        )
                    )
                ),
                React.createElement(
                    "div",
                    { className: "center" },
                    React.createElement(QueryStats, { error: this.state.error,
                        allCountData: this.state.allCountData,
                        selectedData: this.state.listingData,
                        rssUrl: rssUrl,
                        csvUrl: csvUrl,
                        currentTable: this.state.currentTable,
                        onSelectedTableChange: this.onSelectedTableChange }),
                    mainContent
                )
            )
        );
    }
});

if (document.getElementById("searchContainer")) {
    var params = getQueryStringParameters();
    ReactDOM.render(React.createElement(SearchMain, {
        url: configuration.url,
        listingDefaults: configuration.listingDefaults,
        customDetailsProviders: configuration.customDetailsProviders,
        accessDeniedContent: configuration.accessDeniedContent,
        serviceUnavailableContent: configuration.serviceUnavailableContent,
        params: params }), document.getElementById("searchContainer"));

    document.title = configuration.toolName;
}

// Write a simple table with name and value for each property in a data set
var DetailsTable = React.createClass({
    displayName: "DetailsTable",

    onAddClause: function (e) {
        var name = e.target.getAttribute("data");
        this.props.onAddClause(name, stripHighlight(this.props.data[name]));
    },
    render: function () {
        var details = [];

        for (var name in this.props.data) {
            var commands = [];
            var rawValue = stripHighlight(this.props.data[name]);

            // Add 'Pivot to Grid' URL
            var gridUrl = "Grid.html" + buildUrlParameters({ q: this.props.query, t: this.props.table, R1: name + ">" });
            commands.push(React.createElement("a", { href: gridUrl, className: "icon-view-all-albums icon-column-heading", title: "Grid By " + name }));

            var showAddClause = this.props.onAddClause;
            if (showAddClause && rawValue.length < 100) commands.push(React.createElement("span", { className: "icon-zoom icon-column-heading", onClick: this.onAddClause, title: "Add Clause to Query", data: name }));

            var commandCell = null;
            if (commands) {
                var width = 22 * commands.length + "px";
                commandCell = React.createElement(
                    "td",
                    { style: { minWidth: width, width: width } },
                    commands
                );
            }

            details.push(React.createElement(
                "tr",
                { key: name },
                commandCell,
                React.createElement(
                    "td",
                    null,
                    name
                ),
                React.createElement(
                    "td",
                    null,
                    React.createElement("div", { className: "detailsValue", dangerouslySetInnerHTML: highlight(this.props.data[name]), title: name + ':' + rawValue })
                )
            ));
        }

        return React.createElement(
            "table",
            { className: "detailsTable" },
            React.createElement(
                "tbody",
                null,
                details
            )
        );
    }
});

// An error page to show in place of all content when access is denied or the site is down.
var ErrorPage = React.createClass({
    displayName: "ErrorPage",

    render: function () {
        return React.createElement(
            "div",
            { className: "errorPage" },
            React.createElement(
                "h1",
                null,
                this.props.title
            ),
            React.createElement(
                "article",
                null,
                React.createElement(
                    "p",
                    null,
                    this.props.message
                )
            )
        );
    }
});

//# sourceMappingURL=Search.js.map