using System;
using System.Collections.Generic;
using System.Linq;

namespace RamjiMobile
{
    public enum Suit { Spades, Hearts, Clubs, Diamonds }

    public class Card : IComparable<Card>
    {
        public Suit Suit { get; set; }
        public int Rank { get; set; } // 2..10, 11=J, 12=Q, 13=K, 14=A

        public Card(Suit suit, int rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public string SuitSymbol => Suit switch
        {
            Suit.Spades => "♠",
            Suit.Hearts => "♥",
            Suit.Clubs => "♣",
            Suit.Diamonds => "♦",
            _ => ""
        };

        public string RankString => Rank switch
        {
            14 => "A",
            13 => "K",
            12 => "Q",
            11 => "J",
            _ => Rank.ToString()
        };

        public string DisplayText => $"{RankString}{SuitSymbol}";
        public bool IsRed => Suit == Suit.Hearts || Suit == Suit.Diamonds;

        public int CompareTo(Card? other)
        {
            if (other == null) return 1;
            int rankCompare = Rank.CompareTo(other.Rank);
            if (rankCompare != 0) return rankCompare;
            return Suit.CompareTo(other.Suit);
        }

        public override string ToString() => DisplayText;
    }

    public enum MeldCategory
    {
        Invalid = 0,
        PairPlusCard = 1,
        SameSuitNonConsecutive = 2,
        OtherMixedSequence = 3,
        MixedSequenceA23 = 4,
        MixedSequenceAKQ = 5,
        Ramji = 6,
        OtherPureSequence = 7,
        OnedRoghan = 8,
        PureSequenceAKQ = 9,
        RamjiRoghan = 10,
        ThreeOfAKind = 11
    }

    public class Meld : IComparable<Meld>
    {
        public List<Card> Cards { get; set; } = new();
        public MeldCategory Category { get; set; }
        public int PrimaryValue { get; set; }
        public int SecondaryValue { get; set; }

        public string DisplayName => Category switch
        {
            MeldCategory.ThreeOfAKind => $"Three of a Kind ({Cards[0].RankString}s)",
            MeldCategory.RamjiRoghan => $"Ramji Roghan ({Cards[0].SuitSymbol})",
            MeldCategory.PureSequenceAKQ => $"Pure A-K-Q ({Cards[0].SuitSymbol})",
            MeldCategory.OnedRoghan => $"Oned Roghan ({Cards[0].SuitSymbol})",
            MeldCategory.OtherPureSequence => $"Pure Seq ({Cards.Max(c => c.RankString)}-high)",
            MeldCategory.Ramji => "Ramji (2-3-5)",
            MeldCategory.MixedSequenceAKQ => "Mixed A-K-Q",
            MeldCategory.MixedSequenceA23 => "Mixed A-2-3",
            MeldCategory.OtherMixedSequence => $"Mixed Seq ({Cards.Max(c => c.RankString)}-high)",
            MeldCategory.SameSuitNonConsecutive => $"Same Suit Flush ({Cards[0].SuitSymbol})",
            MeldCategory.PairPlusCard => $"Pair of {PrimaryValue}s",
            _ => "Invalid"
        };

        public int CompareTo(Meld? other)
        {
            if (other == null) return 1;
            if (Category != other.Category)
                return Category.CompareTo(other.Category);

            if (PrimaryValue != other.PrimaryValue)
                return PrimaryValue.CompareTo(other.PrimaryValue);

            return SecondaryValue.CompareTo(other.SecondaryValue);
        }
    }
}