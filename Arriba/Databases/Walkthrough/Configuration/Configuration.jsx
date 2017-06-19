    import RateDetails from "./RateDetails";

    export default {
        // Arriba Service URL to query
        // Hardcode to the name of your service. Use port 42784 for HTTP, 42785 for HTTPS.
        url: "http://" + window.location.hostname + ":42784",

        // Name of tool to show [top right and elsewhere]
        toolName: "Rates",

        // E-Mail addresses to whom feedback should go
        feedbackEmailAddresses: "",

        // Listing: Default Columns and Sort Order to show per table
        listingDefaults: {
            "Rates": { columns: ["ZIP", "UtilityName", "ResidentialKwhRate"], sortColumn: "ZIP", sortOrder: "asc" }
        },

        // Listing: Default details rendering per table
        customDetailsProviders: {
            "Rates": RateDetails
        },

        // Grid: Pre-defined Grid queries
        /* gridDefault: "<NameOfDefaultPredefinedQuery>", */
        gridDefaultQueries: {
            "Clear": {},
            "Rate Comparison": {
                query: "",
                rows: ["ResidentialKwhRate < 0.16", "ResidentialKwhRate < 0.14", "ResidentialKwhRate < 0.12", "ResidentialKwhRate < 0.10", "ResidentialKwhRate < 0.09"],
                showPortionOf: "col",
                showPortionAs: "bar",
                show: "both"
            }
        },

        // Content to show before a query is typed
        startContent: {
            overview:(
                <span>
                    <b><a target="_blank" href="https://catalog.data.gov/dataset/u-s-electric-utility-companies-and-rates-look-up-by-zipcode-feb-2011-57a7c">US Electricity Rates</a></b> and <b><a target="_blank" href="https://blog.splitwise.com/2013/09/18/the-2010-us-census-population-by-zip-code-totally-free/">Population</a></b> by ZIP indexed.
                    Need more <a href="Search.html?help=true">help</a>?
                </span>
            ),
            examples: {
                "WA": <span>See rates by ZIP for Washington.</span>,
                "ResidentialKwhRate < 0.10": <span>Find ZIPs with power &lt; 0.10/KWh.</span>,
            }
        },

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
