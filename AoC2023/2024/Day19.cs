using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Primitives;
using Pidgin;
using VDS.Common.Tries;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace AoC2023._2024;

public class Day19 :Adventer{
    protected override void InternalOnLoad() {
        problem = new Problem(Input.Blocks[0].Text, Input.Blocks[1].Lines);
    }

    private class Problem {
        private readonly Regex recognizer;
        private readonly ImmutableArray<string> patterns;
        private readonly ImmutableArray<string> tokens;
        private readonly SparseStringTrie<Unit> tokensTrie;
        private readonly ImmutableArray<string> nonRedundantTokens;
        private readonly Parser<char, List<string>> tokenizer;

        public Problem(string tokens, string[] patterns) {
            this.patterns = [..patterns];
            var recSb = new StringBuilder("^(");
            var tokSpan = tokens.AsSpan();
            var builder = ImmutableArray.CreateBuilder<string>();
            foreach (var tokenRange in tokSpan.Split(',')) {
                var token = tokSpan[tokenRange].Trim();
                builder.Add(token.ToString());
            }
            this.tokens = builder.ToImmutable();

            nonRedundantTokens = [..WithoutRedundant(this.tokens)];
            var regexStr = BuildRegex(nonRedundantTokens);
            recognizer = new Regex(regexStr, RegexOptions.Compiled);
            constructions.Add([], 0);
            
            foreach (var token in nonRedundantTokens) {
                constructions.Add([token], 1);
            }

            foreach (var token in this.tokens) {
                constructions.Add([token], Constructions(token));
            }
            
            tokensTrie = new SparseStringTrie<Unit>();
            foreach (var token in this.tokens) {
                tokensTrie.Add(token, Unit.Value);
            }
        }

        public int Part1() {
            // return patterns.Count(p => recognizer.IsMatch(p));
            var cnt = 0;
            foreach (var (idx, pattern) in patterns.Index()) {
                // var possibleTokens = ApplicableSubTokens(pattern, nonRedundantTokens).ToList();
                // Console.WriteLine("Pattern {0} has {1}/{3} possible tokens: {2}", idx, possibleTokens.Count, string.Join(", ", possibleTokens), nonRedundantTokens.Length);
                // Console.Write("Pattern {0}/{1} ({2}): ", idx, patterns.Length, pattern);
                if (FulLStrategy(pattern)) {
                    // Console.WriteLine("Match");
                    cnt++;
                } else {
                    // Console.WriteLine("No match");
                }
            }

            return cnt;
        }

        public int Part2() {
            // return patterns.Count(p => recognizer.IsMatch(p));
            var cnt = 0;
            foreach (var (idx, pattern) in patterns.Index()) {
                var possibleTokens = ApplicableSubTokens(pattern, nonRedundantTokens).ToList();
                Console.WriteLine("Pattern {0} has {1}/{3} possible tokens: {2}", idx, possibleTokens.Count, string.Join(", ", possibleTokens), nonRedundantTokens.Length);
                Console.Write("Pattern {0}/{1} ({2}): ", idx, patterns.Length, pattern);
                if (Tokens(pattern) is { } tokens) {
                    Console.WriteLine(string.Join(' ', tokens));
                    cnt++;
                } else {
                    Console.WriteLine("No match");
                }
            }

            return cnt;
        }

        private record Count {
            public required int Value { get; init; }
            
            public static implicit operator Count(int value) => new() { Value = value };
            public static implicit operator int(Count value) => value.Value;

            public override string ToString() {
                return Value.ToString();
            }
        }

        private SparseReferenceTrie<ImmutableList<string>, string, Count> constructions = new(l => l);
        
        private int Constructions(string pattern) {
            if (TokensImmutable(pattern) is not { } tokens) {
                return 0;
            }

            return Constructions(tokens);

        }

        private int Constructions(ImmutableList<string> tokens) {
            if (tokens.Count == 0) {
                return 1;
            }
            
            if (constructions.TryGetValue(tokens, out var val)) {
                return val;
            }
            
            var newVal = Constructions(tokens);
            constructions.Add(tokens, newVal);
            return newVal;
        }

        private List<string>? Tokens(string pattern) {
            var match = recognizer.Match(pattern);
            if (!match.Success) {
                return null;
            }

            var captures = match.Groups[1].Captures;
            var tokens = new List<string>(captures.Count);
            foreach (Capture capture in captures) {
                tokens.Add(capture.Value);
            }

            return tokens;
        }
        
        private ImmutableList<string>? TokensImmutable(string pattern) {
            var match = recognizer.Match(pattern);
            if (!match.Success) {
                return null;
            }

            var captures = match.Groups[1].Captures;
            var tokens = ImmutableList.CreateBuilder<string>(); 
            foreach (Capture capture in captures) {
                tokens.Add(capture.Value);
            }

            return tokens.ToImmutable();
        }

        private bool FulLStrategy(string pattern) {
            return recognizer.IsMatch(pattern);
        }
        
        private static string BuildRegex(IEnumerable<string> tokens) {
            var recSb = new StringBuilder("^(");
            recSb.Append(string.Join('|', tokens.Select(token => $"({token})")));
            recSb.Append(")+$");
            return recSb.ToString();
        }

        private static IEnumerable<string> ApplicableSubTokens(string pattern, IEnumerable<string> tokens) {
            return tokens.Where(token => pattern != token && pattern.Contains(token));
        }

        private static bool IsRedundant(string token, IEnumerable<string> tokens) {
            var regex = BuildRegex(ApplicableSubTokens(token, tokens));
            return new Regex(regex).IsMatch(token);
        }

        private static IEnumerable<string> WithoutRedundant(IReadOnlyList<string> tokens) {
            return tokens.Where(token => !IsRedundant(token, tokens));
        }
    }

    private Problem problem;

    public Day19() {
        Bag["test"] = """
                      r, wr, b, g, bwu, rb, gb, br
                      
                      brwrr
                      bggr
                      gbbr
                      rrbgbr
                      ubwu
                      bwurrg
                      brgr
                      bbrgwb
                      """;
    }
    
    protected override object InternalPart1() {
        return problem.Part1();
    }
    
    protected override object InternalPart2() {
        return problem.Part2();
    }
}