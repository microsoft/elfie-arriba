import DetailsField from "../parts/DetailsField";
import DetailsTable from "../parts/DetailsTable";

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
                            <DetailsField value={this.props.data["State"]} columnName="State" inline="true" /> / {this.props.itemId} / {(+this.props.data["Population"]).toLocaleString()} people
                        </div>
                    </div>

                    <div className="detailBody">
                        <div className="box" style={{marginTop: "8px"}}>
                            <DetailsField value={this.props.data["UtilityName"]} columnName="UtilityName" />
                            <div className="indent">Residential: {"$" + (+this.props.data["ResidentialKwhRate"]).toFixed(2)} / KWH</div>
                            <div className="indent">Commercial: {"$" + (+this.props.data["CommercialKwhRate"]).toFixed(2)} / KWH</div>
                            <div className="indent">Industrial: {"$" + (+this.props.data["IndustrialKwhRate"]).toFixed(2)} / KWH</div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }
});
