    import WorkItemDetails from "../parts.optional/WorkItemDetails/WorkItemDetails";

    export default {
        // Arriba Service URL to query
        // Hardcode to the name of your service. Use port 42784 for HTTP, 42785 for HTTPS.
        url: "http://" + window.location.hostname + ":42784",

        // Name of tool to show [top right and elsewhere]
        toolName: "Bung",

        // Theme to use [see theme.css for options and to define others]
        theme: "theme-teal",

        // E-Mail addresses to whom feedback should go
        feedbackEmailAddresses: "scottl@designlaboratory.onmicrosoft.com",

        // Listing: Default Columns and Sort Order to show per table
        listingDefaults: {
             "WDGIS": { columns: ["ID", "Title", "Assigned To"], sortColumn: "ID", sortOrder: "desc" }
        },

        // Listing: Default details rendering per table
        customDetailsProviders: {
            "WDGIS": WorkItemDetails
        },

        databaseName : "WDGIS",
        directLinkUrl : "https://wdginformationsecurity.visualstudio.com/WDIGS/_workitems/edit/",

        // Grid: Pre-defined Grid queries
        /* gridDefault: "<NameOfDefaultPredefinedQuery>", */
        gridDefaultQueries: {
            "Clear": {},
            "Conflux Tasks By Owner": {
                query: "Conflux AND [Remaining Work] > 0",
                aggregationFunction: "SUM",
                aggregateColumn: "Remaining Work",
                currentTable: "WDGIS",
                rows: ["Assigned To>"],
                showPortionOf: "row"
            },
        },

        // Content to show before a query is typed
        startContent: {
            overview:(
                <span>
                    All WDGIS work items indexed.
                    Need more <a href="Search.html?help=true">help</a>?
                </span>
            ),
            examples: {
                "[Assigned To] = @Me": <span>My Items</span>,
                "Conflux AND [Remaining Work] > 0": <span>Conflux Remaining Tasks</span>
            }
        },

        // Content to show if query fails due to access denied (401)
        accessDeniedContent: (
            <span>
                Access to WGIS Bung denied. Join <a target="_blank" href="https://idweb/IdentityManagement/aspx/common/GlobalSearchResult.aspx?searchtype=e0c132db-08d8-4258-8bce-561687a8a51e&content=ConfluxDev">ConfluxDev</a>.
            </span>
        ),

        // Content to show if query fails because service is unavailable (404, 500)
        serviceUnavailableContent: (
            <span>
                WDGIS Bung is currently unavailable.
            </span>
        )
    };
