import "./Index.scss"
import "./shared.jsx"
import "./polyfill.jsx"
import React from "react"
import ReactDOM from "react-dom"
import autobind from 'autobind-decorator'

const extensionsRequire = require.context('./', false, /extensions\.jsx/)
const extensions = extensionsRequire.keys().includes('./extensions.jsx') && extensionsRequire('./extensions.jsx') || {}

;(() => {
    window.log = function() { console.log.apply(console, arguments) }

    // TODO: Run on leading edge AND trailing edge of last request.
    window.debounce = function(func, wait) {
    	var timeout
    	return function() {
    		clearTimeout(timeout)
    		timeout = setTimeout(() => {
                timeout = null
    			func.apply(this, arguments)
            }, wait)
    	}
    }

    window.singleTimeout = function() {
        // Usage:
        // st = singleTimeout()   // Init
        // st(f)                  // Cancels any previous f, runs f synchonously.
        // st(f, 100)             // Cancels any previous f, runs f after 100ms delay.
        // st()                   // Cancels any previous f.

        var timerId
        return (f, delayMs) => {
            clearTimeout(timerId) // No harm if id is undef.
            timerId = undefined
            if (!f) return
            if (delayMs) {
                timerId = setTimeout(f, delayMs)
            } else {
                f()
            }
        }
    }

    Array.prototype.remove = function(item) {
        var i = this.indexOf(item);
        if (i >= 0) this.splice(i, 1);
        return this;
    };

    Array.prototype.toggle = function(item) {
        this.includes(item) ? this.remove(item) : this.push(item);
        return this;
    }

    Array.prototype.last = function() {
        return this[this.length - 1]
    }

    Date.min = function(...dates) {
        return new Date(Math.min(...dates))
    }

    Date.range = function(from, to) {
        const range = [new Date(from)]
        while (from < to) {
            from.setDate(from.getDate() + 1)
            range.push(new Date(from))
        }
        return range
    }

    Date.isDateEquals = function(a, b) {
        return a && b
            && a.getYear()  === b.getYear()
            && a.getMonth() === b.getMonth()
            && a.getDate()  === b.getDate()
    }

    Date.prototype.firstOfMonth = function() {
        const copy = new Date(this)
        copy.setDate(1)
        return copy
    }

    Date.prototype.daysAgo = function(n) {
        const copy = new Date(this)
        copy.setDate(copy.getDate() - (n || 0))
        return copy
    }

    Date.prototype.sunday = function() {
        const copy = new Date(this)
        copy.setDate(copy.getDate() - copy.getDay())
        return copy
    }

    Date.prototype.toXFormat = function() {
        // If today is selected, don't pass a dates
        if(Date.isDateEquals(this, new Date())) return "";

        const mm = this.toLocaleString('en-US', { month: '2-digit' })
        const dd = this.toLocaleString('en-US', { day: '2-digit' })
        return `${this.getFullYear()}-${mm}-${dd}`
    }

    Date.prototype.toFriendly = function() {
        const today = new Date()
        if (Date.isDateEquals(this, today)) return "Today"
        if (Date.isDateEquals(this, today.daysAgo(1))) return "Yesterday"
        if (Date.isDateEquals(this, today.daysAgo(7))) return "Last Week"
        const mm = this.toLocaleString('en-US', { month: 'short' })
        const dd = this.toLocaleString('en-US', { day: 'numeric' })
        return `${mm} ${dd}`
    }

    window.extendEditor = function(editor) {
        editor.valueUntilPosition = function() {
            const position = this.getPosition()
            return this.getModel().getValueInRange({
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: position.lineNumber,
                endColumn: position.column,
            })
        }
        editor.decorate = function(newDecorations) {
            const old = this._oldDecorations || []
            if (old.length || newDecorations.length) {
                this._oldDecorations = this.deltaDecorations(old, newDecorations)
            }
        }
        editor.indexToPosition = function(i) {
            const lines = this.getValue().slice(0, i).split('\n')
            const col = lines.last().length + 1
            return new monaco.Position(lines.length, col)
        }
        editor.append = function(value) {
            this.executeEdits('my-source', [{
                    identifier: { major: 1, minor: 1 },
                    range: monaco.Range.fromPositions(this.getModel().getFullModelRange().getEndPosition()),
                    text: `${value}`,
                    forceMoveMarkers: true,
                }])
        }
        editor.appendLine = function(line) {
            this.append(`${this.getValue().endsWith('\n') ? '' : '\n'}${line}`)
        }

        // Assumes monaco is loaded
        monaco.Position.prototype.toRange = function(length) {
            return new monaco.Range(this.lineNumber, this.column, this.lineNumber, this.column + length)
        }
    }
})()

