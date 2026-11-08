using System;
using System.Collections.Generic;
using System.Linq;

namespace RamjiMobile
{
    public static class MeldEvaluator
    {
        public static Meld Evaluate(List<Card> cards)
        {
            if (cards == null || cards.Count != 3)
                return new Meld { Category = MeldCategory.Invalid };

            var sorted = cards.OrderBy(c => c.Rank).ToList();
            bool sameSuit = sorted[0].Suit == sorted[1].Suit && sorted[1].Suit == sorted[2].Suit;
            var ranks = sorted.Select(c => c.Rank).ToList();

            // 1. Three of a Kind
            if (ranks[0] == ranks[1] && ranks[1] == ranks[2])
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.ThreeOfAKind,
                    PrimaryValue = ranks[0]
                };
            }

            // 2. Ramji Roghan: 2, 3, 5 same suit
            if (sameSuit && ranks[0] == 2 && ranks[1] == 3 && ranks[2] == 5)
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.RamjiRoghan,
                    PrimaryValue = (int)sorted[0].Suit
                };
            }

            // 3. Pure Sequence A-K-Q
            if (sameSuit && ranks[0] == 12 && ranks[1] == 13 && ranks[2] == 14)
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.PureSequenceAKQ,
                    PrimaryValue = (int)sorted[0].Suit
                };
            }

            // 4. Oned Roghan: A, 2, 3 same suit
            if (sameSuit && ranks[0] == 2 && ranks[1] == 3 && ranks[2] == 14)
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.OnedRoghan,
                    PrimaryValue = (int)sorted[0].Suit
                };
            }

            // 5. Other Pure Sequences
            if (sameSuit && ranks[0] + 1 == ranks[1] && ranks[1] + 1 == ranks[2])
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.OtherPureSequence,
                    PrimaryValue = ranks[2]
                };
            }

            // 6. Ramji: 2, 3, 5 mixed
            if (ranks[0] == 2 && ranks[1] == 3 && ranks[2] == 5)
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.Ramji,
                    PrimaryValue = 0
                };
            }

            // 7. Mixed Sequence A-K-Q
            if (ranks[0] == 12 && ranks[1] == 13 && ranks[2] == 14)
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.MixedSequenceAKQ,
                    PrimaryValue = 0
                };
            }

            // 8. Mixed Sequence A-2-3
            if (ranks[0] == 2 && ranks[1] == 3 && ranks[2] == 14)
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.MixedSequenceA23,
                    PrimaryValue = 0
                };
            }

            // 9. Other Mixed Sequences
            if (ranks[0] + 1 == ranks[1] && ranks[1] + 1 == ranks[2])
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.OtherMixedSequence,
                    PrimaryValue = ranks[2]
                };
            }

            // 10. Same-Suit Non-Consecutive
            if (sameSuit)
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.SameSuitNonConsecutive,
                    PrimaryValue = ranks[2],
                    SecondaryValue = ranks[1]
                };
            }

            // 11. Pair + Card
            if (ranks[0] == ranks[1])
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.PairPlusCard,
                    PrimaryValue = ranks[0],
                    SecondaryValue = ranks[2]
                };
            }
            if (ranks[1] == ranks[2])
            {
                return new Meld
                {
                    Cards = cards,
                    Category = MeldCategory.PairPlusCard,
                    PrimaryValue = ranks[1],
                    SecondaryValue = ranks[0]
                };
            }

            return new Meld { Category = MeldCategory.Invalid };
        }

        public static (List<Meld> melds, List<Card> discarded) OptimizeHand(List<Card> hand)
        {
            var bestMelds = new List<Meld>();
            var allTriplets = new List<Meld>();

            int n = hand.Count;
            for (int i = 0; i < n - 2; i++)
            {
                for (int j = i + 1; j < n - 1; j++)
                {
                    for (int k = j + 1; k < n; k++)
                    {
                        var triplet = new List<Card> { hand[i], hand[j], hand[k] };
                        var meld = Evaluate(triplet);
                        if (meld.Category != MeldCategory.Invalid)
                        {
                            allTriplets.Add(meld);
                        }
                    }
                }
            }

            allTriplets = allTriplets.OrderByDescending(m => m).ToList();
            var selected = new List<Meld>();
            var usedCards = new HashSet<Card>();

            void Search(int startIndex)
            {
                if (selected.Count == 5)
                {
                    if (bestMelds.Count < 5 || ScoreMeldSet(selected) > ScoreMeldSet(bestMelds))
                    {
                        bestMelds = new List<Meld>(selected);
                    }
                    return;
                }

                for (int idx = startIndex; idx < allTriplets.Count; idx++)
                {
                    var candidate = allTriplets[idx];

                    if (candidate.Cards.Any(c => usedCards.Contains(c)))
                        continue;

                    if (selected.Count > 0 && candidate.CompareTo(selected[^1]) > 0)
                        continue;

                    selected.Add(candidate);
                    foreach (var c in candidate.Cards) usedCards.Add(c);

                    Search(idx + 1);

                    selected.RemoveAt(selected.Count - 1);
                    foreach (var c in candidate.Cards) usedCards.Remove(c);

                    if (bestMelds.Count == 5) return;
                }
            }

            Search(0);

            if (bestMelds.Count == 0)
            {
                foreach (var meld in allTriplets)
                {
                    if (!meld.Cards.Any(c => usedCards.Contains(c)))
                    {
                        bestMelds.Add(meld);
                        foreach (var c in meld.Cards) usedCards.Add(c);
                        if (bestMelds.Count == 5) break;
                    }
                }
            }

            bestMelds = bestMelds.OrderByDescending(m => m).ToList();
            var used = new HashSet<Card>(bestMelds.SelectMany(m => m.Cards));
            var discarded = hand.Where(c => !used.Contains(c)).ToList();

            return (bestMelds, discarded);
        }

        private static long ScoreMeldSet(List<Meld> melds)
        {
            long score = 0;
            for (int i = 0; i < melds.Count; i++)
            {
                score += (long)melds[i].Category * (100000000L / (long)Math.Pow(10, i * 2));
            }
            return score;
        }
    }
}