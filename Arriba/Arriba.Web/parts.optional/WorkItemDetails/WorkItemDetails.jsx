import LinkSet from "./LinkSet";
import WorkItemHistory from "./WorkItemHistory";
import MailToLink from "./MailToLink";

import RelativeDateTime from "../../parts/RelativeDateTime";
import DetailsField from "../../parts/DetailsField";
import DetailsTable from "../../parts/DetailsTable";

// WorkItemDetails shows details for a TFS Work Item
export default class extends React.Component {
    constructor(props) {
        super(props);
        this.state = { allTableShown: true };
    }
    render() {
        if (!this.props.itemId) return null;
        if (!this.props.data) return null;

        var allTableToggle = <span className="icon-chevron-down-bold clickable" onClick={() => this.setState({ allTableShown: !this.state.allTableShown })} />;
        var detailsTable = null;

        if (this.state.allTableShown) {
            allTableToggle = <span className="icon-chevron-up-bold clickable" onClick={() => this.setState({ allTableShown: !this.state.allTableShown })} />;
            detailsTable = (<DetailsTable
                                data={this.props.data}
                                table={this.props.table}
                                query={this.props.query}
                                onAddClause={this.props.onAddClause}
                                showCommands="false"
                                excludeColumns={["Attachments", "Description", "FullHistory", "History", "Links", "Title"]}
            />);
        }

        var description = null;
        if(this.props.data["Description"]) description = <div className="box" dangerouslySetInnerHTML={highlightHtml(this.props.data["Description"])}></div>;

        var reproSteps = null;
        if(this.props.data["Repro Steps"]) reproSteps = <div className="box" dangerouslySetInnerHTML={highlightHtml(this.props.data["Repro Steps"])}></div>;

        return (
            <div className="details">
                <div className="commands">
                    <span className="icon icon-cancel" onClick={this.props.onClose} />
                </div>
                <div className="detailHeader">
                    <div className="detailTitle font-light">{this.props.itemId} - <DetailsField value={this.props.data["Title"]} columnName="Title" inline="true" /></div>

                    <div className="overviewBoxes">
                        <div className="box">
                            <DetailsField value={this.props.data["State"]} columnName="State" label="Currently" bold="true" />
                            <div className="indent">
                                <MailToLink value={this.props.data["Assigned To"]} columnName="Assigned To" table={this.props.table} itemId={this.props.itemId} />
                                <DetailsField value={this.props.data["Area Path"]} columnName="Area Path" label="Area " bold="true" />
                                <DetailsField value={this.props.data["Iteration Path"]} columnName="Iteration Path" label="Iteration " bold="true" />
                            </div>

                            <br />
                            <RelativeDateTime value={this.props.data["Created Date"]} columnName="Created Date" bold="true" />
                            <div className="indent">
                                <MailToLink value={this.props.data["Created By"]} columnName="Created By" table={this.props.table} itemId={this.props.itemId} />
                                <div>as a <b>{"P" + this.props.data["Priority"]} <DetailsField value={this.props.data["Work Item Type"]} columnName="Work Item Type" inline="true" /></b></div>
                            </div>
                        </div>

                        <div className="box">
                            <div className="linkset">
                                <div className="title">Direct Link</div>
                                <div className="indent">
                                    <a href={configuration.directLinkUrl + this.props.itemId} target="_blank">{configuration.databaseName + "." + this.props.itemId}</a>
                                </div>
                            </div>

                            <LinkSet label="Links" data={this.props.data["Links"]} table={this.props.table} />

                            <LinkSet label="Attachments" data={this.props.data["Attachments"]} />
                        </div>
                    </div>
                    {description}
                    {reproSteps}
                    <WorkItemHistory data={this.props.data["FullHistory"]} table={this.props.table} itemId={this.props.itemId} />
                    <div className="box">
                        {allTableToggle}
                        {detailsTable}
                    </div>
                </div>
            </div>
        );
    }
}
