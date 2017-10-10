// DefaultConfiguration.jsx contains default configuration settings for this site instance.
// Add a copy in configuration\configuration.jsx to override these settings.

    //import <CustomDetailsView> from "./<FileNameWithoutExtension>";

    export default {
        // Arriba Service URL to query
        // Hardcode to the name of your service. Use port 42784 for HTTP, 42785 for HTTPS.
        url: "http://" + window.location.hostname + ":42784",

        // Name of tool to show [top right and elsewhere]
        toolName: "Arriba",

        // E-Mail addresses to whom feedback should go
        feedbackEmailAddresses: "",

        // Listing: Default Columns and Sort Order to show per table
        listingDefaults: {
            /* "<TableName>": { columns: ["<Col1>", "Col2", "..."], sortColumn: "<SortColumnName>", sortOrder: "asc" } */
        },

        // Listing: Default details rendering per table
        customDetailsProviders: {
            /* "<TableName>": <ReactClassNameUnquoted> */
        },

        // Grid: Pre-defined Grid queries
        /* gridDefault: "<NameOfDefaultPredefinedQuery>", */
        gridDefaultQueries: {
            "Clear": {},
            /*"<PredefinedQueryName>": {
                query: "<OverallBaseQuery>",
                currentTable: "<TableToQuery>",
                aggregationFunction: "COUNT", // [*COUNT*|SUM|MIN|MAX],
                aggregateColumn: "<ColumnToAggregateOn>",
                rows: ["<FirstRowQuery>", "<SecondRowQuery>", "..."],
                rowLabels: ["<FirstRowLabel>", "<SecondRowLabel>", "..."],
                cols: ["<FirstColumnQuery>", "<SecondColumnQuery>", "..."],
                colLabels: ["<FirstColumnLabel>", "<SecondColumnLabel>", "..."],
                showPortionOf: "col" // [col|row|*total*], what the background bars should be showing percentages of.
                showPortionAs: "bar" // [*bar*|pie], which visualization to show in each grid cell.
                show: "both" // [number|percentage|*both*], what value(s) to show in each grid cell.
            },*/
        },

        // Content to show before a query is typed
        startContent: {
            overview:(
                <span>
                    Arriba is a data exploration, free text search, and structured search engine.
                    Need more <a href="/help">help</a>?
                </span>
            ),
            /*examples: {
                "<Query>": <span>[Explain Sample Query]</span>,
                ...
            }*/
        },

        // Content to show if query fails due to access denied (401)
        accessDeniedContent: (
            <span>
                Access to Arriba Search denied.
            </span>
        ),

        // Content to show if query fails because service is unavailable (404, 500)
        serviceUnavailableContent: (
            <span>
                Arriba Search is currently unavailable.
            </span>
        )
    };
