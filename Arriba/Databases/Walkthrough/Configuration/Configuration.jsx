// DefaultConfiguration.jsx contains default configuration settings for this site instance.
// Add a copy in configuration\configuration.jsx to override these settings.

    export default {
        // Arriba Service URL to query
        // Hardcode to the name of your service. Use port 42784 for HTTP, 42785 for HTTPS.
        url: "http://" + window.location.hostname + ":42784",

        // Name of tool to show [top right and elsewhere]
        toolName: "Rates",

        // Theme to use [see theme.css for options and to define others]
        theme: "theme-orange",

        // E-Mail addresses to whom feedback should go
        feedbackEmailAddresses: "",

        // Listing: Default Columns and Sort Order to show per table
        listingDefaults: {
            "Rates": { columns: ["zip", "utility_name", "res_rate"], sortColumn: "zip", sortOrder: "asc" }
        },

        // Listing: Default details rendering per table
        customDetailsProviders: {
            /* "<TableName>": <ReactClassNameUnquoted> */
        },

        // Grid: Pre-defined Grid queries
        /* gridDefault: "<NameOfDefaultPredefinedQuery>", */
        gridDefaultQueries: {
            "Clear": {},
            "Rate Comparison": {
                query: "",
                rows: ["ind_rate < 0.15", "ind_rate < 0.10", "ind_rate < 0.08", "ind_rate < 0.07", "ind_rate < 0.06", "ind_rate < 0.05"],
                showPortionOf: "col",
                showPortionAs: "bar",
                show: "both"
            }
        },

        // Content to show before a query is typed
        splashContent: (
            <div>
                Rates is an Arriba Walkthrough showing US Electric Utility Rate Data from <a href="https://catalog.data.gov/dataset/u-s-electric-utility-companies-and-rates-look-up-by-zipcode-feb-2011-57a7c">Data.gov</a>.
            </div>
        ),

        // Content to show if query fails due to access denied (401)
        accessDeniedContent: (
            <span>
                Access to Rates denied.
            </span>
        ),

        // Content to show if query fails because service is unavailable (404, 500)
        serviceUnavailableContent: (
            <span>
                Rates is currently unavailable.
            </span>
        )
    };
