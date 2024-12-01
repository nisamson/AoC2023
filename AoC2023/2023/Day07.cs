using System.Collections.Immutable;
using System.Diagnostics;

namespace AoC2023._2023;

public enum Card {
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace
}

public enum AltCard {
    Jack,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Queen,
    King,
    Ace
}

public enum HandKind {
    High,
    Pair,
    TwoPair,
    ThreeOfAKind,
    FullHouse,
    FourOfAKind,
    FiveOfAKind
}

public static class Day07Extensions {
    public static Card AsCard(this string input) {
        return input switch {
            "2" => Card.Two,
            "3" => Card.Three,
            "4" => Card.Four,
            "5" => Card.Five,
            "6" => Card.Six,
            "7" => Card.Seven,
            "8" => Card.Eight,
            "9" => Card.Nine,
            "T" => Card.Ten,
            "J" => Card.Jack,
            "Q" => Card.Queen,
            "K" => Card.King,
            "A" => Card.Ace,
            _ => throw new ArgumentException("Invalid card", nameof(input))
        };
    }

    public static Card AsCard(this char input) {
        return input switch {
            '2' => Card.Two,
            '3' => Card.Three,
            '4' => Card.Four,
            '5' => Card.Five,
            '6' => Card.Six,
            '7' => Card.Seven,
            '8' => Card.Eight,
            '9' => Card.Nine,
            'T' => Card.Ten,
            'J' => Card.Jack,
            'Q' => Card.Queen,
            'K' => Card.King,
            'A' => Card.Ace,
            _ => throw new ArgumentException("Invalid card", nameof(input))
        };
    }

    public static AltCard ToAltCard(this Card card) {
        return card switch {
            Card.Two => AltCard.Two,
            Card.Three => AltCard.Three,
            Card.Four => AltCard.Four,
            Card.Five => AltCard.Five,
            Card.Six => AltCard.Six,
            Card.Seven => AltCard.Seven,
            Card.Eight => AltCard.Eight,
            Card.Nine => AltCard.Nine,
            Card.Ten => AltCard.Ten,
            Card.Jack => AltCard.Jack,
            Card.Queen => AltCard.Queen,
            Card.King => AltCard.King,
            Card.Ace => AltCard.Ace,
            _ => throw new ArgumentException("Invalid card", nameof(card))
        };
    }
}

public record Hand : IComparable<Hand> {
    public static readonly IComparer<Hand> AlternateComparer = Comparer<Hand>.Create((a, b) => a.CompareToAlternate(b));

    public Hand(IList<Card> cards) {
        if (cards.Count != 5) throw new ArgumentException("Must have 5 cards", nameof(cards));

        Cards = cards.AsReadOnly();
        Kind = CalculateKind();
        AlternateKind = CalculateAlternateKind();
    }

    public IReadOnlyList<Card> Cards { get; }

    public HandKind Kind { get; }
    public HandKind AlternateKind { get; }

    public int CompareTo(Hand? other) {
        if (ReferenceEquals(this, other)) return 0;

        if (ReferenceEquals(null, other)) return 1;

        var cmp = Kind.CompareTo(other.Kind);
        if (cmp != 0) return cmp;

        foreach (var (card, otherCard) in Cards.Zip(other.Cards)) {
            cmp = card.CompareTo(otherCard);
            if (cmp != 0) return cmp;
        }

        return 0;
    }

    private HandKind CalculateKind() {
        var cardsWithCount = Cards.GroupBy(c => c).Select(grp => (grp.Key, grp.Count()))
            .ToDictionary(t => t.Key, t => t.Item2);
        var counts = cardsWithCount.Values;
        if (counts.Contains(5)) return HandKind.FiveOfAKind;

        if (counts.Contains(4)) return HandKind.FourOfAKind;

        if (counts.Contains(3)) {
            if (counts.Contains(2)) return HandKind.FullHouse;

            return HandKind.ThreeOfAKind;
        }

        if (counts.Contains(2)) {
            if (counts.Count(c => c == 2) == 2) return HandKind.TwoPair;

            return HandKind.Pair;
        }

        return HandKind.High;
    }

    private HandKind CalculateAlternateKind() {
        var cardsWithCount = Cards.GroupBy(c => c).Select(grp => (grp.Key, grp.Count()))
            .ToDictionary(t => t.Key, t => t.Item2);
        var jokerCount = cardsWithCount.GetValueOrDefault(Card.Jack, 0);
        if (jokerCount is 0 or 5) return Kind;

        if (jokerCount == 1 && Kind == HandKind.TwoPair) return HandKind.FullHouse;

        cardsWithCount.Remove(Card.Jack);
        var counts = cardsWithCount.Values;
        var max = counts.Max(c => c + jokerCount);
        return max switch {
            5 => HandKind.FiveOfAKind,
            4 => HandKind.FourOfAKind,
            3 => HandKind.ThreeOfAKind,
            2 => HandKind.Pair,
            _ => throw new UnreachableException("Impossible to not have a pair with joker in hand.")
        };
    }

    public static Hand Parse(string input) {
        var cards = input.Select(c => c.AsCard())
            .ToList();
        return new Hand(cards);
    }

    public int CompareToAlternate(Hand? other) {
        if (ReferenceEquals(this, other)) return 0;

        if (ReferenceEquals(null, other)) return 1;

        var cmp = AlternateKind.CompareTo(other.AlternateKind);
        if (cmp != 0) return cmp;

        foreach (var (card, otherCard) in
                 Cards.Select(c => c.ToAltCard()).Zip(other.Cards.Select(c => c.ToAltCard()))) {
            cmp = card.CompareTo(otherCard);
            if (cmp != 0) return cmp;
        }

        return 0;
    }
}

public class Gambit(Hand hand, long bet) {
    public Hand Hand { get; } = hand;
    public long Bet { get; } = bet;

    public static Gambit Parse(string input) {
        var split = input.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var hand = Hand.Parse(split[0]);
        var bet = long.Parse(split[1]);
        return new Gambit(hand, bet);
    }
}

public class Day07 : Adventer {
    private ImmutableArray<Gambit> gambits;

    public Day07() {
        Bag["test"] = """
                      32T3K 765
                      T55J5 684
                      KK677 28
                      KTJJT 220
                      QQQJA 483
                      """;
    }

    protected override void InternalOnLoad() {
        gambits = Input.Lines.Select(Gambit.Parse).ToImmutableArray();
    }

    protected override object InternalPart1() {
        return gambits.OrderBy(g => g.Hand)
            .Select((g, idx) => (idx + 1) * g.Bet)
            .Sum();
    }

    protected override object InternalPart2() {
        return gambits.OrderBy(g => g.Hand, Hand.AlternateComparer)
            .Select((g, idx) => (idx + 1) * g.Bet)
            .Sum();
    }
}