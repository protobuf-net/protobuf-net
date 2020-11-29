using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace ProtoBuf.BuildTools.Internal
{
    internal sealed class TypeContext
    {
        private HashSet<int>? _knownFieldNumbers;
        private HashSet<string>? _knownFieldNames;
        private List<FieldReservation>? _reservations;

        public void AddReservation(ref SyntaxNodeAnalysisContext context, in FieldReservation reservation, ISymbol? symbol)
        {
            if (OverlapsReservation(in reservation, out var existing))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: ProtobufFieldAnalyzer.DuplicateReservation,
                    location: Utils.PickLocation(ref context, symbol),
                    messageArgs: new object[] { reservation, existing },
                    additionalLocations: null,
                    properties: null
                ));
            }
            (_reservations ??= new List<FieldReservation>()).Add(reservation);
        }
        public void AddKnownField(string? name, int number)
        {
            if (name is not null)
            {
                (_knownFieldNames ??= new HashSet<string>()).Add(name);
            }
            (_knownFieldNumbers ??= new HashSet<int>()).Add(number);
        }

        public bool OverlapsField(string name)
            => _knownFieldNames is not null && _knownFieldNames.Contains(name);

        public bool OverlapsField(int number)
            => _knownFieldNumbers is not null && _knownFieldNumbers.Contains(number);

        public bool OverlapsReservation(in FieldReservation reservation, out FieldReservation overlap)
        {
            if (_reservations is not null)
            {
                foreach (var existing in _reservations)
                {
                    if (existing.Overlaps(reservation))
                    {
                        overlap = reservation;
                        return true;
                    }
                }
            }
            overlap = default;
            return false;
        }

        public bool OverlapsReservation(string name, out FieldReservation overlap)
        {
            if (_reservations is not null)
            {
                foreach (var existing in _reservations)
                {
                    if (existing.Includes(name))
                    {
                        overlap = existing;
                        return true;
                    }
                }
            }
            overlap = default;
            return false;
        }

        public bool OverlapsReservation(int number, out FieldReservation overlap)
        {
            if (_reservations is not null)
            {
                foreach (var existing in _reservations)
                {
                    if (existing.Includes(number))
                    {
                        overlap = existing;
                        return true;
                    }
                }
            }
            overlap = default;
            return false;
        }
    }
}
