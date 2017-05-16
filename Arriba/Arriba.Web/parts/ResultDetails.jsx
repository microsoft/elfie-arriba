import DetailsTable from "./DetailsTable";

// ResultDetails shows (default formatted) details for an item when it was clicked.
export default React.createClass({
    handleClose: function () {
        this.props.onClose();
    },
    render: function () {
        if (!this.props.itemId) return null;
        if (!this.props.data) return null;

        return (
            <div className="details">
                <div className="commands">
                    <span className="icon icon-cancel" onClick={this.handleClose} />
                </div>
                <div>
                    <div className="detailHeader">
                        <div className="detailTitle">
                            {this.props.itemId}
                        </div>
                    </div>

                    <div className="detailBody">
                        <div className="box">
                            <DetailsTable 
                                data={this.props.data} 
                                table={this.props.table}
                                query={this.props.query}
                                onAddClause={this.props.onAddClause
                            } />
                        </div>
                    </div>
                </div>
            </div>
        );
    }
});