class Resizer extends React.Component {
	@autobind onMouseDown(ev) {
		const {isHorizontal, onStart} = this.props
		this.base = onStart && onStart() || 0
		this.start = isHorizontal ? ev.clientY : ev.clientX
		addEventListener('mousemove', this.onMouseMove)
		addEventListener('mouseup', this.onMouseUp)
	}
	@autobind onMouseMove(ev) {
		const {isHorizontal, onChange} = this.props
		onChange && onChange(this.base + ((isHorizontal ? ev.clientY : ev.clientX) - this.start))
	}
	@autobind onMouseUp(ev) {
		removeEventListener('mousemove', this.onMouseMove)
		removeEventListener('mouseup', this.onMouseUp)
		this.start = 0
	}
	render() {
		return <div className="resizer"
			style={{
				position: 'absolute',
                userSelect: 'none',
                zIndex: 1,
                ...this.props.isHorizontal ? { // Sticks to the top as opposed to the bottom.
    				height: 20,
    				top: 0,
    				left: 0, right: 0,
                    marginTop: -10,
                    cursor: 'row-resize',
                } : {
    				width: 20,
    				right: 0,
    				top: 0, bottom: 0,
                    marginRight: -10,
                    cursor: 'col-resize',
                },
			}}
			onMouseDown={this.onMouseDown}></div>
	}
}

