import "./Help.scss";

export default class extends React.Component {
    render() {
        return <div className="helpContent">
            <div className="features">
                <h2>Features</h2>
                <div> - Click items to see details.</div>
                <div> - Click column headings to sort.</div>
                <div> - Click <span className="icon-add" /> to add columns to listing, <span className="icon-cancel" /> to remove them.</div>
                <div> - Click <img src="/icons/download.svg" alt="Download" /> to download CSV of listing (same columns, query, and order), up to 50k rows.</div>
                <div> - Click <img src="/icons/rss.svg" alt="RSS" /> for URL to subscribe to query RSS.</div>
            </div>
            <div className="syntaxIntro">
                <h2>Syntax</h2>
                <table className="legacyTable syntaxTable">
                    <thead>
                        <tr>
                            <th style={{ width: "25%" }}>Rule</th>
                            <th style={{ width: "25%" }}>Example</th>
                            <th style={{ width: "50%" }}>Meaning</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>Type anything to search across all columns.</td>
                            <td className="font-example">halo Pris</td>
                            <td>Find items with words starting with &quot;halo&quot; and &quot;Pris&quot; anywhere, case insensitive.</td>
                        </tr>
                        <tr>
                            <td>Use &lt;column&gt; &lt;operator&gt; &lt;value&gt; to search one column.</td>
                            <td className="font-example">Team=Central AccessLevel &gt; 3</td>
                            <td>Team equals &quot;Central&quot; (case sensitive) and AccessLevel is over 3.</td>
                        </tr>
                        <tr>
                            <td>Use &apos;AND&apos;, &apos;OR&apos;, &apos;NOT&apos;, and parens for subexpressions.</td>
                            <td className="font-example">NOT Team=Central AND (AccessLevel &gt; 3 OR Role=Administrator)</td>
                            <td>Team is not Central and (AccessLevel is over 3 or Role is Administrator.</td>
                        </tr>
                        <tr>
                            <td>Use any operators from Web, SQL, or C# syntax.</td>
                            <td>
                                <table className="legacyTable syntaxTable">
                                    <tbody>
                                        <tr>
                                            <td>&amp;&amp;</td>
                                            <td>&amp;</td>
                                            <td>AND</td>
                                            <td>aNd</td>
                                        </tr>
                                        <tr>
                                            <td>||</td>
                                            <td>|</td>
                                            <td>OR</td>
                                            <td>Or</td>
                                        </tr>
                                        <tr>
                                            <td>!</td>
                                            <td>-</td>
                                            <td>NOT</td>
                                            <td>noT</td>
                                        </tr>
                                        <tr>
                                            <td>=</td>
                                            <td>==</td>
                                            <td></td>
                                            <td></td>
                                        </tr>
                                        <tr>
                                            <td>&lt;&gt;</td>
                                            <td>!=</td>
                                            <td></td>
                                            <td></td>
                                        </tr>
                                        <tr>
                                            <td>|&gt;</td>
                                            <td>STARTSWITH</td>
                                            <td>UNDER</td>
                                            <td></td>
                                        </tr>
                                        <tr>
                                            <td>:</td>
                                            <td>MATCH</td>
                                            <td>FREETEXT</td>
                                            <td>CONTAINS</td>
                                        </tr>
                                        <tr>
                                            <td>::</td>
                                            <td>MATCHEXACT</td>
                                            <td></td>
                                            <td></td>
                                        </tr>
                                    </tbody>
                                </table>
                            </td>
                            <td></td>
                        </tr>
                        <tr>
                            <td>Use &apos;:&apos; for &quot;has a word starting with&quot; or &quot;::&quot; for &quot;has the exact word&quot;</td>
                            <td className="font-example">Team:Centr && Name::Will</td>
                            <td>Team contains words starting with &quot;Centr&quot; and Name has the full word &quot;Will&quot; (&quot;William&quot; would not match) (case insensitive).</td>
                        </tr>
                        <tr>
                            <td>Use &apos;|&gt;&apos; for starts with.</td>
                            <td className="font-example">Team |&gt; Centr</td>
                            <td>Team starts with &quot;Centr&quot; (&quot;Grand Central&quot; would not match) (case sensitive).</td>
                        </tr>
                        <tr>
                            <td>Use &quot;&quot; to look for empty values.</td>
                            <td className="font-example">Team=&quot;&quot;</td>
                            <td>Team is empty.</td>
                        </tr>
                        <tr>
                            <td>Use &quot;today-n&quot; (no spaces) for relative dates.</td>
                            <td className="font-example">HireDate &lt; today-60</td>
                            <td>HireDate is more than 60 days ago [UTC].</td>
                        </tr>
                        <tr>
                            <td>Use any .NET DateTime.Parse-able formats.</td>
                            <td className="font-example">HireDate &gt; &quot;2016-10-01 10:00AM&quot;</td>
                            <td>HireDate is after Oct 1, 2016 10:00 AM [UTC].</td>
                        </tr>
                        <tr>
                            <td>Wrap column names with braces and values with quotes if they contain spaces. Escape braces and quotes by doubling them.</td>
                            <td className="font-example">[Owner [Ops]]]=&quot;Bilbo &quot;&quot;Ringbearer&quot;&quot; Baggins&quot;</td>
                            <td>The &#123;Owner [Ops]&#125; column equals &#123;Bilbo &quot;Ringbearer&quot; Baggins&#125;.</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div className="syntaxExamples">
                <h2>Examples</h2>
                <div className="exampleBox">
                    <div className="font-example">Team=&quot;Central&quot; AND IP:10.194</div>
                    <div>Find items where Team equals &quot;Central&quot;, the IP address starts with &quot;10.194&quot;.</div>
                    <div className="indent">
                        The word index operators (&apos;:&apos;, &apos;::&apos;) are not case sensitive, but all other operators (&apos;=&apos;, &apos;!=&apos;, &apos;|&gt;&apos;) are case sensitive.<br />
                        The word index operators only match from word boundaries, so &quot;Team:ent&quot; will not match a Team of &quot;Central&quot;.<br />
                        Text is split into alphanumeric words and dotted phrases. (&quot;10.194.155.11&quot; splits to &quot;10&quot;, &quot;194&quot;, &quot;155&quot;, &quot;11&quot;, &quot;10.194.155.11&quot;)<br />
                        Therefore, &quot;IP:10.194&quot; will match &quot;10.194.155.11&quot; but not &quot;11.10.194.155&quot;.<br />
                    </div>
                </div>

                <div className="exampleBox">
                    <div className="font-example">[Team]=&quot;Central&quot; && (HireDate &lt; today-60 || IsManager == 1)</div>
                    <div>Find items with Team &quot;Central&quot; which were hired in the last 60 days or are managers.</div>
                    <div className="indent">
                        &quot;Today&quot; is midnight UTC, so &quot;today-1&quot; will match 11:59p the day before yesterday (UTC).<br />
                        DateTimes in queries are interpreted at UTC.<br />
                    </div>
                </div>
            </div>
        </div>;
    }
}
