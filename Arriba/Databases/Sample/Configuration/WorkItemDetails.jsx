import LinkSet from "LinkSet";
import WorkItemHistory from "WorkItemHistory";

import RelativeDateTime from "../jsx/RelativeDateTime";
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

        var description = null;
        if(this.props.data["Description"]) description = <div className="box" dangerouslySetInnerHTML={highlight(this.props.data["Description"])}></div>;

        var reproSteps = null;
        if(this.props.data["Repro Steps"]) reproSteps = <div className="box" dangerouslySetInnerHTML={highlight(this.props.data["Repro Steps"])}></div>;

        return (
            <div className="details">
                <div className="commands">
                    <span className="icon icon-cancel" onClick={this.handleClose} />
                </div>
                <div className="detailHeader">
                    <div className="detailTitle font-light">{this.props.itemId} - <DetailsField value={this.props.data["Title"]} columnName="Title" inline="true" /></div>
                    
                    <div className="overviewBoxes">
                        <div className="box">
                            <DetailsField value={this.props.data["State"]} columnName="State" label="Currently" bold="true" />
                            <div className="indent">
                                <DetailsField value={this.props.data["Assigned To"]} columnName="Assigned To" label="Assigned to" bold="true" url="mailto:{Value}" />
                                <DetailsField value={this.props.data["Area Path"]} columnName="Area Path" label="Area " bold="true" />
                                <DetailsField value={this.props.data["Iteration Path"]} columnName="Iteration Path" label="Iteration " bold="true" />
                            </div>

                            <br />                            
                            <RelativeDateTime value={this.props.data["Created Date"]} columnName="Created Date" bold="true" />
                            <div className="indent">
                                <DetailsField value={this.props.data["Created By"]} columnName="Created By" label="Created by" bold="true" url="mailto:{Value}" />
                                <div>as a <b>{"P" + this.props.data["Priority"]} <DetailsField value={this.props.data["Work Item Type"]} columnName="Work Item Type" inline="true" /></b></div>
                            </div>
                        </div>

                        <div className="box">
                            <div><b>Direct Link</b></div>
                            <div className="indent">
                                <a href={configuration.directLinkUrl + this.props.itemId} target="_blank">{configuration.databaseName + "." + this.props.itemId}</a>
                            </div>

                            <LinkSet label="Links" data={this.props.data["Links"]} />
                            
                            <LinkSet label="Attachments" data={this.props.data["Attachments"]} />
                        </div>
                    </div>
                    {description}
                    {reproSteps}
                    <WorkItemHistory data={this.props.data["FullHistory"]} />
                    <div className="box">
                        {allTableToggle}
                        {detailsTable}
                    </div>
                </div>
            </div>
        );
    }
});