class Index extends React.Component {
    constructor(props) {
        super(props)
        this.count = this.baseCount = 50
        this.cols = this.baseCols = 20
        this.debouncedQueryChanged = debounce(this.queryChanged, 500)
        this.state = { query: this.query, userCols: [], saveAs: '', pausePulse: true }

        const params = window.getQueryStringParameters();
        const q = params["q"];
        
        const loc = document.location;
        xhr.urlRoot = loc.port === '8080' ? `${loc.protocol}//${loc.hostname}:5073` : ''

        const defaultQuery = extensions.query || [
            'read WebRequest',
            'where [HttpStatus] != "200"',
        ].join('\n')

        this.initialQuery = q ? q : defaultQuery;

        this.reqPeek = new CachableReusedRequest('run');
        this.reqPeek.caching = true;

        this.dateTimer = singleTimeout()
        this.peekTimer = singleTimeout()
    }
    componentDidMount() {
        window.require.config({ paths: { 'vs': 'node_modules/monaco-editor/min/vs' }});

    	window.require(['vs/editor/editor.main'], () => {
            monaco.languages.register({ id: 'xform' });
            monaco.languages.setMonarchTokensProvider('xform', {
                tokenizer: {
                    root: [
                        [/^\w+/, 'verb'],
                        [/\[\w*\]/, 'column'],
                        [/"\w*"/, 'string'],
                        [/#.*$/, 'comment'],
                    ]
                }
            })
            monaco.editor.defineTheme('xform', {
                base: 'vs',
                inherit: false,
                rules: [
                    // https://github.com/Microsoft/vscode/blob/bef497ff82391f4f29ea52f532d896a6903f6ff6/src/vs/editor/standalone/common/themes.ts
                    { token: 'verb',   foreground: '5c99d6' }, // hsa(210, 60%, 60%), Atom dark: 44C0C6
                    { token: 'column', foreground: '40bfbf' }, // hsl(180, 50%, 50%), Atom dark: D1BC92
                    { token: 'string', foreground: 'bf5540' }, // hsl( 10, 50%, 50%), Atom dark: FC8458
                    { token: 'comment', foreground: '8bc270' }, // hsl(100, 40%, 60%)
                ]
            })
            monaco.languages.registerCompletionItemProvider('xform', {
                provideCompletionItems: (model, position) =>
                    (this.suggestions && Promise.resolve(this.suggestions) || this.suggest).then(o => {
                        this.suggestions = undefined

                        if (!o.Values) return []

                        const textUntilPosition = this.editor.valueUntilPosition()
                        const trunate = o.ItemCategory === '[Column]' && /\[\w*$/.test(textUntilPosition)
                            || o.ItemCategory === 'CompareOperator' && /!$/.test(textUntilPosition)
                            || o.ItemCategory === 'CompareOperator' && /\|$/.test(textUntilPosition)

                        const kind = monaco.languages.CompletionItemKind;
                        return !o.Values.length ? [] : o.Values.split(";").map(s => ({
                            kind: {
                                verb: kind.Keyword,
                                compareOperator: kind.Keyword,
                                columnName: kind.Field,
                            }[o.ItemCategory] || kind.Text,
                            label: s,
                            insertText: trunate ? s.slice(1) : s,
                        }))
                    })
            })

    		this.editor = monaco.editor.create(document.getElementById('queryEditor'), {
    			value: this.initialQuery,
    			language: 'xform',
                scrollBeyondLastLine: false,
                minimap: { enabled: false },
                automaticLayout: true,
                theme: 'xform',
                overviewRulerBorder: false,
                occurrencesHighlight: false,
                hideCursorInOverviewRuler: true,
                fixedOverflowWidgets: true,
    		});
            extendEditor(this.editor)

            this.editor.onDidChangeModelContent(this.queryTextChanged.bind(this))
            this.editor.onDidChangeCursorPosition(e => {
                if (this.textJustChanged) this.queryAndCursorChanged()
                this.textJustChanged = false
            })

            this.validQuery = this.query
            this.queryChanged()
    	});
    }
    componentDidUpdate(prevProps, prevState) {
        if (prevState.peek !== this.state.peek) {
            const params = this.state.peek
                ? { asof: this.state.asOf, q: `${this.query}\npeek [${this.state.peek.name}]` }
                : undefined
            this.reqPeek.update(params, json => {
                this.setState({ peekData: json && json.rows.sort((a, b) => b[1] - a[1]) && json.rows.slice(0, 7) })
            })
        }
    }
    get suggest() {
        return xhr(`suggest`, { asof: this.state.asOf, q: this.editor.valueUntilPosition() })
    }
    get query() {
        return this.editor?.getValue()
    }
    queryTextChanged(force) {
        this.textJustChanged = true
        const trimmedQuery = this.query.trim() // Pre async capture
        xhr(`suggest`, { asof: this.state.asOf, q: this.query }).then(info => {
            if (info.Valid && (force || this.validQuery !== trimmedQuery)) {
                this.validQuery = trimmedQuery
                this.debouncedQueryChanged()
            }

            const errorMessage = info.ErrorMessage
            if (errorMessage !== this.state.errorMessage) this.setState({ errorMessage })

            const usage = info.Usage
            if (usage !== this.state.usage) this.setState({ usage })

            const queryHint = !info.InvalidToken && info.ItemCategory || ''
            if (queryHint != this.state.queryHint) this.setState({ queryHint })

            this.editor.decorate(info.ErrorMessage // Need to verify info.InvalidTokenIndex < this.query.length?
                ? [{
                    range: this.editor.indexToPosition(info.InvalidTokenIndex).toRange(info.InvalidToken.length),
                    options: { inlineClassName: 'validationError' },
                }]
                : [])
        })
        setTimeout(() => {
            const ia = document.querySelector('.inputarea').style
            const qh = document.querySelector('.queryHint').style
            qh.top = parseInt(ia.top) + 1 + 'px'
            qh.left = ia.left
        })
    }
    queryAndCursorChanged() {
        const q = this.editor.valueUntilPosition()
        this.suggest.then(suggestions => {
            if (suggestions.Values && (suggestions.InvalidTokenIndex < q.length || /[\s\(]$/.test(q))) {
                this.suggestions = suggestions
                this.editor.trigger('source', 'editor.action.triggerSuggest', {});
            }
        })
    }
    queryChanged() {
        this.count = this.baseCount
        this.cols = this.baseCols

        if(!!this.validQuery) this.setState({ loading: true, pausePulse: true })

        xhr(`run`, { asof: this.state.asOf, q: `${this.validQuery}\nschema` }).then(o => {
            const schemaBody = (o.rows || []).map(r => ({ name: r[0], type: `${r[1]}` }))
            const colNames = new Set(schemaBody.map(r => r.name))
            this.setState({
                schemaBody,
                userCols: this.state.userCols.filter(c => colNames.has(c)),
            })
            this.limitChanged(0, 0, true)
        })
    }
    limitChanged(addCount = 0, addCols = 0, firstRun) { // firstRun... of the this specific query
        this.count += addCount
        this.cols += addCols
        const q = this.validQuery

        const userCols = this.state.userCols.length && `\nselect ${this.state.userCols.map(c => `[${c}]`).join(', ')}` || ''
        this.setState({ loading: true, pausePulse: firstRun })
        xhr(`run`, { rowLimit: this.count, colLimit: this.cols, asof: this.state.asOf, q: `${q}${userCols}` }).then(o => {
            if (o.Valid === false) {
                this.setState({
                    results: [],
                    resultCount: undefined,
                    loading: false,
                    pausePulse: false,
                })
                return
            }
            if (o.Message || o.ErrorMessage) throw 'Error should have been caught before run.'
            if (firstRun) {
                this.setState({ results: o })
                xhr(`count`, { asof: this.state.asOf, q: this.validQuery }).then(o => {
                    this.setState({
                        resultCount: typeof o.Count === "number" && `${o.Count.toLocaleString()} Results (${o.RuntimeMs} ms)`,
                        loading: false,
                        pausePulse: false,
                    })
                })
            } else {
                this.setState({ results: o, loading: false })
            }
        })
    }
    _makeSvg(list) {
        if (!list.length) return false;

        // Generates a SVG histogram to be displayed behind the completion list.
        // The path goes counter-clockwise starting from the top-right.
        var d = '';

        // The inst() currently concats SVG commands to the list 'd'.
        // However when debugging, it is useful to redirect the ...params to the console.
        const inst = (...params) => d += params.join(" ") + " ";

        // Scrape ___% from the item.hint. If not found, default to 0.
        const values = list.map(item => new Number(item[2].replace('%', '')) + 0 || 0);

        const w = 80; // Matches CSS declared width.
        inst("M", w, 0);
        inst("L", w - values[0] * 0.75, 0);
        const max = Math.max(...values) || 1; // Prevent divide by zero.
        var y = 0; // Running total fo the height.
        values.forEach(val => {
            const x = w - (val/max) * w;
            inst("S", x, y, ",", x, y + 17); // Half of the CSS height.
            y += 34; // Matches the CSS declared height of each row.
        });
        const x = w - values[values.length - 1] * 0.75;
        inst("S", x, y, x, y + 18);
        inst("L", w, y);
        inst("Z");
        return <svg><path id="p" d={d} /></svg>
    }
    render() {
        var cols, rows
        const results = this.state.results
        if (results) {
            cols = results.cols
            rows = results.rows
        }

        const Peek = () => {
            if (!this.state.peek || !this.state.peek.tr || !this.state.peekData) return null
            const rect = this.state.peek.tr.getBoundingClientRect()
            return <div className="peek" style={{ left: `${rect.x + rect.width - 5}px`, top: `${rect.y}px` }}
                onMouseEnter={e => this.peekTimer()}
                onMouseLeave={e => this.peekTimer(() => this.setState({ peek: undefined }), 100)}>
                {this._makeSvg(this.state.peekData)}
                {this.state.peekData.map((row, i) => <div key={i} className="peek-value"
                    onClick={e => {
                        const expr = `[${this.state.peek.name}] = "${row[0]}"`
                        const last = this.query.split('\n').last()
                        if (last.startsWith('where')) {
                            this.editor.append(` AND ${expr}`)
                        } else {
                            this.editor.appendLine(`where ${expr}`)
                        }
                        this.peekTimer(() => this.setState({ peek: undefined }))
                    }}>
                    <span>{row[0] === '' ? '—' : row[0] }</span>
                    <span>{row[2]}</span>
                </div>)}
            </div>
        }

        const DatePicker = () => {
            if (!this.state.showDatePicker) return null
            const today = new Date()
            const startDate = Date.min(today.firstOfMonth(), today.daysAgo(7)).sunday()
            return <div className="datePicker"
                onMouseEnter={e => this.dateTimer()}
                onMouseLeave={e => this.dateTimer(() => this.setState({ showDatePicker: undefined }), 100)}>
                <div>{new Date().toLocaleString('en-US', { month: 'long' })}</div>
                <div>
                    {'SMTWTFS'.split('').map((d, i)=> <span key={`header${i}`} className="header">{d}</span>)}
                    {Date.range(startDate, today).map(d => <span key={d}
                        className={o2c({
                            current: Date.isDateEquals(this.state.asOfDate || new Date(), d),
                            outside: today.getMonth() !== d.getMonth(),
                        })}
                        onClick={() => {
                            this.setState({ asOf: d.toXFormat(), asOfDate: d }, () => this.queryTextChanged(true))
                            this.dateTimer(() => this.setState({ showDatePicker: undefined }))
                        }}>
                        <span>{d.getDate()}</span>
                    </span>)}
                </div>
            </div>
        }

        const encodedParams = encodeParams({ asof: this.state.asOf, q: this.validQuery })

        const rowsSample = (rows || []).slice(0, 3)
        const columnMeta = (cols || []).map((col, i) => {
            const defaultFormat = { test: /.?/                  , formatter: cell => cell                                             }
            const formats = [
                { test: /^file:\/\//                            , formatter: cell => <a href={cell.replace("file:///", "vscode://file/")}>{cell}</a>        },
                { test: /^\w{1,10}:\/\//                        , formatter: cell => <a href={cell} target="_blank">{cell}</a>        }, // Must go after file:// link conversion
                { test: /^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}Z/ , formatter: cell => cell && (new Date(cell)).toLocaleString() || '—' }, // Must go before parseInt.
                { test: /^-?\d+$/                               , formatter: cell => cell && (+cell).toLocaleString()          || '—' }, // Make sure exclude semVers such as 1.0.0
                defaultFormat,
            ]
            
            const inferredFormat = formats.find(({ test, formatter }) => {
                return rowsSample.length && rowsSample.every(row => !row[i] || `${row[i]}`.match(test))
            })
            const formatter = (inferredFormat || defaultFormat).formatter
            
            const firstCell = rows && rows[0] && rows[0][i]
            const formattedFirstCell = formatter(firstCell)
            const firstCellLength = `${typeof formattedFirstCell === 'string' ? formattedFirstCell : firstCell}`.length || 0
            const headerOrFirstWidth = Math.max(Math.ceil(col.length * 6.5), Math.ceil(firstCellLength * 6.8)) + (i === 0 ? 20 : 30)
            return {
                width: Math.min(400, headerOrFirstWidth), // Limit default column width to 400.
                formatter,
            }
        })
        
        const everyFive = function*(list) {
            for (let i = 0; i < list.length; i += 5) {
                yield list.slice(i, i + 5)
            }
        }

        return [<div key="root" className={`root`}>
            <div className="query" style={{ width: this.state.queryPaneWidth }}>
                <div className="queryHeader">
                    <input type="text" placeholder="Save As"
                        value={this.state.saveAs} onChange={e => this.setState({ saveAs: e.target.value })}/>
                    <span className="save" style={{ opacity: +!!this.state.saveAs }} onClick={e => {
                        const q = this.query
                        const name = this.state.saveAs
                        if (!name || !q) return
                        xhr(`save`, { name, q }).then(o => {
                            this.setState({ saving: "Saved" })
                            setTimeout(() => this.setState({ saving: "Save" }), 3000)
                        })
                    }}>{ this.state.saving || "Save" }</span>
                    <span title={(this.state.asOfDate || new Date()).toDateString()}
                        className={'button' + (this.state.showDatePicker ? ' hot' : '')}
                        onMouseEnter={e => this.dateTimer(() => this.setState({ showDatePicker: true }))}
                        onMouseLeave={e => this.dateTimer(() => this.setState({ showDatePicker: undefined }), 100)}>
                        As of {(this.state.asOfDate || new Date()).toFriendly()}</span>
                </div>
                <div className="queryUsage">{
                    this.state.errorMessage && <span className="errorMessage">{this.state.errorMessage}</span>
                    || this.state.usage || `\u200B`
                }</div>
                <div ref="queryEditor" id="queryEditor" style={{ height: this.state.queryPaneHeight }}>
                    <div className="queryHint">{this.state.queryHint}</div>
                </div>
                <DatePicker key="datePicker" />
                <Resizer
                    onStart={() => this.refs.queryEditor.offsetWidth}
                    onChange={i => this.setState({ queryPaneWidth: Math.max(300, i) })}/>
                <div className="schemaHeader" style={{ position: 'relative' }}>{/* Relative for Resizer */}
                    {!this.state.userCols.length && this.state.schemaBody && <span>{this.state.schemaBody.length} Columns</span>}
                    {!!this.state.userCols.length && <span className="button" onClick={e => this.setState({ userCols: [] }, () => this.limitChanged())}>Reset</span>}
                    <span className="flexFill"></span>
                    {!!this.state.userCols.length && <span className="button" onClick={e => {
                        this.editor.appendLine(`select ${this.state.userCols.map(c => `[${c}]`).join(', ')}`)
                        this.setState({ userCols: [] }, () => this.limitChanged())
                    }}>Apply</span>}
                    <Resizer isHorizontal
                        onStart={() => this.refs.queryEditor.offsetHeight}
                        onChange={i => this.setState({ queryPaneHeight: Math.max(100, i) })}/>
                </div>
                {this.state.schemaBody && <div className="tableWrapper">
                    <div className="schemaColumns">
                        {[...everyFive(this.state.schemaBody)].map((five, ii) => <div className="groupOfFive" key={ii}>
                            {five.map((r, i) => <div key={i}
                                ref={tr => r.tr = tr}
                                onMouseEnter={e => this.peekTimer(() => this.setState({ peek: r }))}
                                onMouseLeave={e => this.peekTimer(() => this.setState({ peek: undefined }), 100)}>
                                <label><input type="checkbox" checked={this.state.userCols.includes(r.name)} onChange={e => {
                                    this.setState({ userCols: [...this.state.userCols].toggle(r.name) }, () => this.limitChanged())
                                }}/>{r.name}</label>
                            </div>)}
                        </div>)}
                    </div>
                </div>}
            </div>
            <div id="results">
                <div className="" className={`resultsHeader ${this.state.pausePulse ? '' : 'pulse'}`}>
                    <span>{this.state.resultCount}</span>
                    <span className="flexFill"></span>
                    {encodedParams && <a className="button segoe-icon-link" alt="Link" href={`${xhr.urlRoot}/?${encodedParams}`}></a>}
                    {encodedParams && <a className="button" target="_blank" href={`${xhr.urlRoot}/download?fmt=csv&${encodedParams}`}>CSV</a>}
                    {extensions.links && extensions.links(encodedParams)}
                    {encodedParams && <a className="button" target="_blank" href={`${xhr.urlRoot}/download?fmt=tsv&${encodedParams}`}>TSV</a>}
                    <span className={`loading ${ this.state.loading && 'loading-active' }`}></span>
                </div>
                <div className="tableWrapper" onScroll={e => {
                        const element = e.target
                        const pixelsFromLimitX = (element.scrollWidth - element.clientWidth - element.scrollLeft)
                        const pixelsFromLimitY = (element.scrollHeight - element.clientHeight - element.scrollTop)
                        if (pixelsFromLimitX < 20 && this.cols < this.state.schemaBody.length ) this.limitChanged(0, 10)
                        if (pixelsFromLimitY < 100) this.limitChanged(50)
                    }}>
                    <table>
                        <thead>
                            <tr>
                                {cols?.map((c, i) => <td ref={`col${c}`} key={c} style={{ minWidth: columnMeta[i].width }}>
                                    <span>{c}</span>
                                    <Resizer
                                        onStart={() => this.refs[`col${c}`].offsetWidth}
                                        onChange={i => this.refs[`col${c}`].style.minWidth = `${i}px`}/>
                                </td>)}
                            </tr>
                        </thead>
                        <tbody>
                            {rows?.map((r, i) => <tr key={i}>{r.map((c, ii) => <td key={i + "x" + ii}>
                                <span>{columnMeta[ii].formatter(c)}</span>
                            </td>)}</tr>)}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>,
        <Peek key="peek" />]
    }
}

ReactDOM.render(<Index />, document.getElementById("app"))
