﻿using DTOMaker.Gentime;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace DTOMaker.MemBlocks
{
    internal sealed class MemBlockEntity : TargetEntity
    {
        public bool HasEntityLayoutAttribute { get; set; }
        public LayoutMethod LayoutMethod { get; set; }
        public int BlockLength { get; set; }
        public string EntityId { get; set; } = "_undefined_entity_id_";

        public MemBlockEntity(TargetDomain domain, string nameSpace, string name, Location location) : base(domain, nameSpace, name, location) { }

        private SyntaxDiagnostic? CheckHasEntityLayoutAttribute()
        {
            return !HasEntityLayoutAttribute
                ? new SyntaxDiagnostic(
                        DiagnosticId.DMMB0005, "Missing [EntityLayout] attribute", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"[EntityLayout] attribute is missing.")
                : null;
        }

        private SyntaxDiagnostic? CheckBlockSizeIsValid()
        {
            if (!HasEntityLayoutAttribute)
                return null;

            if (LayoutMethod != LayoutMethod.Explicit)
                return null;

            return BlockLength switch
            {
                1 => null,
                2 => null,
                4 => null,
                8 => null,
                16 => null,
                32 => null,
                64 => null,
                128 => null,
                256 => null,
                512 => null,
                1024 => null,
                _ => new SyntaxDiagnostic(
                        DiagnosticId.DMMB0001, "Invalid block length", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"BlockLength ({BlockLength}) is invalid. BlockLength must be a whole power of 2 between 1 and 1024.")
            };
        }

        private SyntaxDiagnostic? CheckLayoutMethodIsSupported()
        {
            if (!HasEntityLayoutAttribute)
                return null;

            return LayoutMethod switch
            {
                LayoutMethod.Explicit => null,
                LayoutMethod.Linear => null,
                LayoutMethod.Undefined => new SyntaxDiagnostic(
                        DiagnosticId.DMMB0004, "Invalid layout method", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"LayoutMethod is not defined."),
                _ => new SyntaxDiagnostic(
                        DiagnosticId.DMMB0004, "Invalid layout method", DiagnosticCategory.Design, Location, DiagnosticSeverity.Error,
                        $"LayoutMethod ({LayoutMethod}) is not supported.")
            };
        }

        private SyntaxDiagnostic? CheckMemberLayoutHasNoOverlaps()
        {
            // memory map of every byte in the entity block
            int[] memberMap = new int[BlockLength];

            if (LayoutMethod == LayoutMethod.Undefined) return null;

            foreach (var member in Members.Values.OrderBy(m => m.Sequence).OfType<MemBlockMember>())
            {
                if (member.FieldOffset < 0)
                {
                    return new SyntaxDiagnostic(
                        DiagnosticId.DMMB0008, "Member layout issue", DiagnosticCategory.Design, member.Location, DiagnosticSeverity.Error,
                        $"This member extends before the start of the block.");
                }

                if (member.FieldOffset + member.TotalLength > BlockLength)
                {
                    return new SyntaxDiagnostic(
                        DiagnosticId.DMMB0008, "Member layout issue", DiagnosticCategory.Design, member.Location, DiagnosticSeverity.Error,
                        $"This member extends beyond the end of the block.");
                }

                if (member.TotalLength > 0 && (member.FieldOffset % member.TotalLength != 0))
                {
                    return new SyntaxDiagnostic(
                        DiagnosticId.DMMB0008, "Member layout issue", DiagnosticCategory.Design, member.Location, DiagnosticSeverity.Error,
                        $"This member is incorrectly aligned. FieldOffset ({member.FieldOffset}) modulo total length ({member.TotalLength}) must be 0.");
                }

                // check value bytes layout
                for (var i = 0; i < member.TotalLength; i++)
                {
                    int offset = member.FieldOffset + i;
                    if (memberMap[offset] != 0)
                    {
                        int conflictSequence = memberMap[offset];
                        return new SyntaxDiagnostic(
                            DiagnosticId.DMMB0008, "Member layout issue", DiagnosticCategory.Design, member.Location, DiagnosticSeverity.Error,
                            $"This member overlaps memory assigned to another member (sequence {conflictSequence}).");
                    }
                    else
                    {
                        // not assigned
                        memberMap[offset] = member.Sequence;
                    }
                }
            }

            return null;
        }

        protected override IEnumerable<SyntaxDiagnostic> OnGetValidationDiagnostics()
        {
            foreach (var diagnostic1 in base.OnGetValidationDiagnostics())
            {
                yield return diagnostic1;
            }

            SyntaxDiagnostic? diagnostic2;
            if ((diagnostic2 = CheckHasEntityLayoutAttribute()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckLayoutMethodIsSupported()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckBlockSizeIsValid()) is not null) yield return diagnostic2;
            if ((diagnostic2 = CheckMemberLayoutHasNoOverlaps()) is not null) yield return diagnostic2;
        }
    }
}