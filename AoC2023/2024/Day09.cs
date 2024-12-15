using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Akade.IndexedSet;
using VDS.Common.Collections.Enumerations;
using System.Collections.Generic;
using System.Diagnostics;
using AoC.Support.Functional;
using NetTopologySuite.Utilities;

namespace AoC2023._2024;

public class Day09 : Adventer {
    private readonly record struct Space {
        public required short? Id { get; init; }
        public required byte Length { get; init; }

        public required int Offset { get; init; }

        [MemberNotNullWhen(false, nameof(Id))] public bool IsFree => !Id.HasValue;
    }

    private readonly record struct DiskMap {
        public ImmutableArray<Space> Spaces { get; private init; }

        public int TotalLength => Spaces.Sum(s => s.Length);

        public DiskMap(string map) {
            var builder = ImmutableArray.CreateBuilder<Space>();
            var file = true;
            short curFile = 0;
            var offset = 0;
            foreach (var num in map.Select(c => c - '0')) {
                short? id = null;
                if (file) {
                    id = curFile++;
                }

                file = !file;

                builder.Add(new Space {
                    Id = id,
                    Length = (byte)num,
                    Offset = offset
                });
                offset += num;
            }

            Spaces = builder.ToImmutable();
        }
    }

    private class Disk {
        private readonly record struct FreeSpace : IComparable<FreeSpace> {
            public required int Offset { get; init; }
            public required Memory<short?> Contents { get; init; }

            public FreeSpace? CopyFile(short id, int length) {
                var fillSpan = Contents[..length];
                fillSpan.Span.Fill(id);
                var newContents = Contents[length..];

                if (newContents.IsEmpty) {
                    return null;
                }

                return new FreeSpace {
                    Offset = Offset + length,
                    Contents = Contents[length..]
                };
            }

            public int Length => Contents.Length;

            public int CompareTo(FreeSpace other) {
                return Offset.CompareTo(other.Offset);
            }
        }

        private short?[] Contents { get; }

        public Disk(DiskMap map) {
            Contents = new short?[map.TotalLength];
            var offset = 0;
            foreach (var space in map.Spaces) {
                if (!space.IsFree) {
                    for (var i = 0; i < space.Length; i++) {
                        Contents[offset + i] = space.Id;
                    }
                }

                offset += space.Length;
            }
        }

        public void Compact() {
            var frontView = Contents.AsSpan();
            var backView = frontView;
            while (frontView.Overlaps(backView)) {
                var changed = false;
                while (frontView is [{ } id, ..]) {
                    frontView = frontView[1..];
                    changed = true;
                }

                while (backView is [.., null]) {
                    backView = backView[..^1];
                    changed = true;
                }

                if (changed) {
                    // recheck overlap
                    continue;
                }

                // no overlap, front is free and back has data
                while (frontView is [null, ..] && backView is [.., { }]) {
                    frontView[0] = backView[^1];
                    backView[^1] = null;
                    frontView = frontView[1..];
                    backView = backView[..^1];
                }
            }
        }

        public void CompactFull() {
            var index = new FreeSpaceIndex(GetFreeSpaces());

            var mem = Contents.AsSpan();
            while (!mem.IsEmpty) {
                while (mem is [.., null]) {
                    mem = mem[..^1];
                }

                if (mem.IsEmpty) {
                    break;
                }

                // can't be null because of above loop
                var curId = (short)mem[^1]!;
                var curOffset = mem.Length;
                while (mem is [.., { } id] && id == curId) {
                    mem = mem[..^1];
                }

                var newOffset = mem.Length;

                if (!index.TryMoveFile(curId, newOffset, curOffset - newOffset)) {
                    continue;
                }

                var fileData = Contents.AsSpan()[newOffset..curOffset];
                fileData.Clear();
            }
        }

