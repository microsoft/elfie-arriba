import DetailsField from "../jsx/DetailsField";
import DetailsTable from "../jsx/DetailsTable";

// WorkItemDetails shows details for a TFS Work Item
export default React.createClass({
    getInitialState: function() {
        return { allTableShown: true };
    },
    handleClose: function () {
        this.props.onClose();
    },
    toggle: function() {
        this.setState({ allTableShown: !this.state.allTableShown });
    },
    render: function () {
        if (!this.props.itemId) return null;
        if (!this.props.data) return null;

        var allTableToggle = <span className="icon-chevron-down-bold clickable" onClick={this.toggle} />;
        var detailsTable = null;

        if (this.state.allTableShown) {
            allTableToggle = <span className="icon-chevron-up-bold clickable" onClick={this.toggle} />;
            detailsTable = (<DetailsTable 
                                data={this.props.data} 
                                table={this.props.table}
                                query={this.props.query}
                                onAddClause={this.props.onAddClause}
            />);
        }

        return (
            <div className="details">
                <div className="commands">
                    <span className="icon icon-cancel" onClick={this.handleClose} />
                </div>
                <div className="detailHeader">
                    <div className="detailTitle font-light">{this.props.itemId}</div>
                    <DetailsField value={this.props.data["Title"]} columnName="Title" inline="true" />
                    <div className="overviewBoxes">
                        <div className="box">
                            Asset
                            <div className="indent">
                                <DetailsField value={this.props.data["Assigned To"]} columnName="Assigned To" label="Assigned To" />
                            </div>
                            <br />

                            Ownership
                            <div className="indent">
                            </div>
                        </div>

                        <div className="box">
                            Status
                            <div className="indent">
                            </div>
                        </div>
                    </div>
                    <div className="box">
                        {allTableToggle}
                        {detailsTable}
                    </div>
                </div>
            </div>
        );
    }
});

