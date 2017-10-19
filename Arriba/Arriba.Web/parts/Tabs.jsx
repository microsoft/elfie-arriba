import "./Tabs.scss";
import AnchoredPopup from "./AnchoredPopup"
import Delete from "./Delete"

export default class extends React.Component {
    constructor(props) {
        super(props);
        this.state = {};
    }
    render() {
        const parsedQuery =
            this.props.counts &&
            this.props.counts.parsedQuery;

        var tables =
            this.props.counts &&
                this.props.counts.resultsPerTable ||
            Object.keys(this.props.allBasics).length &&
                Object.map(this.props.allBasics, (k, v) => ({ tableName: k, count: v.rowCount, succeeded: true })) ||
            [{ tableName: "Loading...", succeeded: false, locked: true }]; // Solely to prevent jumpy re-layout when allBasics comes in.

        tables.forEach(t => {
            t.pinned = t.tableName === this.props.userSelectedTable;
            t.canAdminister =
                this.props.allBasics &&
                this.props.allBasics[t.tableName] &&
                this.props.allBasics[t.tableName].canAdminister;
        });

        const overflowLimit = 5;
        const tabs = tables.map(t => <span
            key={t.tableName}
            className={`tableTab ${this.props.currentTable === t.tableName ? "current" : ""} ${t.locked ? "locked" : ""}`}
            onClick={e => this.props.userSelectedTableChanged(t.tableName)}>
            {t.pinned && <img src="/icons/pinned.svg" alt="pinned" className="pinned" title="Unpin" onClick={e => {
                e.stopPropagation();
                this.props.userSelectedTableChanged(undefined);
            }} />}
            <span>{t.tableName}</span>
            <b>{t.allowedToRead === false /* no lock icon if undefined */
                ? <span className="icon-lock icon" />
                : t.succeeded ? t.count.toLocaleString() : "‒"}</b>
            {t.canAdminister && <Delete onClick={e => {
                e.stopPropagation();
                if (confirm(`Delete table "${t.tableName}"?`)) {
                    xhr(`table/${t.tableName}/delete`)
                        .then(() => this.props.refreshAllBasics(() => {
                            this.props.userSelectedTableChanged()
                        }));
                }
            }} />}
        </span>);

        // Promote pinned tab out of overflow if needed.
        const pindex = tables.findIndex(t => t.pinned);
        if (overflowLimit > 0 && pindex >= overflowLimit) {
            const pinned = tabs.splice(pindex, 1);
            tabs.splice(overflowLimit - 1, 0, pinned);
        }

        return <div className="searchBoxAndTabs">
            <div className="tableTabs">
                {tabs.slice(0, overflowLimit)}
                {tabs.length > overflowLimit &&
                    <span key="overflow" ref="overflow"
                        className="tableTab" style={{ position: "relative" }}
                        onClick={() => this.refs.popup.show()}>
                        ⋯
                        <AnchoredPopup ref="popup" className="tabs-overflow" bottom="-5px" left="0">
                            {tabs.slice(overflowLimit)}
                        </AnchoredPopup>
                    </span>
                }
                <span className="tableTabs-fill"></span>
                {parsedQuery && <a title="Explanation" href="#" onMouseOver={e => this.setState({ showExplanation: true })} onMouseOut={e => this.setState({ showExplanation: false })}>
                    <img src="/icons/info.svg" alt="rss"/>
                </a>}
                {this.props.queryUrl && <a title="RSS Link" target="_blank" href={`${this.props.queryUrl}&fmt=rss&t=100&iURL=${encodeURIComponent(this.props.thisUrl + "&open=")}`}>
                    <img src="/icons/rss.svg" alt="rss"/>
                </a>}
                {this.props.queryUrl && <a title="Download CSV" target="_blank" href={`${this.props.queryUrl}&fmt=csv&t=50000`}>
                    <img src="/icons/download.svg" alt="download"/>
                </a>}
                {this.props.query && <a title="Mail" href={
                        "mailto:?subject=" + encodeURIComponent(configuration.toolName)
                        + ": " + encodeURIComponent(this.props.query)
                        + "&body=" + encodeURIComponent(window.location.href)
                    }>
                    <img src="/icons/mail.svg" alt="mail"/>
                </a>}
            </div>
            {this.props.children}
            {this.state.showExplanation && <div className="explanation">
                {parsedQuery || "Explanation"}
            </div>}
        </div>
    }
}
