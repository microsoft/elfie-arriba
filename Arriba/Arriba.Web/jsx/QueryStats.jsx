// QueryStats is a bar just under the SearchHeader. It shows errors, if any, or the current query, returned count, and runtime.
export default React.createClass({
    onTableTileClick: function (name, e) {
        this.props.onSelectedTableChange(name);
    },
    render: function () {
        if (this.props.error) {
            // If the query didn't return, show an error
            return <div className="queryStats"><span className="error-icon icon" /><span className="error">{this.props.error}</span></div>;
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
                    parts.push(<span className="lock-icon icon" />);
                } else if (!tableResult.succeeded) {
                    parts.push(<span className="error-icon icon" />);
                } else {
                    parts.push(<span className="countValue">{tableResult.count.toLocaleString()}</span>);
                }

                tiles.push(
                    <span key={"tableTile_" + tableResult.tableName} className={cssClasses + " clickable"} onClick={this.onTableTileClick.bind(this, tableResult.tableName)}>
                        {parts}
                        <span>{tableResult.tableName}</span>
                    </span>
                );
            }

            // Write details for selected table
            var selectedDetails = [];
            if(this.props.selectedData && this.props.selectedData.content) {
                var selectedContent = this.props.selectedData.content;

                if(selectedContent.query.where) {
                    selectedDetails.push(<span>for <span className="explanation">{selectedContent.query.where}</span></span>);
                }

                if (selectedContent.details.errors) {
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

                if (this.props.rssUrl && selectedContent.details.succeeded) {
                    selectedDetails.push(
                        <a title="RSS Link" target="_blank" href={this.props.rssUrl}>
                            <img className="rss" src="icons/feed-icon-14x14.png" style={{ position: "relative", top: "1px" }} alt="RSS" />
                        </a>
                    );
                }

                if (this.props.csvUrl && selectedContent.details.succeeded) {
                    selectedDetails.push(
                        <a title="Download CSV" target="_blank" href={this.props.csvUrl}>
                            <span className="icon-download" />
                        </a>
                    );
                }
            }
        }

        return (
            <div className="queryStats">
                <span className="statsForTable">{tiles}</span>
                {selectedDetails}
            </div>
        );
    }
});