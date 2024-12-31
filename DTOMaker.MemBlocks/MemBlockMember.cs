﻿using DTOMaker.Gentime;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace DTOMaker.MemBlocks
{
    internal sealed class MemBlockMember : TargetMember
    {
        public MemBlockMember(TargetEntity entity, string name, Location location) : base(entity, name, location) { }

        public LayoutMethod LayoutMethod => (Entity as MemBlockEntity)?.LayoutMethod ?? LayoutMethod.Undefined;

        // todo? remove these flags if not used
        public bool HasOffsetAttribute { get; set; }
        public int StringLength { get; set; }
        public int ArrayCapacity { get; set; }
        public int FieldOffset { get; set; }
        public int FieldLength { get; set; }
        public int TotalLength => MemberIsArray ? FieldLength * ArrayCapacity : FieldLength;
        public bool IsBigEndian { get; set; } = false;

        private SyntaxDiagnostic? CheckMemberType()
        {
            return MemberTypeName switch
            {
                "Boolean" => null,
                "SByte" => null,
                "Byte" => null,
                "Int16" => null,
                "UInt16" => null,
                "Char" => null,
                "Int32" => null,
                "UInt32" => null,
                "Int64" => null,
                "UInt64" => null,
                "Half" => null,
                "Single" => null,
                "Double" => null,
                "Int128" => null,
                "UInt128" => null,
                "Decimal" => null,
                "Guid" => null,
                "String" => null,
                _ => new SyntaxDiagnostic(
                    DiagnosticId.DMMB0007, "Unsupported member datatype", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                    $"MemberType '{MemberTypeName}' not supported")
            };
        }

        private SyntaxDiagnostic? CheckMemberIsNotNullable()
        {
            if (!MemberIsNullable) return null;

            return new SyntaxDiagnostic(
                        DiagnosticId.DMMB0007, "Unsupported member type", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"Nullable type '{MemberTypeName}?' is not supported.");
        }

        private SyntaxDiagnostic? CheckHasOffsetAttribute()
        {
            if (LayoutMethod == LayoutMethod.Linear)
                return null;

            if (HasOffsetAttribute) return null;

            return (SyntaxDiagnostic?)new SyntaxDiagnostic(
                     DiagnosticId.DMMB0006, "Missing [Offset] attribute", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                     "[Offset] attribute is missing.");
        }

        private SyntaxDiagnostic? CheckFieldOffsetIsValid()
        {
            return FieldOffset switch
            {
                >= 0 => null,
                _ => new SyntaxDiagnostic(
                        DiagnosticId.DMMB0002, "Invalid field offset", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"FieldOffset ({FieldOffset}) must be >= 0")
            };
        }

        private static bool IsPowerOf2(int value, int minimum = 1, int maximum = 1024)
        {
            if (value < minimum) return false;
            if (value > maximum) return false;
            int comparand = 1;
            while (true)
            {
                if (comparand > maximum) return false;
                if (comparand >= minimum)
                {
                    // compare
                    if (value == comparand) return true;
                }
                comparand = comparand * 2;
            }
        }

        private SyntaxDiagnostic? CheckStringLengthIsValid()
        {
            if (MemberTypeName != "String") return null;
            if (IsPowerOf2(StringLength, 1, 1024)) return null;
            return new SyntaxDiagnostic(
                        DiagnosticId.DMMB0009, "Invalid string length", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"StringLength ({StringLength}) is invalid. StringLength must be a whole power of 2 between 1 and 1024.");
        }

        private SyntaxDiagnostic? CheckArrayCapacityIsValid()
        {
            if (!MemberIsArray) return null;
            if (IsPowerOf2(ArrayCapacity, 1, 1024)) return null;
            return new SyntaxDiagnostic(
                        DiagnosticId.DMMB0009, "Invalid array capacity", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"ArrayCapacity ({ArrayCapacity}) is invalid. ArrayCapacity must be a whole power of 2 between 1 and 1024.");
        }

        private SyntaxDiagnostic? CheckFieldLengthIsValid()
        {
            if (IsPowerOf2(FieldLength, 1, 1024)) return null;
            return new SyntaxDiagnostic(
                        DiagnosticId.DMMB0003, "Invalid field length", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"FieldLength ({FieldLength}) is invalid. FieldLength must be a whole power of 2 between 1 and 1024.");
        }

        private SyntaxDiagnostic? CheckTotalLengthIsValid()
        {
            if (!MemberIsArray) return null;
            int totalLength = FieldLength * ArrayCapacity;
            if (IsPowerOf2(totalLength, 1, 1024)) return null;
            return new SyntaxDiagnostic(
                        DiagnosticId.DMMB0009, "Invalid total length", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"Total length ({totalLength}) is invalid. Total length must be a whole power of 2 between 1 and 1024.");
        }

        protected override IEnumerable<SyntaxDiagnostic> OnGetValidationDiagnostics()
        {
            foreach (var diagnostic1 in base.OnGetValidationDiagnostics())
            {
                yield return diagnostic1;
            }

            SyntaxDiagnostic? diagnostic2;
            if ((diagnostic2 = CheckMemberType()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckMemberIsNotNullable()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckHasOffsetAttribute()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckFieldOffsetIsValid()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckFieldLengthIsValid()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckStringLengthIsValid()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckArrayCapacityIsValid()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckTotalLengthIsValid()) is not null) yield return diagnostic2;
        }


    }
}
