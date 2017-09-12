import "./QueryStats.scss";

// QueryStats is a bar just under the SearchHeader. It shows errors, if any, or the current query, returned count, and runtime.
export default React.createClass({
    render: function () {
        if (this.props.error) {
            // If the query didn't return, show an error
            return <div className="queryStats"><span className="error-icon icon" /><span className="error">{this.props.error}</span></div>;
        }

        // Write details for selected table
        var selectedDetails = [];
        if (this.props.selectedData) {
            var selectedContent = this.props.selectedData;

            // Only show if the ResultListing isn't alredy showing the error.
            if (selectedContent.values && selectedContent.details.errors) {
                selectedDetails.push(<span>&nbsp;<span className="error-icon icon" /><b>{selectedContent.details.errors}</b></span>);
            }

            if (selectedContent.details.warnings) {
                selectedDetails.push(<span>&nbsp;<span className="icon-warning icon" /><b>{selectedContent.details.warnings}</b></span>);
            }

            if (selectedContent.details.accessDeniedColumns) {
                var deniedColumnList = "Denied Columns: ";
                for (var i = 0; i < selectedContent.details.accessDeniedColumns.length; ++i) {
                    if (i > 0) deniedColumnList += ", ";
                    deniedColumnList += selectedContent.details.accessDeniedColumns[i];
                }

                selectedDetails.push(<span>&nbsp;<span className="icon-lock icon" title={deniedColumnList} /></span>);
            }
        }

        return !!selectedDetails.length && <div className="queryStats">{selectedDetails}</div>;
    }
});
