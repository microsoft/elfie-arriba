export default class Mode extends React.Component {
    render() {
        const isPageGrid = window.location.pathname.startsWith("/Grid.html");
        const urlParameters = buildUrlParameters({ t: this.props.currentTable, q: this.props.query });
        const listingUrl = !isPageGrid ? undefined : `/${urlParameters}`;
        const gridUrl = isPageGrid ? undefined : `/Grid.html${this.props.query ? urlParameters : "?p=default"}`;

        return <div className="mode">
            <a title="Listing" href={listingUrl} className={!isPageGrid ? "selected" : undefined}><i className="icon-details"></i></a>
            <a title="Grid" href={gridUrl} className={isPageGrid ? "selected" : undefined}><i className="icon-view-all-albums"></i></a>
            <span className="mode-fill"></span>
            <a title="Feedback" href={"mailto:" + encodeURIComponent(configuration.feedbackEmailAddresses) + "?subject=" + encodeURIComponent(configuration.toolName) + " Feedback"}>
                <img src="/icons/feedback.svg" alt="feedback"/>
            </a>
            <a title="Help" href="/?help=true">
                <img src="/icons/help.svg" alt="help"/>
            </a>
        </div>
    }
}
