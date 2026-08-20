; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
; Release tracking IS now enforced: Microsoft.CodeAnalysis.Analyzers is referenced and RS2000/RS2001/
; RS2002 are escalated to errors, so an id that is not listed here is a build break rather than a
; convention someone has to remember. Verified by deleting an entry and watching the build fail.
; AGENTS.md still carries the *ownership* table, which this file structurally cannot express: it maps
; id -> category/severity/title, never which type declares it - and the PBN40xx block has two owners.
