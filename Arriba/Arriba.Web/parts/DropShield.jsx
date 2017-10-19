import "./DropShield.scss";
import "../js/utilities.jsx";

export default class extends React.Component {
    constructor(props) {
        super(props);
        this.state = { label: "Upload" };
    }
    cancel() {
        this.setState({ file: undefined, tablename: "", columns: undefined });
    }
    render() {
        return <div className={"dropShield" + ((this.props.dropping || this.state.file) ? " dropShield-active" : "")}
            onDragEnter={e => {
                if (this.state.file) this.cancel();
            }}
            onDragOver={e => e.preventDefault() }
            onDragLeave={e => setTimeout(() => this.props.droppingChanged(false)) }
            onDrop={e => {
                e.preventDefault();
                const limitGB = 1; // Mirrors the server side limit.
                const file = e.dataTransfer.files[0];
                const notify = label => {
                    this.setState({ label });
                    setTimeout(() => {
                        this.setState({ label: "Upload" });
                        this.props.droppingChanged(false);
                    }, 2500);
                };

                if (!file) {
                    this.props.droppingChanged(false); // Abort if user unintentionally drags an element of the page onto itself.
                } else if (!file.name.toUpperCase().endsWith(".CSV")) {
                    notify(`*.csv Required!`);
                } else if (file.size > limitGB * 1024 * 1024 * 1024) {
                    notify(`${limitGB}GB Limit!`);
                } else {
                    this.props.droppingChanged(false);
                    const tablename = file.name.replace(".csv", "");
                    this.setState({ file, tablename });
                    file.slice(0, 64 * 1024).readAsText()
                        .then(txt => xhr(`sample?type=csv`, {},  txt))
                        .then(o => this.setState({
                            columns: o.columns,
                            rowCountEst: Math.floor((file.size / Math.min(64 * 1024, file.size)) * (o.rowCount - 1))
                        }))
                        .catch(log);
                }
            }}
            >
            {this.props.dropping && <div className="dialog dialog-upload">
                <img src="/icons/upload.svg" alt="upload"/>{this.state.label}
            </div>}
            {this.state.file && <UploadConfirm
                tablename={this.state.tablename}
                rowCountEst={this.state.rowCountEst}
                file = {this.state.file}
                cancel={() => this.cancel()}
                columns={this.state.columns}

                existingTablenames={this.props.existingTablenames}
                refreshAllBasics={this.props.refreshAllBasics}
                queryChanged={this.props.queryChanged}
                getCounts={this.props.getCounts}
                columnsChanged={this.props.columnsChanged} />}
        </div>
    }
}

class UploadConfirm extends React.Component {
    constructor(props) {
        super(props);
        this.state = { tablename: this.props.tablename || "" };
    }
    render() {
        const isValid = /^[\w\.\-]+$/.test(this.state.tablename);
        const isReplacing = this.props.existingTablenames.any(s => String.ciEquals(s, this.state.tablename));

        return <div className="dialog uploadConfirm">
            <div className="title">Name</div>
            <input type="text" spellCheck="false" className={isValid ? "" : "invalid"}
                value={this.state.tablename || ""}
                onInput={e => this.setState({ tablename: e.target.value })} />
            <div className="columns">
                {this.props.rowCountEst
                    &&<div className="rowCountEst">{this.props.rowCountEst.toLocaleString()} Rows Estimated</div>
                    || false}
                {(this.props.columns || []).map(col =>
                    <div className="col">
                        <span className="key">{col.isPrimaryKey ? "✱" : ""}</span>
                        <span className="name">{col.name}</span>
                        <span className="hint">{col.type}</span>
                    </div>
                )}
            </div>
            <div className="buttons">
                <input type="button"
                    value="Cancel"
                    onClick={this.props.cancel} />
                <input type="button"
                    disabled={!isValid}
                    value={isReplacing ? "Replace ⚠" : "Upload"}
                    onClick={e => {
                        xhr(`table/${this.state.tablename}/delete`)
                            .then(() => xhr(`table`, {}, {
                                tableName: this.state.tablename,
                                itemCountLimit: this.props.rowCountEst * 2,
                                columns: this.props.columns
                            }))
                            .then(() => this.props.file.readAsText())
                            .then(txt => {
                                var loading = true;
                                const check = () => {
                                    if (loading) setTimeout(() => this.props.getCounts(check), 100);
                                }
                                this.props.refreshAllBasics(check);
                                this.props.cancel();
                                this.props.queryChanged("*");
                                this.props.columnsChanged(this.props.columns.slice(0, 4).map(col => col.name), this.state.tablename);
                                return xhr(`table/${this.state.tablename}?type=csv`, {}, txt)
                                    .then(() => loading = false)
                            });
                    }} />
            </div>
        </div>
    }
}
