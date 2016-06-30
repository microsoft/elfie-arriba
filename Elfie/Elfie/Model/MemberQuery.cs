// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Index;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    /// <summary>
    ///  MemberQuery is used to query for specific members across any IMemberDatabase.
    ///  It can be used to do searches, Add Reference, Go To Definition, or IntelliSense
    ///  enabling queries.
    /// </summary>
    public class MemberQuery
    {
        public SymbolType Type;
        public SymbolModifier Modifiers;
        public bool IsFullNamespace;
        public bool IsFullSuffix;
        public bool IgnoreCase;

        internal String8 SymbolName8;
        internal String8 Parameters8;

        internal String8 SymbolNameSuffix;
        internal String8Set SplitSymbolName8;
        internal Range[] SymbolNamePrefixIdentifiers;
        internal Range SymbolNameSuffixIdentifiers;
        internal Range ParametersIdentifiers;

        internal StringStore LastResolvedStore;
        internal bool LastResolveResult;

        public MemberQuery(string symbolName, bool isFullSuffix, bool isFullNamespace)
        {
            this.SymbolName = symbolName;
            this.IsFullSuffix = isFullSuffix;
            this.IsFullNamespace = isFullNamespace;

            this.IgnoreCase = true;
        }

        public MemberQuery(string symbolName, string parameters, bool isFullSuffix, bool isFullNamespace) : this(symbolName, isFullSuffix, isFullNamespace)
        {
            this.Parameters = parameters;
        }

        public string SymbolName
        {
            get { return SymbolName8.ToString(); }
            set
            {
                this.SymbolName8 = String8.Convert(value, new byte[String8.GetLength(value)]);
                this.SplitSymbolName8 = String8Set.Split(this.SymbolName8, '.', new int[String8Set.GetLength(this.SymbolName8, '.')]);

                if (this.SplitSymbolName8.Count == 0)
                {
                    this.SymbolNamePrefixIdentifiers = EmptyArray<Range>.Instance;
                    this.SymbolNameSuffix = String8.Empty;
                }
                else
                {
                    this.SymbolNamePrefixIdentifiers = new Range[this.SplitSymbolName8.Count - 1];
                    this.SymbolNameSuffix = this.SplitSymbolName8[this.SplitSymbolName8.Count - 1];
                }

                // Trigger re-search for identifier for value
                ForceReresolve();
            }
        }

        public string Parameters
        {
            get { return Parameters8.ToString(); }
            set
            {
                this.Parameters8 = String8.Convert(value, new byte[String8.GetLength(value)]);

                // Trigger re-search for identifier for value
                ForceReresolve();
            }
        }

        public void ForceReresolve()
        {
            this.LastResolvedStore = null;
        }

        internal bool ResolveStringsTo(StringStore store)
        {
            // If we've already resolved to this store, don't re-resolve
            if (store == this.LastResolvedStore) return this.LastResolveResult;

            // Record we last resolved against this store and failed
            this.LastResolvedStore = store;
            this.LastResolveResult = false;

            // Look up symbol name *prefix* parts (all exact)
            for (int i = 0; i < this.SplitSymbolName8.Count - 1; ++i)
            {
                if (!store.TryFindString(this.SplitSymbolName8[i], this.IgnoreCase, out this.SymbolNamePrefixIdentifiers[i])) return false;
            }

            // Look up symbol name suffix (exact only if IsFullSuffix)
            if (this.IsFullSuffix)
            {
                if (!store.TryFindString(this.SymbolNameSuffix, this.IgnoreCase, out this.SymbolNameSuffixIdentifiers)) return false;
            }
            else
            {
                if (this.SymbolNameSuffix.IsEmpty())
                {
                    this.SymbolNameSuffixIdentifiers = Range.Max;
                }
                else
                {
                    if (!store.TryGetRangeStartingWith(this.SymbolNameSuffix, out this.SymbolNameSuffixIdentifiers)) return false;

                    // NOTE: Can't make a prefix Range case sensitive, so have to validate casing later
                    // Case-insensitive sort means you can have [..., array, Array, arrayList, ArrayList, ...], so no way to return case sensitive range starting with 'Array'
                }
            }

            // Look up parameters [and get the copy from the StringStore for fast comparison on signature]
            if (!store.TryFindString(this.Parameters8, this.IgnoreCase, out this.ParametersIdentifiers)) return false;
            this.Parameters8 = store[this.ParametersIdentifiers.Start];

            // If we found everything, record we succeeded
            this.LastResolveResult = true;

            return true;
        }

        /// <summary>
        ///  Search the given IMemberDatabase for matches to this query, up to the given
        ///  result count limit.
        /// </summary>
        /// <param name="db">Database to search</param>
        /// <param name="maxResultCount">Maximum count of results to return</param>
        /// <returns>Array of Symbol for each result</returns>
        public Symbol[] FindMembers(IMemberDatabase db, int maxResultCount)
        {
            PartialArray<Symbol> results = new PartialArray<Symbol>(maxResultCount);
            if (!TryFindMembers(db, ref results))
            {
                return EmptyArray<Symbol>.Instance;
            }

            return results.ToArray();
        }

        /// <summary>
        ///  Search the given IMemberDatabase for matches to this query and put
        ///  results into the results array provided. The capacity of the results
        ///  array determines how many results are returned.
        /// </summary>
        /// <param name="db">Database to search</param>
        /// <param name="results">PartialArray to contain results, sized for the count desired.</param>
        /// <returns>True if results were added, False otherwise</returns>
        public bool TryFindMembers(IMemberDatabase db, ref PartialArray<Symbol> results)
        {
            // Ensure strings must be found again so that benchmarks are realistic
            ForceReresolve();

            // Clear results from a previous query
            results.Clear();

            // If there was no query, return with no results
            if (String.IsNullOrEmpty(SymbolName)) return false;

            // Get required members from database
            StringStore strings = db.StringStore;
            ItemTree declaredMembers = db.DeclaredMembers;
            MemberIndex index = db.Index;

            // Map strings to the local StringStore. Stop immediately if any values aren't found.
            if (!ResolveStringsTo(strings)) return false;

            // Cache whether this query needs details to match
            bool usesDetails = !this.Parameters8.IsEmpty() || this.Type != SymbolType.Any || this.Modifiers != SymbolModifier.None;

            int[] matches;
            int matchesIndex, matchesCount;

            if (SplitSymbolName8.Count == 1)
            {
                // Find the set of symbols with names in range. If no symbols in index, return nothing
                if (!index.TryGetMatchesInRange(SymbolNameSuffixIdentifiers, out matches, out matchesIndex, out matchesCount)) return false;

                // If there was just one name part searched for, all matches count
                for (int i = matchesIndex; i < matchesIndex + matchesCount; ++i)
                {
                    if ((usesDetails ? MatchesDetailed(declaredMembers, strings, db, matches[i]) : Matches(declaredMembers, strings, matches[i])))
                    {
                        results.Add(new Symbol(db, matches[i]));
                        if (results.IsFull) return true;
                    }
                }
            }
            else
            {
                // Find all entries with exactly the second-to-last name
                if (!index.TryGetMatchesInRange(SymbolNamePrefixIdentifiers[SymbolNamePrefixIdentifiers.Length - 1], out matches, out matchesIndex, out matchesCount)) return false;

                for (int i = matchesIndex; i < matchesIndex + matchesCount; ++i)
                {
                    int currentMatchIndex = matches[i];

                    // First, do all previous name parts in the query match?
                    int currentAncestorIndex = currentMatchIndex;
                    int namePartIndex = SymbolNamePrefixIdentifiers.Length - 2;
                    for (; namePartIndex >= 0; --namePartIndex)
                    {
                        currentAncestorIndex = declaredMembers.GetParent(currentAncestorIndex);
                        int currentAncestorNameIdentifier = declaredMembers.GetNameIdentifier(currentAncestorIndex);
                        if (!SymbolNamePrefixIdentifiers[namePartIndex].Contains(currentAncestorNameIdentifier)) break;
                    }

                    if (namePartIndex != -1) continue;

                    // If this was a full match, are we out of namespaces?
                    if (IsFullNamespace)
                    {
                        currentAncestorIndex = declaredMembers.GetParent(currentAncestorIndex);
                        SymbolType symbolAboveFullNameType = db.GetMemberType(currentAncestorIndex);
                        if (!symbolAboveFullNameType.IsAboveNamespace()) return false;
                    }

                    // Next, find children of this item which match the last part typed
                    int leafId = declaredMembers.GetFirstChild(currentMatchIndex);
                    while (leafId > 0)
                    {
                        if ((usesDetails ? MatchesDetailed(declaredMembers, strings, db, leafId) : Matches(declaredMembers, strings, leafId)))
                        {
                            results.Add(new Symbol(db, leafId));
                            if (results.IsFull) return true;
                        }

                        leafId = declaredMembers.GetNextSibling(leafId);
                    }
                }
            }

            return results.Count > 0;
        }

        private bool Matches(ItemTree declaredMembers, StringStore strings, int symbolIndex)
        {
            int candidateNameIdentifier = declaredMembers.GetNameIdentifier(symbolIndex);
            if (!SymbolNameSuffixIdentifiers.Contains(candidateNameIdentifier)) return false;

            if (this.IgnoreCase == true)
            {
                return true;
            }
            else
            {
                String8 candidateName8 = strings[candidateNameIdentifier];
                if (this.IsFullSuffix)
                {
                    return this.SymbolNameSuffix.CompareTo(candidateName8, false) == 0;
                }
                else
                {
                    return this.SymbolNameSuffix.CompareAsPrefixTo(candidateName8, false) == 0;
                }
            }
        }

        private bool MatchesDetailed(ItemTree declaredMembers, StringStore strings, IMemberDatabase db, int symbolIndex)
        {
            if (!Matches(declaredMembers, strings, symbolIndex)) return false;

            if (Type != SymbolType.Any && db.GetMemberType(symbolIndex) != Type) return false;
            if (!Modifiers.Matches(db.GetMemberModifiers(symbolIndex))) return false;

            // ISSUE: Need a way to specify you want the empty params overload of a method (other than full match)
            // NOTE: Parameters8 was a copy gotten from StringStore to make this comparison fast (rather than a byte-by-byte comparison)
            // NOTE: Case insensitive comparison because StringStore lookup was case-insensitive, so Parameters8 casing isn't specific
            // NOTE: Need String8 rather than just checking Range.Contains because IMemberDatabase doesn't offer returning the identifier
            if ((IsFullSuffix || !this.ParametersIdentifiers.IsEmpty()) && db.GetMemberParameters(symbolIndex).CompareTo(Parameters8, true) != 0) return false;

            return true;
        }
    }
}
