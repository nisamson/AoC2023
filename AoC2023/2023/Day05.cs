#region license

// AoC2023 - AoC2023 - Day05.cs
// Copyright (C) 2023 Nicholas
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

    using System.Collections.Immutable;
    using AoC.Support;
    using Farkle;
    using Farkle.Builder;

    namespace AoC2023._2023;

    internal enum Category {
        Seed,
        Soil,
        Fertilizer,
        Water,
        Light,
        Temperature,
        Humidity,
        Location,
    }

    internal static class CategoryExtensions {
        public static Category ToCategory(this string input) {
            return input switch {
                "seed"        => Category.Seed,
                "soil"        => Category.Soil,
                "fertilizer"  => Category.Fertilizer,
                "water"       => Category.Water,
                "light"       => Category.Light,
                "temperature" => Category.Temperature,
                "humidity"    => Category.Humidity,
                "location"    => Category.Location,
                _             => throw new ArgumentException("Invalid category", nameof(input))
            };
        }
    }

    internal readonly record struct CategoryRanges {
        public long DestinationRangeStart { get; }
        public long SourceRangeStart { get; }
        public long RangeLength { get; }

        public long DestinationRangeEnd => DestinationRangeStart + RangeLength;
        public long SourceRangeEnd => SourceRangeStart + RangeLength;
        
        public Range<long> SourceRange => new(SourceRangeStart, SourceRangeEnd);
        public Range<long> DestinationRange => new(DestinationRangeStart, DestinationRangeEnd);

        public CategoryRanges(long destinationRangeStart, long sourceRangeStart, long rangeLength) {
            DestinationRangeStart = destinationRangeStart;
            SourceRangeStart = sourceRangeStart;
            RangeLength = rangeLength;
        }
        
        public long? Map(long value) {
            if (!SourceRange.Contains(value)) {
                return null;
            }

            var offset = SourceRangeEnd - value;
            return DestinationRangeStart + offset;
        }
    }

    internal class SeedProblem {
        public IReadOnlySet<long> Seeds { get; init; } = ImmutableHashSet<long>.Empty;

        public IReadOnlyDictionary<(Category, Category), List<CategoryRanges>> Ranges { get; init; } =
            ImmutableDictionary<(Category, Category), List<CategoryRanges>>.Empty;

        public static readonly IReadOnlyList<(Category, Category)> CategoryOrder = new[] {
            (Category.Seed, Category.Soil),
            (Category.Soil, Category.Fertilizer),
            (Category.Fertilizer, Category.Water),
            (Category.Water, Category.Light),
            (Category.Light, Category.Temperature),
            (Category.Temperature, Category.Humidity),
            (Category.Humidity, Category.Location),
        };

        public long Map(Category src, Category dest, long value) {
            if (!Ranges.TryGetValue((src, dest), out var ranges)) {
                return value;
            }

            foreach (var range in ranges) {
                var mapped = range.Map(value);
                if (mapped.HasValue) {
                    return mapped.Value;
                }
            }

            return value;
        }

        public long MapSeed(long seed) {
            var value = seed;
            foreach (var (src, dest) in CategoryOrder) {
                value = Map(src, dest, value);
            }

            return value;
        }
    }

    static class SeedLang {
        public static readonly PrecompilableDesigntimeFarkle<SeedProblem> Designtime;
        public static readonly RuntimeFarkle<SeedProblem> Runtime;

        static SeedLang() {
            var number = Terminals.Int64("number");
            var seedList = Terminal.Literal("seeds:").Appended()
                .Extend(number.Many<long, HashSet<long>>());

            var categorySep = Terminal.Literal("-");
            var word = Terminal.Create("word", (_, data) => data.ToString(), Regex.FromRegexString(@"[a-zA-Z]+"));
            var categoryHeader = Nonterminal.Create(
                "categoryHeader",
                word.Extended().Append(categorySep).Append(word).Append(categorySep).Extend(word).Append(Terminal.Literal("map:"))
                    .Finish((a, b) => (a.ToCategory(), b.ToCategory()))
            );
            var categoryRange = Nonterminal.Create(
                "categoryRange",
                number.Extended().Extend(number).Extend(number).Finish((a, b, c) => new CategoryRanges(a, b, c))
            );
            var categoryRanges = categoryRange.Many<CategoryRanges, List<CategoryRanges>>(true);

            var category = Nonterminal.Create(
                "category",
                categoryHeader.Extended().Extend(categoryRanges).Finish(
                    (header, ranges) => new KeyValuePair<(Category, Category), List<CategoryRanges>>(header, ranges)
                )
            );

            var categoryList = category.Many<KeyValuePair<(Category, Category), List<CategoryRanges>>, Dictionary<(Category, Category), List<CategoryRanges>>>(true);
            
            var seedProblem = Nonterminal.Create(
                "seedProblem",
                seedList.Extend(categoryList).Finish((seeds, ranges) => new SeedProblem {
                    Seeds = seeds,
                    Ranges = ranges.AsReadOnly(),
                })
            );

            Designtime = seedProblem.MarkForPrecompile();
            Runtime = Designtime.Build();
        }
    }

    public class Day05 : Adventer {
        private SeedProblem seedProblem = new();

        protected override void InternalOnLoad() {
            var res = SeedLang.Runtime.Parse(Input.Text);
            if (res.ErrorValue is not null) {
                throw new Exception(res.ErrorValue.ToString());
            }
            seedProblem = res.ResultValue;
        }

        protected override object InternalPart1() {
            return seedProblem.Seeds.Select(seedProblem.MapSeed).Min();
        }

        protected override object InternalPart2() {
            throw new NotImplementedException();
        }
    }