        public void CompactFullSlow() {
            var mem = Contents.AsSpan();
            while (!mem.IsEmpty) {
                while (mem is [.., null]) {
                    mem = mem[..^1];
                }

                if (mem.IsEmpty) {
                    break;
                }

                // can't be null because of above loop
                var curId = (short)mem[^1]!;
                var curOffset = mem.Length;
                while (mem is [.., { } id] && id == curId) {
                    mem = mem[..^1];
                }

                var newOffset = mem.Length;
                var length = curOffset - newOffset;

                var freeSpace = GetFreeSpaces().FirstOrNone(space => space.Length >= length && space.Offset < newOffset);
                if (freeSpace.IsNone) {
                    continue;
                }

                var space = freeSpace.Value;
                space.Contents[..length].Span.Fill(curId);
                var fileData = Contents.AsSpan()[newOffset..curOffset];
                fileData.Clear();
            }
        }

        public override string ToString() {
            return string.Join("|", Contents.Select(
                s => s switch {
                    { } id => id.ToString(),
                    null => "."
                }
            ));
        }

        public long CalculateChecksum() {
            var sum = 0L;
            for (var i = 0; i < Contents.Length; i++) {
                if (Contents[i] is { } id) {
                    sum += i * id;
                }
            }

            return sum;
        }

        private IEnumerable<FreeSpace> GetFreeSpaces() {
            var offset = 0;
            while (offset < Contents.Length) {
                var changed = false;
                while (offset < Contents.Length && Contents[offset] is not null) {
                    offset++;
                    changed = true;
                }

                if (changed) {
                    continue;
                }

                var length = 0;
                while (offset + length < Contents.Length && Contents[offset + length] is null) {
                    length++;
                }

                yield return new FreeSpace {
                    Offset = offset,
                    Contents = Contents.AsMemory()[offset..(offset + length)]
                };
                offset += length;
            }
        }

        private class FreeSpaceIndex {
            private static readonly FreeSpace MinSpace = new() {
                Contents = Memory<short?>.Empty,
                Offset = int.MinValue
            };

            private static readonly FreeSpace MaxSpace = new() {
                Contents = Memory<short?>.Empty,
                Offset = int.MaxValue
            };

            private SortedDictionary<int, PriorityQueue<FreeSpace, int>> spaces;

            public FreeSpaceIndex(IEnumerable<FreeSpace> freeSpaces) {
                spaces = new();
                foreach (var space in freeSpaces) {
                    AddSpace(space);
                }
            }

            private void AddSpace(FreeSpace space) {
                GetSet(space.Length).Enqueue(space, space.Offset);
            }

            private PriorityQueue<FreeSpace, int> GetSet(int length) {
                if (spaces.TryGetValue(length, out var set)) {
                    return set;
                }

                var newSet = new PriorityQueue<FreeSpace, int>();
                spaces[length] = newSet;
                return newSet;
            }

            private FreeSpace? AllocateSpace(int offset, int length) {
                var availableSpaces = spaces.Keys.Where(k => k >= length);
                foreach (var spaceSet in availableSpaces.Select(GetSet)) {
                    if (!spaceSet.TryDequeue(out var space, out var spaceOffset)) {
                        continue;
                    }

                    if (spaceOffset < offset) {
                        return space;
                    }

                    spaceSet.Enqueue(space, spaceOffset);
                }

                return null;
            }

            // returns true on successful copy
            private bool TryCopyFile(short id, int offset, int length, out FreeSpace? newSpace) {
                newSpace = null;
                if (AllocateSpace(offset, length) is { } space) {
                    newSpace = space.CopyFile(id, length);
                    return true;
                }

                return false;
            }

            public bool TryMoveFile(short id, int offset, int length) {
                var success = TryCopyFile(id, offset, length, out var newSpace);
                if (newSpace is { } space) {
                    AddSpace(space);
                }

                return success;
            }
        }
    }

    private DiskMap map;

    public Day09() {
        Bag["test"] = "2333133121414131402";
    }

    protected override void InternalOnLoad() {
        map = new DiskMap(Input.Text);
    }

    protected override object InternalPart1() {
        var disk = new Disk(map);
        disk.Compact();
        return disk.CalculateChecksum();
    }

    protected override object InternalPart2() {
        var disk = new Disk(map);
        disk.CompactFullSlow();
        return disk.CalculateChecksum();
    }
}